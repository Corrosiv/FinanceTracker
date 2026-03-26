using Microsoft.EntityFrameworkCore;
using FinanceTracker.API.Data;
using FinanceTracker.API.DTOs;

namespace FinanceTracker.API.Services;

public class CategoryAssignmentService : ICategoryAssignmentService
{
    private readonly FinanceDbContext _db;

    public CategoryAssignmentService(FinanceDbContext db)
    {
        _db = db;
    }

    public async Task<BulkCategoryAssignmentResponseDto> AssignCategoryAsync(
        Guid userId, List<Guid> transactionIds, Guid categoryId)
    {
        var transactions = await _db.Transactions
            .Where(t => t.UserId == userId && transactionIds.Contains(t.Id))
            .ToListAsync();

        var foundIds = transactions.Select(t => t.Id).ToHashSet();
        var notFoundIds = transactionIds.Where(id => !foundIds.Contains(id)).ToList();

        foreach (var tx in transactions)
        {
            tx.CategoryId = categoryId;
        }

        await _db.SaveChangesAsync();

        return new BulkCategoryAssignmentResponseDto
        {
            UpdatedCount = transactions.Count,
            NotFoundIds = notFoundIds
        };
    }
}
