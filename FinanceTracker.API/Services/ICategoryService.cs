using FinanceTracker.API.Models;

namespace FinanceTracker.API.Services;

public interface ICategoryService
{
    Task<Category?> GetByIdAsync(Guid id);
    Task<IEnumerable<Category>> GetAllAsync();
    Task<Category> CreateAsync(Category category);
    Task<Category?> UpdateAsync(Guid id, string? name, string? description);
    Task<bool> DeleteAsync(Guid id);
}
