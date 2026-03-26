namespace FinanceTracker.API.Models;

public enum ImportStatus
{
    Pending,
    Processing,
    Completed,
    Failed
}

public class Import
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string FileName { get; set; } = null!;
    public DateTime UploadDate { get; set; }
    public int RowCount { get; set; }
    public int ProcessedCount { get; set; }
    public int DuplicateCount { get; set; }
    public ImportStatus Status { get; set; }
    public string? ColumnMapping { get; set; }

    public User User { get; set; } = null!;
    public ICollection<Transaction> Transactions { get; set; } = [];
}
