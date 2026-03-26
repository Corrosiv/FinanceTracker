namespace FinanceTracker.API.DTOs;

public class BulkCategoryAssignmentResponseDto
{
    public int UpdatedCount { get; set; }
    public List<Guid> NotFoundIds { get; set; } = [];
}
