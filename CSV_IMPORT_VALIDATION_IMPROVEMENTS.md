# CSV Import Validation Improvements

## Overview
Enhanced CSV import validation in FinanceTracker.API to provide **field-level error reporting**, **upfront column validation**, and **silent duplicate handling** for transaction reports and re-uploads.

## Changes Made

### 1. Enhanced Error Reporting (DTO)
**File:** `FinanceTracker.API/DTOs/ImportResponseDto.cs`

Added a `Field` property to `ImportRowErrorDto` to track which field failed validation:

```csharp
public class ImportRowErrorDto
{
    public int RowNumber { get; set; }
    public string? Field { get; set; }      // NEW: Track which field failed
    public string Error { get; set; } = null!;
    public string? RawData { get; set; }
}
```

**Benefits:**
- Users no longer have to guess which field has an error
- Structured error reporting enables client-side UI improvements
- Facilitates better error handling and debugging

### 2. Custom Exception for Field Validation
**File:** `FinanceTracker.API/Services/FieldValidationException.cs` (NEW)

Created a dedicated exception class to track field-level failures:

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

**Benefits:**
- Clean separation between field-level and row-level errors
- Field name is automatically captured and reported
- Easy to identify and distinguish from parsing errors

### 3. Upfront Column Validation
**File:** `FinanceTracker.API/Services/ImportService.cs`

Added `ParseCsvWithValidation()` method that validates required columns **before** processing any rows:

```csharp
private static (List<Dictionary<string, string>>, List<ImportRowErrorDto>) 
    ParseCsvWithValidation(Stream stream, CultureInfo culture, CsvColumnMappingDto mapping)
{
    // ... CSV parsing ...

    // Validate all required columns exist
    var missingColumns = new List<string>();
    if (!headers.Contains(mapping.Date, StringComparer.OrdinalIgnoreCase))
        missingColumns.Add($"Date (column '{mapping.Date}')");
    if (!headers.Contains(mapping.Description, StringComparer.OrdinalIgnoreCase))
        missingColumns.Add($"Description (column '{mapping.Description}')");

    // Check amount/debit/credit columns...

    if (missingColumns.Count > 0)
    {
        errors.Add(new ImportRowErrorDto
        {
            RowNumber = 0,
            Field = "Header",
            Error = $"Missing required columns: {string.Join(", ", missingColumns)}..."
        });
        return (records, errors);
    }

    // ... continue with row processing ...
}
```

**Features:**
- **Case-insensitive matching:** Column names don't need to match case exactly
- **Clear error messages:** Lists all missing columns for easy correction
- **Early termination:** Fails immediately if columns are missing, avoiding wasted processing
- **Header error identification:** Uses `RowNumber = 0` and `Field = "Header"` for column errors

### 4. Enhanced Field-Level Error Messages
Updated parsing methods to throw `FieldValidationException` with descriptive messages:

**Date parsing:**
```csharp
private static DateTime ParseDate(string dateStr, string? dateFormat, CultureInfo culture)
{
    if (dateFormat is not null)
    {
        if (DateTime.TryParseExact(dateStr, dateFormat, culture, DateTimeStyles.None, out var exact))
            return exact;
        throw new FieldValidationException("Date", 
            $"Invalid date format. Expected format: '{dateFormat}', got: '{dateStr}'.");
    }

    if (DateTime.TryParse(dateStr, culture, DateTimeStyles.None, out var parsed))
        return parsed;
    throw new FieldValidationException("Date", 
        $"Cannot parse date '{dateStr}'. Please check the date value and format.");
}
```

**Amount parsing:**
```csharp
private static decimal ParseDecimal(string value, CultureInfo culture)
{
    var cleaned = value
        .Replace("$", "")
        .Replace("€", "")
        // ... strip symbols ...
        .Trim();

    if (decimal.TryParse(cleaned, NumberStyles.Number | NumberStyles.AllowLeadingSign, culture, out var result))
        return result;

    throw new FieldValidationException("Amount", 
        $"Cannot parse amount '{value}' as a decimal number. Please check the value.");
}
```

**Debit/Credit parsing:**
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
        throw new FieldValidationException("Debit", $"Invalid debit amount: {ex.Message}");
    }
}
```

### 5. Silent Duplicate Handling
**Existing functionality, verified by new tests**

Duplicates are **silently skipped** without error reporting:

```csharp
foreach (var tx in transactions)
{
    if (existingHashSet.Contains(tx.DeduplicationHash!) || !seenHashes.Add(tx.DeduplicationHash!))
    {
        duplicateCount++;  // Count duplicates
        var rawRow = rawRows.First(r => r.TransactionId == tx.Id);
        rawRow.TransactionId = null;
        rawRow.Error = "Duplicate transaction.";  // Logged, not reported as error
    }
    else
    {
        uniqueTransactions.Add(tx);
    }
}
```

**Benefits:**
- Users can re-upload files without seeing error messages
- Idempotent API: same file can be uploaded multiple times safely
- Deduplication hash includes: `userId | date | amount | normalizedDescription`

## API Response Examples

### Example 1: Missing Columns
**Request:** CSV missing "Description" column
**Response (400 Bad Request):**
```json
{
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "fileName": "incomplete.csv",
    "status": "Failed",
    "rowCount": 0,
    "processedCount": 0,
    "duplicateCount": 0,
    "errors": [
        {
            "rowNumber": 0,
            "field": "Header",
            "error": "Missing required columns: Description (column 'Description'). Please verify the column mapping configuration.",
            "rawData": null
        }
    ]
}
```

### Example 2: Field-Level Errors with Mixed Valid/Invalid Rows
**Request:** CSV with 1 bad date and 1 bad amount, plus 1 good row
**Response (200 OK with partial results):**
```json
{
    "id": "550e8400-e29b-41d4-a716-446655440001",
    "fileName": "mixed.csv",
    "status": "Completed",
    "rowCount": 3,
    "processedCount": 1,
    "duplicateCount": 0,
    "errors": [
        {
            "rowNumber": 1,
            "field": "Date",
            "error": "Invalid date format. Expected format: 'MM/dd/yyyy', got: 'BADDATE'.",
            "rawData": "{\"Date\":\"BADDATE\",\"Description\":\"Row 1\",\"Amount\":\"-10.00\"}"
        },
        {
            "rowNumber": 2,
            "field": "Amount",
            "error": "Cannot parse amount 'NOTANUMBER' as a decimal number. Please check the value.",
            "rawData": "{\"Date\":\"03/16/2026\",\"Description\":\"Row 2\",\"Amount\":\"NOTANUMBER\"}"
        }
    ]
}
```

### Example 3: Silent Duplicate Handling
**First upload:**
```json
{
    "status": "Completed",
    "rowCount": 1,
    "processedCount": 1,
    "duplicateCount": 0,
    "errors": []
}
```

**Second upload (same file):**
```json
{
    "status": "Completed",
    "rowCount": 1,
    "processedCount": 0,
    "duplicateCount": 1,
    "errors": []  // NO ERRORS - silently skipped
}
```

## Test Coverage

### New Tests Added (28 tests total, all passing)
All tests are in `tests/FinanceTracker.Tests/ImportServiceTests.cs`

**Field-level error reporting tests:**
- ✅ `WhenRowHasInvalidDate_ShouldIncludeFieldNameInError` - Date field errors
- ✅ `WhenRowHasInvalidAmount_ShouldIncludeFieldNameInError` - Amount field errors
- ✅ `WhenRowHasMissingDescription_ShouldIncludeFieldNameInError` - Missing field errors
- ✅ `WhenInvalidDebitAmount_ShouldIncludeDebitFieldNameInError` - Debit field errors
- ✅ `WhenInvalidCreditAmount_ShouldIncludeCreditFieldNameInError` - Credit field errors

**Column validation tests:**
- ✅ `WhenRequiredColumnIsMissing_ShouldReportHeaderError` - Missing single column
- ✅ `WhenMultipleColumnsAreMissing_ShouldReportAllMissingColumns` - Missing multiple columns
- ✅ `WhenAmountColumnMissing_ShouldReportErrorIfNoDebitCredit` - Missing amount pattern
- ✅ `WhenColumnNameIsCaseInsensitive_ShouldMatch` - Case-insensitive column matching

**Silent duplicate tests:**
- ✅ `WhenImportingPreviouslySubmittedFile_ShouldSilentlySkipDuplicates` - Re-upload same file
- ✅ `WhenUploadingPartiallyDuplicateFile_ShouldSkipDuplicatesAndProcessNew` - Partial re-upload

**All existing tests (116) continue to pass**, validating backward compatibility.

## Breaking Changes

**None.** This update is **fully backward compatible**:
- The `Field` property in `ImportRowErrorDto` is nullable (optional)
- Existing error handling code continues to work
- API behavior for successful imports is unchanged
- Duplicate detection logic is unchanged

## Usage Recommendations

### For API Clients
1. **Check the `field` property** in error responses to identify which column needs fixing
2. **Validate column mappings early** - the API will now fail immediately if columns don't exist
3. **No need to handle duplicate errors** - they're silently skipped, making re-uploads safe
4. **Use raw data for debugging** - the `rawData` field contains the original parsed row

### For Future Enhancements
- Consider adding `onDuplicate=merge|skip|error` parameter (Phase 1 TODO)
- Add streaming support for large files (Phase 1 TODO)
- Implement import templates/profiles (Phase 2 TODO)

## Files Modified
1. `FinanceTracker.API/DTOs/ImportResponseDto.cs` - Added `Field` property
2. `FinanceTracker.API/Services/ImportService.cs` - Enhanced validation and error handling
3. `FinanceTracker.API/Services/FieldValidationException.cs` - NEW exception class
4. `tests/FinanceTracker.Tests/ImportServiceTests.cs` - Added 28 new test cases

## Test Results
```
Total Tests: 144
Passed: 144 ✅
Failed: 0
Skipped: 0
Duration: ~1 second
```

All tests passing, including:
- ✅ Existing CSV parsing tests (backward compatible)
- ✅ Deduplication tests
- ✅ Culture-aware date/amount parsing
- ✅ Quoted field handling
- ✅ **NEW** Field-level error reporting
- ✅ **NEW** Column validation
- ✅ **NEW** Silent duplicate handling
