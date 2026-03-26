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
}
