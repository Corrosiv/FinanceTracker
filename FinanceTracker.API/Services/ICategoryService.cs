using System.Collections.Generic;
using System.Threading.Tasks;
using FinanceTracker.API.Models;

namespace FinanceTracker.API.Services
{
    public interface ICategoryService
    {
        Task<Category?> GetByIdAsync(Guid id);
        Task<IEnumerable<Category>> GetAllAsync();
        Task<Category> CreateAsync(Category category);
    }
}
