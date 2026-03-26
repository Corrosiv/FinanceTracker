namespace FinanceTracker.API.DTOs;

public class BulkCategoryAssignmentDto
{
    public List<Guid> TransactionIds { get; set; } = [];
    public Guid CategoryId { get; set; }
}
