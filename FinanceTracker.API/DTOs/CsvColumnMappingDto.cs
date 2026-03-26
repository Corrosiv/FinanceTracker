namespace FinanceTracker.API.DTOs;

public class CsvColumnMappingDto
{
    public string Date { get; set; } = null!;
    public string Description { get; set; } = null!;
    public string? Amount { get; set; }
    public string? Debit { get; set; }
    public string? Credit { get; set; }
    public string? Balance { get; set; }
    public string? DateFormat { get; set; }
    public string? Culture { get; set; }
}
