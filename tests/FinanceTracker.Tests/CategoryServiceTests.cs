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
    public async Task WhenCreatingCategory_ShouldPersistAndReturnWithId()
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
    public async Task WhenGettingAll_ShouldReturnAllCategories()
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
    public async Task WhenDeletingExistingCategory_ShouldRemoveFromDatabase()
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
    public async Task WhenDeletingNonExistentCategory_ShouldReturnFalse()
    {
        using var db = CreateInMemoryDb();
        var svc = new CategoryService(db);
        Assert.False(await svc.DeleteAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task WhenGettingNonExistentCategory_ShouldReturnNull()
    {
        using var db = CreateInMemoryDb();
        var svc = new CategoryService(db);
        var result = await svc.GetByIdAsync(Guid.NewGuid());
        Assert.Null(result);
    }

    [Fact]
    public async Task WhenDeletingSameCategoryTwice_SecondDeleteShouldReturnFalse()
    {
        using var db = CreateInMemoryDb();
        var svc = new CategoryService(db);
        var cat = await svc.CreateAsync(new Category
        {
            UserId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            Name = "DoubleDeleteCat"
        });

        Assert.True(await svc.DeleteAsync(cat.Id));
        Assert.False(await svc.DeleteAsync(cat.Id));
    }

    [Fact]
    public async Task WhenUpdatingName_ShouldOnlyChangeName()
    {
        using var db = CreateInMemoryDb();
        var svc = new CategoryService(db);
        var cat = await svc.CreateAsync(new Category
        {
            UserId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            Name = "Food",
            Description = "Groceries"
        });

        var updated = await svc.UpdateAsync(cat.Id, name: "Dining", description: null);

        Assert.NotNull(updated);
        Assert.Equal("Dining", updated!.Name);
        Assert.Equal("Groceries", updated.Description); // unchanged
    }

    [Fact]
    public async Task WhenUpdatingDescription_ShouldOnlyChangeDescription()
    {
        using var db = CreateInMemoryDb();
        var svc = new CategoryService(db);
        var cat = await svc.CreateAsync(new Category
        {
            UserId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            Name = "Transport",
            Description = "Public transit"
        });

        var updated = await svc.UpdateAsync(cat.Id, name: null, description: "Rides and fares");

        Assert.NotNull(updated);
        Assert.Equal("Transport", updated!.Name); // unchanged
        Assert.Equal("Rides and fares", updated.Description);
    }

    [Fact]
    public async Task WhenUpdatingBothFields_ShouldChangeBoth()
    {
        using var db = CreateInMemoryDb();
        var svc = new CategoryService(db);
        var cat = await svc.CreateAsync(new Category
        {
            UserId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            Name = "Old Name",
            Description = "Old Description"
        });

        var updated = await svc.UpdateAsync(cat.Id, name: "New Name", description: "New Description");

        Assert.NotNull(updated);
        Assert.Equal("New Name", updated!.Name);
        Assert.Equal("New Description", updated.Description);
    }

    [Fact]
    public async Task WhenUpdatingNonExistentCategory_ShouldReturnNull()
    {
        using var db = CreateInMemoryDb();
        var svc = new CategoryService(db);

        var result = await svc.UpdateAsync(Guid.NewGuid(), name: "Anything", description: null);

        Assert.Null(result);
    }

    [Fact]
    public async Task WhenUpdatingWithNoChanges_ShouldPreserveOriginalValues()
    {
        using var db = CreateInMemoryDb();
        var svc = new CategoryService(db);
        var cat = await svc.CreateAsync(new Category
        {
            UserId = Guid.Parse("00000000-0000-0000-0000-000000000001"),
            Name = "Utilities",
            Description = "Monthly bills"
        });

        var updated = await svc.UpdateAsync(cat.Id, name: null, description: null);

        Assert.NotNull(updated);
        Assert.Equal("Utilities", updated!.Name);
        Assert.Equal("Monthly bills", updated.Description);
    }
}
