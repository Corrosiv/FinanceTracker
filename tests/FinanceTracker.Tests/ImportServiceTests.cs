using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using FinanceTracker.API.Data;
using FinanceTracker.API.DTOs;
using FinanceTracker.API.Models;
using FinanceTracker.API.Services;

namespace FinanceTracker.Tests;

public class ImportServiceTests
{
    private static readonly Guid UserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    private static FinanceDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new FinanceDbContext(options);
        db.Users.Add(new User { Id = UserId, Name = "Test", CreatedAt = DateTime.UtcNow });
        db.SaveChanges();
        return db;
    }

    private static Stream ToCsvStream(string csv) =>
        new MemoryStream(Encoding.UTF8.GetBytes(csv));

    private static CsvColumnMappingDto UsMapping => new()
    {
        Date = "Date",
        Description = "Description",
        Amount = "Amount",
        Balance = "Balance",
        DateFormat = "MM/dd/yyyy"
    };

    private static CsvColumnMappingDto MxMapping => new()
    {
        Date = "Fecha",
        Description = "Descripcion",
        Debit = "Cargo",
        Credit = "Abono",
        Balance = "Saldo",
        DateFormat = "dd/MM/yyyy"
    };

    // ── US bank format (Pattern 1: single signed Amount) ───────────

    [Fact]
    public async Task WhenImportingUsCsv_ShouldCreateTransactions()
    {
        using var db = CreateInMemoryDb();
        var svc = new ImportService(db);
        var csv = "Date,Description,Amount,Balance\n03/15/2026,WALMART GROCERY,-45.23,1204.77\n03/14/2026,DIRECT DEPOSIT,2500.00,1250.00\n";

        var result = await svc.ImportCsvAsync(ToCsvStream(csv), "chase.csv", UsMapping);

        Assert.Equal("Completed", result.Status);
        Assert.Equal(2, result.RowCount);
        Assert.Equal(2, result.ProcessedCount);
        Assert.Equal(0, result.DuplicateCount);
        Assert.Empty(result.Errors);
    }

    [Fact]
    public async Task WhenImportingUsCsv_ShouldStoreCorrectAmountsAndDates()
    {
        using var db = CreateInMemoryDb();
        var svc = new ImportService(db);
        var csv = "Date,Description,Amount,Balance\n03/15/2026,WALMART GROCERY,-45.23,1204.77\n";

        await svc.ImportCsvAsync(ToCsvStream(csv), "chase.csv", UsMapping);

        var tx = await db.Transactions.FirstAsync(t => t.RawDescription == "WALMART GROCERY");
        Assert.Equal(-45.23m, tx.Amount);
        Assert.Equal(1204.77m, tx.Balance);
        Assert.Equal(new DateTime(2026, 3, 15), tx.Date);
        Assert.Equal("walmart grocery", tx.NormalizedDescription);
    }

    // ── Mexican bank format (Pattern 2: Cargo/Abono) ───────────────

    [Fact]
    public async Task WhenImportingMexicanCsv_ShouldParseDebitCreditColumns()
    {
        using var db = CreateInMemoryDb();
        var svc = new ImportService(db);
        var csv = "Fecha,Descripcion,Cargo,Abono,Saldo\n15/03/2026,OXXO PAGO,345.00,,8500.00\n14/03/2026,TRANSFERENCIA SPEI,,5000.00,8845.00\n";

        var result = await svc.ImportCsvAsync(ToCsvStream(csv), "bbva.csv", MxMapping);

        Assert.Equal(2, result.ProcessedCount);
        Assert.Empty(result.Errors);

        var expense = await db.Transactions.FirstAsync(t => t.RawDescription == "OXXO PAGO");
        Assert.Equal(-345.00m, expense.Amount); // debit stored as negative

        var income = await db.Transactions.FirstAsync(t => t.RawDescription == "TRANSFERENCIA SPEI");
        Assert.Equal(5000.00m, income.Amount); // credit stored as positive
    }

    [Fact]
    public async Task WhenImportingMexicanCsv_ShouldParseDdMmYyyyDates()
    {
        using var db = CreateInMemoryDb();
        var svc = new ImportService(db);
        var csv = "Fecha,Descripcion,Cargo,Abono,Saldo\n25/12/2026,WALMART SUPERCENTER,1234.56,,12045.77\n";

        await svc.ImportCsvAsync(ToCsvStream(csv), "banorte.csv", MxMapping);

        var tx = await db.Transactions.FirstAsync();
        Assert.Equal(new DateTime(2026, 12, 25), tx.Date);
    }

    // ── Deduplication ──────────────────────────────────────────────

    [Fact]
    public async Task WhenImportingSameFileTwice_ShouldDetectDuplicates()
    {
        using var db = CreateInMemoryDb();
        var svc = new ImportService(db);
        var csv = "Date,Description,Amount,Balance\n03/15/2026,COFFEE SHOP,-4.50,995.50\n";

        await svc.ImportCsvAsync(ToCsvStream(csv), "first.csv", UsMapping);
        var second = await svc.ImportCsvAsync(ToCsvStream(csv), "second.csv", UsMapping);

        Assert.Equal(1, second.RowCount);
        Assert.Equal(0, second.ProcessedCount);
        Assert.Equal(1, second.DuplicateCount);
    }

    [Fact]
    public async Task WhenImportingDuplicatesWithinSameBatch_ShouldDeduplicateIntraBatch()
    {
        using var db = CreateInMemoryDb();
        var svc = new ImportService(db);
        var csv = "Date,Description,Amount,Balance\n03/15/2026,COFFEE,-4.50,\n03/15/2026,COFFEE,-4.50,\n";

        var result = await svc.ImportCsvAsync(ToCsvStream(csv), "dupes.csv", UsMapping);

        Assert.Equal(2, result.RowCount);
        Assert.Equal(1, result.ProcessedCount);
        Assert.Equal(1, result.DuplicateCount);
    }

    [Fact]
    public async Task WhenImportingSameAmountDifferentDescription_ShouldNotBeDuplicate()
    {
        using var db = CreateInMemoryDb();
        var svc = new ImportService(db);
        var csv = "Date,Description,Amount,Balance\n03/15/2026,STARBUCKS,-4.50,\n03/15/2026,DUNKIN,-4.50,\n";

        var result = await svc.ImportCsvAsync(ToCsvStream(csv), "coffees.csv", UsMapping);

        Assert.Equal(2, result.ProcessedCount);
        Assert.Equal(0, result.DuplicateCount);
    }

    // ── Hash computation ───────────────────────────────────────────

    [Fact]
    public void WhenComputingHash_ShouldBeConsistentForSameInput()
    {
        var hash1 = ImportService.ComputeHash(UserId, new DateTime(2026, 3, 15), -45.23m, "walmart grocery");
        var hash2 = ImportService.ComputeHash(UserId, new DateTime(2026, 3, 15), -45.23m, "walmart grocery");
        Assert.Equal(hash1, hash2);
    }

    [Fact]
    public void WhenComputingHash_ShouldDifferForDifferentAmounts()
    {
        var hash1 = ImportService.ComputeHash(UserId, new DateTime(2026, 3, 15), -45.23m, "walmart grocery");
        var hash2 = ImportService.ComputeHash(UserId, new DateTime(2026, 3, 15), -99.99m, "walmart grocery");
        Assert.NotEqual(hash1, hash2);
    }

    // ── Raw row storage ────────────────────────────────────────────

    [Fact]
    public async Task WhenImporting_ShouldStoreRawImportRows()
    {
        using var db = CreateInMemoryDb();
        var svc = new ImportService(db);
        var csv = "Date,Description,Amount,Balance\n03/15/2026,LUNCH,-12.00,\n03/16/2026,DINNER,-25.00,\n";

        var result = await svc.ImportCsvAsync(ToCsvStream(csv), "meals.csv", UsMapping);

        var rawRows = await db.RawImportRows.Where(r => r.ImportId == result.Id).OrderBy(r => r.RowNumber).ToListAsync();
        Assert.Equal(2, rawRows.Count);
        Assert.Equal(1, rawRows[0].RowNumber);
        Assert.NotNull(rawRows[0].TransactionId);
        Assert.Contains("LUNCH", rawRows[0].RawData);
    }

    // ── Import record tracking ─────────────────────────────────────

    [Fact]
    public async Task WhenImportCompletes_ShouldUpdateImportRecord()
    {
        using var db = CreateInMemoryDb();
        var svc = new ImportService(db);
        var csv = "Date,Description,Amount,Balance\n03/15/2026,TEST,-10.00,\n";

        var result = await svc.ImportCsvAsync(ToCsvStream(csv), "test.csv", UsMapping);

        var import = await db.Imports.FindAsync(result.Id);
        Assert.NotNull(import);
        Assert.Equal(ImportStatus.Completed, import!.Status);
        Assert.Equal(1, import.RowCount);
        Assert.Equal(1, import.ProcessedCount);
        Assert.Equal("test.csv", import.FileName);
    }

    // ── Error handling ─────────────────────────────────────────────

    [Fact]
    public async Task WhenRowHasInvalidDate_ShouldReportErrorAndContinue()
    {
        using var db = CreateInMemoryDb();
        var svc = new ImportService(db);
        var csv = "Date,Description,Amount,Balance\nNOT-A-DATE,BAD ROW,-10.00,\n03/16/2026,GOOD ROW,-5.00,\n";

        var result = await svc.ImportCsvAsync(ToCsvStream(csv), "mixed.csv", UsMapping);

        Assert.Equal(2, result.RowCount);
        Assert.Equal(1, result.ProcessedCount);
        Assert.Single(result.Errors);
        Assert.Equal(1, result.Errors[0].RowNumber);
        Assert.Contains("date", result.Errors[0].Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WhenRowHasMissingDescription_ShouldReportError()
    {
        using var db = CreateInMemoryDb();
        var svc = new ImportService(db);
        var csv = "Date,Description,Amount,Balance\n03/15/2026,,-10.00,\n";

        var result = await svc.ImportCsvAsync(ToCsvStream(csv), "blank.csv", UsMapping);

        Assert.Equal(1, result.RowCount);
        Assert.Equal(0, result.ProcessedCount);
        Assert.Single(result.Errors);
        Assert.Contains("Description", result.Errors[0].Error);
    }

    [Fact]
    public async Task WhenAllRowsFail_ShouldSetStatusToFailed()
    {
        using var db = CreateInMemoryDb();
        var svc = new ImportService(db);
        var csv = "Date,Description,Amount,Balance\nBAD,,-10.00,\n";

        var result = await svc.ImportCsvAsync(ToCsvStream(csv), "bad.csv", UsMapping);

        Assert.Equal("Failed", result.Status);
        Assert.Equal(0, result.ProcessedCount);
    }

    [Fact]
    public async Task WhenDebitCreditBothEmpty_ShouldReportError()
    {
        using var db = CreateInMemoryDb();
        var svc = new ImportService(db);
        var csv = "Fecha,Descripcion,Cargo,Abono,Saldo\n15/03/2026,OXXO,,,8500.00\n";

        var result = await svc.ImportCsvAsync(ToCsvStream(csv), "empty.csv", MxMapping);

        Assert.Single(result.Errors);
        Assert.Contains("Debit", result.Errors[0].Error, StringComparison.OrdinalIgnoreCase);
    }

    // ── Balance is optional ────────────────────────────────────────

    [Fact]
    public async Task WhenBalanceColumnMissing_ShouldStoreNullBalance()
    {
        using var db = CreateInMemoryDb();
        var svc = new ImportService(db);
        var mapping = new CsvColumnMappingDto
        {
            Date = "Date",
            Description = "Description",
            Amount = "Amount",
            DateFormat = "MM/dd/yyyy"
        };
        var csv = "Date,Description,Amount\n03/15/2026,NO BALANCE,-20.00\n";

        await svc.ImportCsvAsync(ToCsvStream(csv), "nobal.csv", mapping);

        var tx = await db.Transactions.FirstAsync();
        Assert.Null(tx.Balance);
    }

    // ── Quoted fields ──────────────────────────────────────────────

    [Fact]
    public async Task WhenDescriptionContainsComma_ShouldParseCorrectly()
    {
        using var db = CreateInMemoryDb();
        var svc = new ImportService(db);
        var csv = "Date,Description,Amount,Balance\n03/15/2026,\"WALMART, SUPERCENTER\",-45.23,1204.77\n";

        var result = await svc.ImportCsvAsync(ToCsvStream(csv), "quoted.csv", UsMapping);

        Assert.Equal(1, result.ProcessedCount);
        var tx = await db.Transactions.FirstAsync();
        Assert.Equal("WALMART, SUPERCENTER", tx.RawDescription);
    }

    // ── Field-level error reporting ────────────────────────────────

    [Fact]
    public async Task WhenRowHasInvalidDate_ShouldIncludeFieldNameInError()
    {
        using var db = CreateInMemoryDb();
        var svc = new ImportService(db);
        var csv = "Date,Description,Amount,Balance\nBADDATE,GOOD,-10.00,\n";

        var result = await svc.ImportCsvAsync(ToCsvStream(csv), "field-error.csv", UsMapping);

        Assert.Single(result.Errors);
        var error = result.Errors[0];
        Assert.Equal(1, error.RowNumber);
        Assert.Equal("Date", error.Field);
        Assert.Contains("date", error.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WhenRowHasInvalidAmount_ShouldIncludeFieldNameInError()
    {
        using var db = CreateInMemoryDb();
        var svc = new ImportService(db);
        var csv = "Date,Description,Amount,Balance\n03/15/2026,ITEM,NOTANUMBER,\n";

        var result = await svc.ImportCsvAsync(ToCsvStream(csv), "amount-error.csv", UsMapping);

        Assert.Single(result.Errors);
        var error = result.Errors[0];
        Assert.Equal(1, error.RowNumber);
        Assert.Equal("Amount", error.Field);
        Assert.Contains("amount", error.Error, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task WhenRowHasMissingDescription_ShouldIncludeFieldNameInError()
    {
        using var db = CreateInMemoryDb();
        var svc = new ImportService(db);
        var csv = "Date,Description,Amount,Balance\n03/15/2026,,-10.00,\n";

        var result = await svc.ImportCsvAsync(ToCsvStream(csv), "description-error.csv", UsMapping);

        Assert.Single(result.Errors);
        var error = result.Errors[0];
        Assert.Equal(1, error.RowNumber);
        Assert.Equal("Description", error.Field);
    }

    [Fact]
    public async Task WhenInvalidDebitAmount_ShouldIncludeDebitFieldNameInError()
    {
        using var db = CreateInMemoryDb();
        var svc = new ImportService(db);
        var csv = "Fecha,Descripcion,Cargo,Abono,Saldo\n15/03/2026,OXXO,NOTANUMBER,,8500.00\n";

        var result = await svc.ImportCsvAsync(ToCsvStream(csv), "debit-error.csv", MxMapping);

        Assert.Single(result.Errors);
        var error = result.Errors[0];
        Assert.Equal("Debit", error.Field);
    }

    [Fact]
    public async Task WhenInvalidCreditAmount_ShouldIncludeCreditFieldNameInError()
    {
        using var db = CreateInMemoryDb();
        var svc = new ImportService(db);
        var csv = "Fecha,Descripcion,Cargo,Abono,Saldo\n15/03/2026,TRANSFER,,NOTANUMBER,8500.00\n";

        var result = await svc.ImportCsvAsync(ToCsvStream(csv), "credit-error.csv", MxMapping);

        Assert.Single(result.Errors);
        var error = result.Errors[0];
        Assert.Equal("Credit", error.Field);
    }

    // ── Column validation (upfront) ────────────────────────────────

    [Fact]
    public async Task WhenRequiredColumnIsMissing_ShouldReportHeaderError()
    {
        using var db = CreateInMemoryDb();
        var svc = new ImportService(db);
        var csv = "Date,Amount,Balance\n03/15/2026,-10.00,\n"; // Missing Description

        var result = await svc.ImportCsvAsync(ToCsvStream(csv), "missing-column.csv", UsMapping);

        Assert.Equal("Failed", result.Status);
        Assert.Equal(0, result.RowCount); // No rows processed
        Assert.Single(result.Errors);
        var error = result.Errors[0];
        Assert.Equal(0, error.RowNumber); // Header error
        Assert.Equal("Header", error.Field);
        Assert.Contains("Missing required columns", error.Error);
        Assert.Contains("Description", error.Error);
    }

    [Fact]
    public async Task WhenMultipleColumnsAreMissing_ShouldReportAllMissingColumns()
    {
        using var db = CreateInMemoryDb();
        var svc = new ImportService(db);
        var csv = "Amount,Balance\n-10.00,\n"; // Missing Date and Description

        var result = await svc.ImportCsvAsync(ToCsvStream(csv), "missing-multiple.csv", UsMapping);

        Assert.Equal("Failed", result.Status);
        Assert.Single(result.Errors);
        var error = result.Errors[0];
        Assert.Contains("Date", error.Error);
        Assert.Contains("Description", error.Error);
    }

    [Fact]
    public async Task WhenAmountColumnMissing_ShouldReportErrorIfNoDebitCredit()
    {
        using var db = CreateInMemoryDb();
        var svc = new ImportService(db);
        var csv = "Date,Description,Balance\n03/15/2026,ITEM,\n"; // Missing Amount, Debit, Credit

        var result = await svc.ImportCsvAsync(ToCsvStream(csv), "missing-amount.csv", UsMapping);

        Assert.Equal("Failed", result.Status);
        Assert.Single(result.Errors);
        var error = result.Errors[0];
        Assert.Contains("Amount", error.Error);
    }

    [Fact]
    public async Task WhenColumnNameIsCaseInsensitive_ShouldMatch()
    {
        using var db = CreateInMemoryDb();
        var svc = new ImportService(db);
        var csv = "date,description,amount,balance\n03/15/2026,ITEM,-10.00,\n"; // lowercase headers

        var result = await svc.ImportCsvAsync(ToCsvStream(csv), "lowercase.csv", UsMapping);

        Assert.Equal("Completed", result.Status);
        Assert.Equal(1, result.ProcessedCount);
        Assert.Empty(result.Errors);
    }

    // ── Silent duplicate handling ──────────────────────────────────

    [Fact]
    public async Task WhenImportingPreviouslySubmittedFile_ShouldSilentlySkipDuplicates()
    {
        using var db = CreateInMemoryDb();
        var svc = new ImportService(db);
        var csv = "Date,Description,Amount,Balance\n03/15/2026,COFFEE,-4.50,\n";

        var firstImport = await svc.ImportCsvAsync(ToCsvStream(csv), "first.csv", UsMapping);
        Assert.Equal(1, firstImport.ProcessedCount);
        Assert.Equal(0, firstImport.DuplicateCount);

        // Re-upload the same file
        var secondImport = await svc.ImportCsvAsync(ToCsvStream(csv), "second.csv", UsMapping);
        Assert.Equal(1, secondImport.RowCount);
        Assert.Equal(0, secondImport.ProcessedCount); // Silently skipped
        Assert.Equal(1, secondImport.DuplicateCount);
        Assert.Empty(secondImport.Errors); // No errors, just silently skipped
    }

    [Fact]
    public async Task WhenUploadingPartiallyDuplicateFile_ShouldSkipDuplicatesAndProcessNew()
    {
        using var db = CreateInMemoryDb();
        var svc = new ImportService(db);
        var firstCsv = "Date,Description,Amount,Balance\n03/15/2026,OLD COFFEE,-4.50,\n";
        var secondCsv = "Date,Description,Amount,Balance\n03/15/2026,OLD COFFEE,-4.50,\n03/16/2026,NEW COFFEE,-5.00,\n";

        await svc.ImportCsvAsync(ToCsvStream(firstCsv), "first.csv", UsMapping);
        var secondImport = await svc.ImportCsvAsync(ToCsvStream(secondCsv), "second.csv", UsMapping);

        Assert.Equal(2, secondImport.RowCount);
        Assert.Equal(1, secondImport.ProcessedCount); // Only new one
        Assert.Equal(1, secondImport.DuplicateCount); // Only old one
        Assert.Empty(secondImport.Errors);
    }
}
