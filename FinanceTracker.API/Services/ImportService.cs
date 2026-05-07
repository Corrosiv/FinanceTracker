using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;
using Microsoft.EntityFrameworkCore;
using FinanceTracker.API.Data;
using FinanceTracker.API.DTOs;
using FinanceTracker.API.Models;

namespace FinanceTracker.API.Services;

public class ImportService : IImportService
{
    private readonly FinanceDbContext _db;

    private static readonly Guid DefaultUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public ImportService(FinanceDbContext db)
    {
        _db = db;
    }

    public async Task<ImportResponseDto> ImportCsvAsync(Stream csvStream, string fileName, CsvColumnMappingDto mapping)
    {
        var import = new Import
        {
            Id = Guid.NewGuid(),
            UserId = DefaultUserId,
            FileName = fileName,
            UploadDate = DateTime.UtcNow,
            Status = ImportStatus.Processing,
            ColumnMapping = JsonSerializer.Serialize(mapping)
        };
        _db.Imports.Add(import);
        await _db.SaveChangesAsync();

        var errors = new List<ImportRowErrorDto>();
        var transactions = new List<Transaction>();
        var rawRows = new List<RawImportRow>();

        var culture = ResolveCulture(mapping.Culture);
        var (records, columnValidationErrors) = ParseCsvWithValidation(csvStream, culture, mapping);

        // If required columns are missing, report summary error
        if (columnValidationErrors.Count > 0)
        {
            import.Status = ImportStatus.Failed;
            await _db.SaveChangesAsync();

            return new ImportResponseDto
            {
                Id = import.Id,
                FileName = import.FileName,
                Status = import.Status.ToString(),
                RowCount = 0,
                ProcessedCount = 0,
                DuplicateCount = 0,
                Errors = columnValidationErrors
            };
        }

        int rowNumber = 0;

        foreach (var record in records)
        {
            rowNumber++;
            var rawJson = JsonSerializer.Serialize(record);
            var rawRow = new RawImportRow
            {
                Id = Guid.NewGuid(),
                ImportId = import.Id,
                RowNumber = rowNumber,
                RawData = rawJson
            };

            try
            {
                var transaction = NormalizeRow(record, mapping, import.Id, culture);
                rawRow.TransactionId = transaction.Id;
                transactions.Add(transaction);
            }
            catch (FieldValidationException ex)
            {
                rawRow.Error = ex.Message;
                errors.Add(new ImportRowErrorDto
                {
                    RowNumber = rowNumber,
                    Field = ex.FieldName,
                    Error = ex.Message,
                    RawData = rawJson
                });
            }
            catch (Exception ex)
            {
                rawRow.Error = ex.Message;
                errors.Add(new ImportRowErrorDto
                {
                    RowNumber = rowNumber,
                    Error = ex.Message,
                    RawData = rawJson
                });
            }

            rawRows.Add(rawRow);
        }

        // Deduplication: check existing hashes in DB
        var candidateHashes = transactions
            .Select(t => t.DeduplicationHash!)
            .ToHashSet();

        var existingHashes = await _db.Transactions
            .Where(t => t.UserId == DefaultUserId && candidateHashes.Contains(t.DeduplicationHash!))
            .Select(t => t.DeduplicationHash!)
            .ToListAsync();

        var existingHashSet = existingHashes.ToHashSet();

        // Also deduplicate within the batch itself
        var seenHashes = new HashSet<string>();
        int duplicateCount = 0;
        var uniqueTransactions = new List<Transaction>();

        foreach (var tx in transactions)
        {
            if (existingHashSet.Contains(tx.DeduplicationHash!) || !seenHashes.Add(tx.DeduplicationHash!))
            {
                duplicateCount++;
                var rawRow = rawRows.First(r => r.TransactionId == tx.Id);
                rawRow.TransactionId = null;
                rawRow.Error = "Duplicate transaction.";
            }
            else
            {
                uniqueTransactions.Add(tx);
            }
        }

        _db.RawImportRows.AddRange(rawRows);
        _db.Transactions.AddRange(uniqueTransactions);

        import.RowCount = rowNumber;
        import.ProcessedCount = uniqueTransactions.Count;
        import.DuplicateCount = duplicateCount;
        import.Status = errors.Count > 0 && uniqueTransactions.Count == 0
            ? ImportStatus.Failed
            : ImportStatus.Completed;

        await _db.SaveChangesAsync();

        return new ImportResponseDto
        {
            Id = import.Id,
            FileName = import.FileName,
            Status = import.Status.ToString(),
            RowCount = import.RowCount,
            ProcessedCount = import.ProcessedCount,
            DuplicateCount = import.DuplicateCount,
            Errors = errors
        };
    }

    // ── CSV parsing with column validation ──────────────────────────

    private static (List<Dictionary<string, string>>, List<ImportRowErrorDto>) ParseCsvWithValidation(
        Stream stream,
        CultureInfo culture,
        CsvColumnMappingDto mapping)
    {
        var records = new List<Dictionary<string, string>>();
        var errors = new List<ImportRowErrorDto>();

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var config = new CsvConfiguration(culture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
            TrimOptions = TrimOptions.Trim
        };

        using var csv = new CsvReader(reader, config);
        csv.Read();
        csv.ReadHeader();
        var headers = csv.HeaderRecord!;

        // Validate that all required columns exist
        var missingColumns = new List<string>();
        if (!headers.Contains(mapping.Date, StringComparer.OrdinalIgnoreCase))
            missingColumns.Add($"Date (column '{mapping.Date}')");
        if (!headers.Contains(mapping.Description, StringComparer.OrdinalIgnoreCase))
            missingColumns.Add($"Description (column '{mapping.Description}')");

        // Check amount/debit/credit columns
        bool hasAmountColumn = mapping.Amount is not null && headers.Contains(mapping.Amount, StringComparer.OrdinalIgnoreCase);
        bool hasDebitColumn = mapping.Debit is not null && headers.Contains(mapping.Debit, StringComparer.OrdinalIgnoreCase);
        bool hasCreditColumn = mapping.Credit is not null && headers.Contains(mapping.Credit, StringComparer.OrdinalIgnoreCase);

        if (!hasAmountColumn && !hasDebitColumn && !hasCreditColumn)
        {
            if (mapping.Amount is not null)
                missingColumns.Add($"Amount (column '{mapping.Amount}')");
            if (mapping.Debit is not null)
                missingColumns.Add($"Debit (column '{mapping.Debit}')");
            if (mapping.Credit is not null)
                missingColumns.Add($"Credit (column '{mapping.Credit}')");
        }

        if (missingColumns.Count > 0)
        {
            errors.Add(new ImportRowErrorDto
            {
                RowNumber = 0,
                Field = "Header",
                Error = $"Missing required columns: {string.Join(", ", missingColumns)}. Please verify the column mapping configuration."
            });
            return (records, errors);
        }

        // Parse data rows
        while (csv.Read())
        {
            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in headers)
            {
                row[header] = csv.GetField(header) ?? string.Empty;
            }
            records.Add(row);
        }

        return (records, errors);
    }

    // ── CSV parsing ────────────────────────────────────────────────────

    private static List<Dictionary<string, string>> ParseCsv(Stream stream, CultureInfo culture)
    {
        var records = new List<Dictionary<string, string>>();

        using var reader = new StreamReader(stream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true);
        var config = new CsvConfiguration(culture)
        {
            HasHeaderRecord = true,
            MissingFieldFound = null,
            BadDataFound = null,
            TrimOptions = TrimOptions.Trim
        };

        using var csv = new CsvReader(reader, config);
        csv.Read();
        csv.ReadHeader();
        var headers = csv.HeaderRecord!;

        while (csv.Read())
        {
            var row = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var header in headers)
            {
                row[header] = csv.GetField(header) ?? string.Empty;
            }
            records.Add(row);
        }

        return records;
    }

    // ── Row normalization ──────────────────────────────────────────────

    private Transaction NormalizeRow(
        Dictionary<string, string> row,
        CsvColumnMappingDto mapping,
        Guid importId,
        CultureInfo culture)
    {
        var dateStr = GetRequiredField(row, mapping.Date, "Date");
        var date = ParseDate(dateStr, mapping.DateFormat, culture);

        var description = GetRequiredField(row, mapping.Description, "Description");

        var amount = ParseAmount(row, mapping, culture);

        decimal? balance = null;
        if (mapping.Balance is not null
            && row.TryGetValue(mapping.Balance, out var balStr)
            && !string.IsNullOrWhiteSpace(balStr))
        {
            balance = ParseDecimal(balStr, culture);
        }

        var normalizedDescription = description.Trim().ToLowerInvariant();
        var hash = ComputeHash(DefaultUserId, date, amount, normalizedDescription);

        return new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = DefaultUserId,
            ImportId = importId,
            Date = date,
            Amount = amount,
            RawDescription = description,
            NormalizedDescription = normalizedDescription,
            Balance = balance,
            CreatedAt = DateTime.UtcNow,
            DeduplicationHash = hash
        };
    }

    // ── Field helpers ──────────────────────────────────────────────────

    private static string GetRequiredField(Dictionary<string, string> row, string columnName, string fieldName)
    {
        if (!row.TryGetValue(columnName, out var value) || string.IsNullOrWhiteSpace(value))
            throw new FieldValidationException(fieldName, $"Missing required field '{fieldName}' (column '{columnName}').");
        return value;
    }

    private static DateTime ParseDate(string dateStr, string? dateFormat, CultureInfo culture)
    {
        if (dateFormat is not null)
        {
            if (DateTime.TryParseExact(dateStr, dateFormat, culture, DateTimeStyles.None, out var exact))
                return exact;
            throw new FieldValidationException("Date", $"Invalid date format. Expected format: '{dateFormat}', got: '{dateStr}'.");
        }

        if (DateTime.TryParse(dateStr, culture, DateTimeStyles.None, out var parsed))
            return parsed;
        throw new FieldValidationException("Date", $"Cannot parse date '{dateStr}'. Please check the date value and format.");
    }

    private static decimal ParseAmount(Dictionary<string, string> row, CsvColumnMappingDto mapping, CultureInfo culture)
    {
        // Pattern 1: single signed Amount column (Chase, Capital One, etc.)
        if (mapping.Amount is not null)
        {
            var amountStr = GetRequiredField(row, mapping.Amount, "Amount");
            try
            {
                return ParseDecimal(amountStr, culture);
            }
            catch (FieldValidationException)
            {
                throw;
            }
        }

        // Pattern 2: separate Debit/Credit columns (BBVA, Santander, Wells Fargo)
        if (mapping.Debit is not null || mapping.Credit is not null)
        {
            if (mapping.Debit is not null
                && row.TryGetValue(mapping.Debit, out var debitStr)
                && !string.IsNullOrWhiteSpace(debitStr))
            {
                try
                {
                    return -Math.Abs(ParseDecimal(debitStr, culture));
                }
                catch (FieldValidationException ex)
                {
                    throw new FieldValidationException("Debit", $"Invalid debit amount: {ex.Message}");
                }
            }

            if (mapping.Credit is not null
                && row.TryGetValue(mapping.Credit, out var creditStr)
                && !string.IsNullOrWhiteSpace(creditStr))
            {
                try
                {
                    return Math.Abs(ParseDecimal(creditStr, culture));
                }
                catch (FieldValidationException ex)
                {
                    throw new FieldValidationException("Credit", $"Invalid credit amount: {ex.Message}");
                }
            }

            throw new FieldValidationException("Debit/Credit", "Row has neither Debit nor Credit value.");
        }

        throw new InvalidOperationException("Column mapping must specify either 'Amount' or 'Debit'/'Credit'.");
    }

    private static decimal ParseDecimal(string value, CultureInfo culture)
    {
        // Strip common currency symbols and whitespace
        var cleaned = value
            .Replace("$", "")
            .Replace("€", "")
            .Replace("MXN", "")
            .Replace("USD", "")
            .Replace('\u2212', '-') // U+2212 minus sign → standard hyphen
            .Trim();

        if (decimal.TryParse(cleaned, NumberStyles.Number | NumberStyles.AllowLeadingSign, culture, out var result))
            return result;

        throw new FieldValidationException("Amount", $"Cannot parse amount '{value}' as a decimal number. Please check the value.");
    }

    // ── Deduplication ──────────────────────────────────────────────────

    internal static string ComputeHash(Guid userId, DateTime date, decimal amount, string normalizedDescription)
    {
        var input = $"{userId}|{date:yyyy-MM-dd}|{amount}|{normalizedDescription}";
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(input));
        return Convert.ToHexStringLower(bytes);
    }

    private static CultureInfo ResolveCulture(string? cultureName)
    {
        if (cultureName is null) return CultureInfo.InvariantCulture;
        try { return CultureInfo.GetCultureInfo(cultureName); }
        catch { return CultureInfo.InvariantCulture; }
    }
}
