using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FinanceTracker.API.Models;

namespace FinanceTracker.API.Services
{
    public class ExpenseService : IExpenseService
    {
        private readonly List<Expense> _store = new();

        public Task<Expense> CreateAsync(Expense expense)
        {
            expense.Id = _store.Count + 1;
            _store.Add(expense);
            return Task.FromResult(expense);
        }

        public Task<Expense?> GetByIdAsync(int id)
        {
            var e = _store.FirstOrDefault(x => x.Id == id);
            return Task.FromResult(e);
        }
    }
}
