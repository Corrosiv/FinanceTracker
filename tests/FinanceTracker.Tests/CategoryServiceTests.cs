using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using FinanceTracker.API.Data;
using FinanceTracker.API.Models;
using FinanceTracker.API.Services;

namespace FinanceTracker.Tests;

public class CategoryServiceTests
{
    private static FinanceDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new FinanceDbContext(options);
        db.Users.Add(new User { Id = Guid.Parse("00000000-0000-0000-0000-000000000001"), Name = "Test", CreatedAt = DateTime.UtcNow });
        db.SaveChanges();
        return db;
    }

    [Fact]
    public async Task CreateAndGetCategory()
    {
        using var db = CreateInMemoryDb();
        var svc = new CategoryService(db);
        var cat = new Category
        {
            UserId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            Name = "Food",
            Description = "Groceries"
        };

        var created = await svc.CreateAsync(cat);
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.Id);

        var fetched = await svc.GetByIdAsync(created.Id);
        Assert.NotNull(fetched);
        Assert.Equal("Food", fetched!.Name);
    }

    [Fact]
    public async Task GetAllReturnsCategories()
    {
        using var db = CreateInMemoryDb();
        var svc = new CategoryService(db);
        var userId = Guid.Parse("00000000-0000-0000-0000-000000000001");

        await svc.CreateAsync(new Category { UserId = userId, Name = "Food" });
        await svc.CreateAsync(new Category { UserId = userId, Name = "Transport" });

        var all = (await svc.GetAllAsync()).ToList();
        Assert.Equal(2, all.Count);
    }

    [Fact]
    public async Task DeleteRemovesCategory()
    {
        using var db = CreateInMemoryDb();
        var svc = new CategoryService(db);
        var cat = await svc.CreateAsync(new Category
        {
            UserId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            Name = "ToDelete"
        });

        var deleted = await svc.DeleteAsync(cat.Id);
        Assert.True(deleted);
        Assert.Null(await svc.GetByIdAsync(cat.Id));
    }

    [Fact]
    public async Task DeleteNonExistentReturnsFalse()
    {
        using var db = CreateInMemoryDb();
        var svc = new CategoryService(db);
        Assert.False(await svc.DeleteAsync(Guid.NewGuid()));
    }
}
