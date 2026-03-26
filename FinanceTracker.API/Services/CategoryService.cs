using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FinanceTracker.API.Models;

namespace FinanceTracker.API.Services
{
    public class CategoryService : ICategoryService
    {
        private readonly List<Category> _store = new();

        public Task<Category> CreateAsync(Category category)
        {
            category.Id = Guid.NewGuid();
            _store.Add(category);
            return Task.FromResult(category);
        }

        public Task<IEnumerable<Category>> GetAllAsync()
        {
            return Task.FromResult<IEnumerable<Category>>(_store);
        }

        public Task<Category?> GetByIdAsync(Guid id)
        {
            var c = _store.FirstOrDefault(x => x.Id == id);
            return Task.FromResult(c);
        }
    }
}
