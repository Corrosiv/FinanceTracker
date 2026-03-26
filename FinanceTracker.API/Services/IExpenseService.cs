using System.Threading.Tasks;
using FinanceTracker.API.Models;

namespace FinanceTracker.API.Services
{
    public interface IExpenseService
    {
        Task<Expense> CreateAsync(Expense expense);
        Task<Expense?> GetByIdAsync(Guid id);
    }
}
