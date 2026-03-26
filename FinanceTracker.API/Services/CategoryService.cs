using Microsoft.EntityFrameworkCore;
using FinanceTracker.API.Data;
using FinanceTracker.API.Models;

namespace FinanceTracker.API.Services;

public class CategoryService : ICategoryService
{
    private readonly FinanceDbContext _db;

    public CategoryService(FinanceDbContext db)
    {
        _db = db;
    }

    public async Task<Category> CreateAsync(Category category)
    {
        category.Id = Guid.NewGuid();
        category.CreatedAt = DateTime.UtcNow;
        _db.Categories.Add(category);
        await _db.SaveChangesAsync();
        return category;
    }

    public async Task<IEnumerable<Category>> GetAllAsync()
    {
        return await _db.Categories.AsNoTracking().ToListAsync();
    }

    public async Task<Category?> GetByIdAsync(Guid id)
    {
        return await _db.Categories.FindAsync(id);
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var category = await _db.Categories.FindAsync(id);
        if (category is null) return false;
        _db.Categories.Remove(category);
        await _db.SaveChangesAsync();
        return true;
    }
}
