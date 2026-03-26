using FinanceTracker.API.Models;

namespace FinanceTracker.API.Services;

public interface IExpenseService
{
    Task<Transaction> CreateAsync(Transaction transaction);
    Task<Transaction?> GetByIdAsync(Guid id);
    Task<IEnumerable<Transaction>> GetAllAsync();
    Task<Transaction?> UpdateAsync(Guid id, decimal? amount, string? description, DateTime? date, Guid? categoryId);
    Task<bool> DeleteAsync(Guid id);
}
