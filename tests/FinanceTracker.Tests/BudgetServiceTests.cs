using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using FinanceTracker.API.Data;
using FinanceTracker.API.DTOs;
using FinanceTracker.API.Models;
using FinanceTracker.API.Services;
using FinanceTracker.API.Validators;

namespace FinanceTracker.Tests;

public class BudgetServiceTests
{
    private static readonly Guid UserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid ImportId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    private static FinanceDbContext CreateDb()
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
            FileName = "test",
            UploadDate = DateTime.UtcNow,
            Status = ImportStatus.Completed
        });
        db.SaveChanges();
        return db;
    }

    private static Category SeedCategory(FinanceDbContext db, string name = "Groceries")
    {
        var cat = new Category
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            Name = name,
            CreatedAt = DateTime.UtcNow
        };
        db.Categories.Add(cat);
        db.SaveChanges();
        return cat;
    }

    [Fact]
    public async Task WhenCreatingBudget_ShouldPersistAndReturnWithId()
    {
        var db = CreateDb();
        var cat = SeedCategory(db);
        var service = new BudgetService(db);

        var budget = new Budget
        {
            UserId = UserId,
            CategoryId = cat.Id,
            Period = BudgetPeriod.Monthly,
            LimitAmount = 500m
        };

        var created = await service.CreateAsync(budget);

        Assert.NotEqual(Guid.Empty, created.Id);
        Assert.Equal(500m, created.LimitAmount);
    }

    [Fact]
    public async Task WhenGettingAll_ShouldReturnAllBudgets()
    {
        var db = CreateDb();
        var cat = SeedCategory(db);
        var service = new BudgetService(db);

        await service.CreateAsync(new Budget { UserId = UserId, CategoryId = cat.Id, Period = BudgetPeriod.Monthly, LimitAmount = 300m });
        await service.CreateAsync(new Budget { UserId = UserId, CategoryId = cat.Id, Period = BudgetPeriod.Weekly, LimitAmount = 100m });

        var all = await service.GetAllAsync();
        Assert.Equal(2, all.Count());
    }

    [Fact]
    public async Task WhenUpdatingLimitAmount_ShouldOnlyChangeLimitAmount()
    {
        var db = CreateDb();
        var cat = SeedCategory(db);
        var service = new BudgetService(db);

        var budget = await service.CreateAsync(new Budget
        {
            UserId = UserId,
            CategoryId = cat.Id,
            Period = BudgetPeriod.Monthly,
            LimitAmount = 300m
        });

        var updated = await service.UpdateAsync(budget.Id, 600m);

        Assert.NotNull(updated);
        Assert.Equal(600m, updated!.LimitAmount);
        Assert.Equal(BudgetPeriod.Monthly, updated.Period);
    }

    [Fact]
    public async Task WhenUpdatingNonExistentBudget_ShouldReturnNull()
    {
        var db = CreateDb();
        var service = new BudgetService(db);

        var result = await service.UpdateAsync(Guid.NewGuid(), 500m);
        Assert.Null(result);
    }

    [Fact]
    public async Task WhenDeletingExistingBudget_ShouldRemoveFromDatabase()
    {
        var db = CreateDb();
        var cat = SeedCategory(db);
        var service = new BudgetService(db);

        var budget = await service.CreateAsync(new Budget
        {
            UserId = UserId,
            CategoryId = cat.Id,
            Period = BudgetPeriod.Monthly,
            LimitAmount = 300m
        });

        var deleted = await service.DeleteAsync(budget.Id);
        Assert.True(deleted);
        Assert.Null(await service.GetByIdAsync(budget.Id));
    }

    [Fact]
    public async Task WhenDeletingNonExistentBudget_ShouldReturnFalse()
    {
        var db = CreateDb();
        var service = new BudgetService(db);

        Assert.False(await service.DeleteAsync(Guid.NewGuid()));
    }

    [Fact]
    public async Task WhenCalculatingSpentAmount_ShouldSumOnlyNegativeTransactionsInPeriod()
    {
        var db = CreateDb();
        var cat = SeedCategory(db);
        var service = new BudgetService(db);

        // Current month expense
        db.Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            ImportId = ImportId,
            Date = DateTime.UtcNow.AddDays(-1),
            Amount = -50m,
            RawDescription = "Store",
            NormalizedDescription = "store",
            CategoryId = cat.Id,
            CreatedAt = DateTime.UtcNow
        });
        // Income (should be excluded)
        db.Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            ImportId = ImportId,
            Date = DateTime.UtcNow.AddDays(-1),
            Amount = 1000m,
            RawDescription = "Salary",
            NormalizedDescription = "salary",
            CategoryId = cat.Id,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var spent = await service.GetSpentAmountAsync(UserId, cat.Id, BudgetPeriod.Monthly);
        Assert.Equal(50m, spent);
    }

    [Fact]
    public async Task WhenSuggestingBudgets_ShouldReturnSuggestionsWithBuffer()
    {
        var db = CreateDb();
        var cat = SeedCategory(db);
        var service = new BudgetService(db);

        for (int i = 1; i <= 3; i++)
        {
            db.Transactions.Add(new Transaction
            {
                Id = Guid.NewGuid(),
                UserId = UserId,
                ImportId = ImportId,
                Date = DateTime.UtcNow.AddMonths(-i).AddDays(5),
                Amount = -200m,
                RawDescription = "Groceries",
                NormalizedDescription = "groceries",
                CategoryId = cat.Id,
                CreatedAt = DateTime.UtcNow
            });
        }
        await db.SaveChangesAsync();

        var suggestions = (await service.SuggestBudgetsAsync(UserId)).ToList();

        Assert.Single(suggestions);
        Assert.True(suggestions[0].SuggestedLimit > suggestions[0].AverageMonthlySpending);
    }

    [Fact]
    public async Task WhenGettingAlerts_ShouldReturnBudgetsOverThreshold()
    {
        var db = CreateDb();
        var cat = SeedCategory(db);
        var service = new BudgetService(db);

        await service.CreateAsync(new Budget
        {
            UserId = UserId,
            CategoryId = cat.Id,
            Period = BudgetPeriod.Monthly,
            LimitAmount = 100m
        });

        db.Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            ImportId = ImportId,
            Date = DateTime.UtcNow.AddDays(-1),
            Amount = -90m,
            RawDescription = "Big grocery run",
            NormalizedDescription = "big grocery run",
            CategoryId = cat.Id,
            CreatedAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();

        var alerts = (await service.GetAlertsAsync(UserId, 80)).ToList();

        Assert.Single(alerts);
        Assert.Equal("warning", alerts[0].Severity);
        Assert.Equal(90m, alerts[0].PercentUsed);
    }
}
