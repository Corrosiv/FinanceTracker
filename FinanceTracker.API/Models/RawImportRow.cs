namespace FinanceTracker.API.Models;

public class RawImportRow
{
    public Guid Id { get; set; }
    public Guid ImportId { get; set; }
    public int RowNumber { get; set; }
    public string RawData { get; set; } = null!;
    public Guid? TransactionId { get; set; }
    public string? Error { get; set; }

    public Import Import { get; set; } = null!;
    public Transaction? Transaction { get; set; }
}
