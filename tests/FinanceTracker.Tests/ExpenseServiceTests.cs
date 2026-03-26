using Xunit;
using FinanceTracker.API.Services;
using FinanceTracker.API.Models;
using System.Threading.Tasks;

namespace FinanceTracker.Tests;

public class ExpenseServiceTests
{
    [Fact]
    public async Task CreateAndGetExpense()
    {
        var svc = new ExpenseService();
        var exp = new Expense { Amount = 12.34m, Description = "Lunch", Date = System.DateTime.UtcNow };

        var created = await svc.CreateAsync(exp);
        Assert.NotNull(created);
        Assert.Equal(1, created.Id);

        var fetched = await svc.GetByIdAsync(created.Id);
        Assert.NotNull(fetched);
        Assert.Equal(12.34m, fetched!.Amount);
    }
}
