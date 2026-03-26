using Xunit;
using FinanceTracker.API.Services;
using FinanceTracker.API.Models;
using System.Threading.Tasks;

namespace FinanceTracker.Tests;

public class CategoryServiceTests
{
    [Fact]
    public async Task CreateAndGetCategory()
    {
        var svc = new CategoryService();
        var cat = new Category { Name = "Food", Description = "Groceries" };

        var created = await svc.CreateAsync(cat);
        Assert.NotNull(created);
        Assert.Equal(1, created.Id);

        var fetched = await svc.GetByIdAsync(created.Id);
        Assert.NotNull(fetched);
        Assert.Equal("Food", fetched!.Name);
    }
}
