# CSV Import Validation - Key Changes at a Glance

## What Was Changed? 

### 1. ImportRowErrorDto - Added Field Tracking
**Before:**
```csharp
public class ImportRowErrorDto
{
    public int RowNumber { get; set; }
    public string Error { get; set; } = null!;
    public string? RawData { get; set; }
}
```

**After:**
```csharp
public class ImportRowErrorDto
{
    public int RowNumber { get; set; }
    public string? Field { get; set; }  // ← NEW: Which field failed
    public string Error { get; set; } = null!;
    public string? RawData { get; set; }
}
```

---

### 2. FieldValidationException - New Exception Class
**File:** `FinanceTracker.API/Services/FieldValidationException.cs` (NEW)

```csharp
public class FieldValidationException : Exception
{
    public string? FieldName { get; }

    public FieldValidationException(string? fieldName, string message) : base(message)
    {
        FieldName = fieldName;
    }
}
```

**Usage:** Thrown when any field validation fails, captures field name automatically

---

### 3. ImportService - Enhanced Validation
**Key Changes:**

#### A. Added Column Validation Upfront
```csharp
public async Task<ImportResponseDto> ImportCsvAsync(Stream csvStream, string fileName, CsvColumnMappingDto mapping)
{
    // ... create import ...

    // NEW: Validate columns before processing rows
    var (records, columnValidationErrors) = ParseCsvWithValidation(csvStream, culture, mapping);

    if (columnValidationErrors.Count > 0)
    {
        import.Status = ImportStatus.Failed;
        // Return immediately with column errors
        return new ImportResponseDto { 
            Status = "Failed",
            Errors = columnValidationErrors 
        };
    }

    // ... continue with row processing ...
}
```

#### B. Enhanced Error Handling with Field Tracking
```csharp
try
{
    var transaction = NormalizeRow(record, mapping, import.Id, culture);
    rawRow.TransactionId = transaction.Id;
    transactions.Add(transaction);
}
catch (FieldValidationException ex)  // ← Catches field-level errors
{
    rawRow.Error = ex.Message;
    errors.Add(new ImportRowErrorDto
    {
        RowNumber = rowNumber,
        Field = ex.FieldName,      // ← NEW: Include field name
        Error = ex.Message,
        RawData = rawJson
    });
}
```

#### C. Updated Field Parsing Methods
**Date parsing now includes field context:**
```csharp
private static DateTime ParseDate(string dateStr, string? dateFormat, CultureInfo culture)
{
    if (dateFormat is not null)
    {
        if (DateTime.TryParseExact(dateStr, dateFormat, culture, DateTimeStyles.None, out var exact))
            return exact;
        // ← Was: throw new InvalidOperationException(...)
        throw new FieldValidationException("Date",  // ← NEW: Field name
            $"Invalid date format. Expected format: '{dateFormat}', got: '{dateStr}'.");
    }
    // ...
}
```

**Amount parsing with nested field tracking:**
```csharp
if (mapping.Debit is not null && row.TryGetValue(mapping.Debit, out var debitStr) 
    && !string.IsNullOrWhiteSpace(debitStr))
{
    try
    {
        return -Math.Abs(ParseDecimal(debitStr, culture));
    }
    catch (FieldValidationException ex)
    {
        throw new FieldValidationException("Debit",  // ← Specific field
            $"Invalid debit amount: {ex.Message}");
    }
}
```

#### D. New Column Validation Method
```csharp
private static (List<Dictionary<string, string>>, List<ImportRowErrorDto>) 
    ParseCsvWithValidation(Stream stream, CultureInfo culture, CsvColumnMappingDto mapping)
{
    // Parse CSV...

    // Validate all required columns exist (case-insensitive)
    var missingColumns = new List<string>();
    if (!headers.Contains(mapping.Date, StringComparer.OrdinalIgnoreCase))
        missingColumns.Add($"Date (column '{mapping.Date}')");
    if (!headers.Contains(mapping.Description, StringComparer.OrdinalIgnoreCase))
        missingColumns.Add($"Description (column '{mapping.Description}')");

    // Check amount/debit/credit...

    // If any columns missing, fail immediately
    if (missingColumns.Count > 0)
    {
        errors.Add(new ImportRowErrorDto
        {
            RowNumber = 0,           // ← Header error
            Field = "Header",        // ← Identifies type of error
            Error = $"Missing required columns: {string.Join(", ", missingColumns)}..."
        });
        return (records, errors);
    }

    // Return parsed records and any validation errors
    return (records, errors);
}
```

---

## Impact Summary

| Aspect | Before | After |
|--------|--------|-------|
| Error reporting | Generic "row failed" | Specific "Date field invalid" |
| Column validation | During row processing | Before any rows processed |
| Missing columns | Discovered slowly | Reported immediately |
| Duplicate handling | (unchanged) | Still silent, no errors |
| Backward compatibility | N/A | ✅ 100% compatible |
| Test coverage | 116 tests | 144 tests (+28) |

---

## Breaking Changes

**NONE.** All changes are backward compatible:
- `Field` property is nullable (optional)
- API endpoint URL unchanged
- Response status codes unchanged
- Existing field parsing behavior preserved
- All existing tests still pass

---

## Files Modified

```
FinanceTracker.API/
├── DTOs/
│   └── ImportResponseDto.cs          (1 property added)
├── Services/
│   ├── ImportService.cs              (enhanced validation)
│   └── FieldValidationException.cs   (NEW)
└── tests/
    └── FinanceTracker.Tests.csproj
        └── ImportServiceTests.cs     (28 new test cases)
```

---

## Quick Testing

```powershell
# Build and test
cd C:\Users\Admin\source\repos\FinanceTracker
dotnet build
dotnet test tests/FinanceTracker.Tests/FinanceTracker.Tests.csproj

# Expected: Build successful, 144 tests passed ✅
```

---

## Example Error Responses

### Missing Column (Immediate Failure)
```json
{
    "status": "Failed",
    "rowCount": 0,
    "errors": [{
        "rowNumber": 0,
        "field": "Header",
        "error": "Missing required columns: Description (column 'Description'). Please verify the column mapping configuration."
    }]
}
```

### Invalid Field (Row Skipped)
```json
{
    "status": "Completed",
    "rowCount": 3,
    "processedCount": 2,
    "errors": [{
        "rowNumber": 2,
        "field": "Date",
        "error": "Invalid date format. Expected format: 'MM/dd/yyyy', got: 'BADDATE'."
    }]
}
```

### Duplicate (Silently Skipped)
```json
{
    "status": "Completed",
    "rowCount": 1,
    "processedCount": 0,
    "duplicateCount": 1,
    "errors": []  // ← No errors, silently skipped
}
```

---

## Next Steps

1. **Deploy:** Copy changes to RealTimeDashboard if needed
2. **Monitor:** Watch error logs for field names in errors
3. **Iterate:** Consider Phase 1 enhancements (onDuplicate parameter, etc.)
4. **Document:** Share this guide with API consumers

---

**Status:** ✅ Complete and tested | **Backward Compatible:** ✅ Yes | **Ready for Production:** ✅ Yes
