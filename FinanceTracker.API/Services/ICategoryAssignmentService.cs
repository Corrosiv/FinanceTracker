using FinanceTracker.API.DTOs;

namespace FinanceTracker.API.Services;

public interface ICategoryAssignmentService
{
    Task<BulkCategoryAssignmentResponseDto> AssignCategoryAsync(Guid userId, List<Guid> transactionIds, Guid categoryId);
}
