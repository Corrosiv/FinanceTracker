using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using FinanceTracker.API.Data;
using FinanceTracker.API.Models;
using FinanceTracker.API.Services;

namespace FinanceTracker.Tests;

public class ExpenseServiceTests
{
    private static readonly Guid UserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid ImportId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    private static FinanceDbContext CreateInMemoryDb()
    {
        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var db = new FinanceDbContext(options);
        db.Users.Add(new User { Id = UserId, Name = "Test", CreatedAt = DateTime.UtcNow });
        db.Imports.Add(new Import
        {
            Id = ImportId,
            UserId = UserId,
            FileName = "manual",
            UploadDate = DateTime.UtcNow,
            Status = ImportStatus.Completed
        });
        db.SaveChanges();
        return db;
    }

    [Fact]
    public async Task CreateAndGetExpense()
    {
        using var db = CreateInMemoryDb();
        var svc = new ExpenseService(db);
        var tx = new Transaction
        {
            UserId = UserId,
            ImportId = ImportId,
            Amount = -12.34m,
            RawDescription = "Lunch",
            NormalizedDescription = "lunch",
            Date = DateTime.UtcNow
        };

        var created = await svc.CreateAsync(tx);
        Assert.NotNull(created);
        Assert.NotEqual(Guid.Empty, created.Id);

        var fetched = await svc.GetByIdAsync(created.Id);
        Assert.NotNull(fetched);
        Assert.Equal(-12.34m, fetched!.Amount);
    }

    [Fact]
    public async Task GetAllReturnsOnlyExpenses()
    {
        using var db = CreateInMemoryDb();
        var svc = new ExpenseService(db);

        await svc.CreateAsync(new Transaction
        {
            UserId = UserId, ImportId = ImportId,
            Amount = -5m, RawDescription = "Coffee", NormalizedDescription = "coffee",
            Date = DateTime.UtcNow
        });

        // income (positive amount) should not be returned by GetAll
        db.Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(), UserId = UserId, ImportId = ImportId,
            Amount = 100m, RawDescription = "Salary", NormalizedDescription = "salary",
            Date = DateTime.UtcNow, CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var expenses = (await svc.GetAllAsync()).ToList();
        Assert.Single(expenses);
        Assert.True(expenses[0].Amount < 0);
    }

    [Fact]
    public async Task DeleteRemovesExpense()
    {
        using var db = CreateInMemoryDb();
        var svc = new ExpenseService(db);
        var tx = await svc.CreateAsync(new Transaction
        {
            UserId = UserId, ImportId = ImportId,
            Amount = -10m, RawDescription = "Taxi", NormalizedDescription = "taxi",
            Date = DateTime.UtcNow
        });

        Assert.True(await svc.DeleteAsync(tx.Id));
        Assert.Null(await svc.GetByIdAsync(tx.Id));
    }

    [Fact]
    public async Task UpdateAsync_UpdatesAmount()
    {
        using var db = CreateInMemoryDb();
        var svc = new ExpenseService(db);
        var tx = await svc.CreateAsync(new Transaction
        {
            UserId = UserId, ImportId = ImportId,
            Amount = -10m, RawDescription = "Taxi", NormalizedDescription = "taxi",
            Date = DateTime.UtcNow
        });

        var updated = await svc.UpdateAsync(tx.Id, amount: -25m, description: null, date: null, categoryId: null);

        Assert.NotNull(updated);
        Assert.Equal(-25m, updated!.Amount);
        Assert.Equal("Taxi", updated.RawDescription); // unchanged
    }

    [Fact]
    public async Task UpdateAsync_UpdatesDescription()
    {
        using var db = CreateInMemoryDb();
        var svc = new ExpenseService(db);
        var tx = await svc.CreateAsync(new Transaction
        {
            UserId = UserId, ImportId = ImportId,
            Amount = -10m, RawDescription = "Taxi", NormalizedDescription = "taxi",
            Date = DateTime.UtcNow
        });

        var updated = await svc.UpdateAsync(tx.Id, amount: null, description: "Uber ride", date: null, categoryId: null);

        Assert.NotNull(updated);
        Assert.Equal("Uber ride", updated!.RawDescription);
        Assert.Equal("uber ride", updated.NormalizedDescription);
        Assert.Equal(-10m, updated.Amount); // unchanged
    }

    [Fact]
    public async Task UpdateAsync_UpdatesDateAndCategory()
    {
        using var db = CreateInMemoryDb();
        var svc = new ExpenseService(db);
        var categoryId = Guid.NewGuid();
        db.Categories.Add(new Category { Id = categoryId, UserId = UserId, Name = "Transport", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var tx = await svc.CreateAsync(new Transaction
        {
            UserId = UserId, ImportId = ImportId,
            Amount = -10m, RawDescription = "Taxi", NormalizedDescription = "taxi",
            Date = new DateTime(2026, 1, 1)
        });

        var newDate = new DateTime(2026, 3, 15);
        var updated = await svc.UpdateAsync(tx.Id, amount: null, description: null, date: newDate, categoryId: categoryId);

        Assert.NotNull(updated);
        Assert.Equal(newDate, updated!.Date);
        Assert.Equal(categoryId, updated.CategoryId);
    }

    [Fact]
    public async Task UpdateAsync_NonExistentReturnsNull()
    {
        using var db = CreateInMemoryDb();
        var svc = new ExpenseService(db);

        var result = await svc.UpdateAsync(Guid.NewGuid(), amount: -5m, description: null, date: null, categoryId: null);

        Assert.Null(result);
    }

    [Fact]
    public async Task UpdateAsync_NoChangesPreservesOriginal()
    {
        using var db = CreateInMemoryDb();
        var svc = new ExpenseService(db);
        var originalDate = new DateTime(2026, 3, 1);
        var tx = await svc.CreateAsync(new Transaction
        {
            UserId = UserId, ImportId = ImportId,
            Amount = -42m, RawDescription = "Phone bill", NormalizedDescription = "phone bill",
            Date = originalDate
        });

        var updated = await svc.UpdateAsync(tx.Id, amount: null, description: null, date: null, categoryId: null);

        Assert.NotNull(updated);
        Assert.Equal(-42m, updated!.Amount);
        Assert.Equal("Phone bill", updated.RawDescription);
        Assert.Equal(originalDate, updated.Date);
    }
}
