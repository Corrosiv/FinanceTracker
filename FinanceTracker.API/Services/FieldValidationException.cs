namespace FinanceTracker.API.Services;

/// <summary>
/// Exception thrown when a specific field fails validation during CSV import.
/// Includes the field name for structured error reporting.
/// </summary>
public class FieldValidationException : Exception
{
    public string? FieldName { get; }

    public FieldValidationException(string? fieldName, string message) : base(message)
    {
        FieldName = fieldName;
    }
}
