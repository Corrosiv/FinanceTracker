using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Xunit;
using FinanceTracker.API.Data;
using FinanceTracker.API.Models;
using FinanceTracker.API.Services;

namespace FinanceTracker.Tests;

public class AnalyticsServiceTests
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

    private static Category SeedCategory(FinanceDbContext db, string name)
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

    private static void SeedTransaction(FinanceDbContext db, decimal amount, string desc,
        DateTime date, Guid? categoryId = null)
    {
        db.Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            ImportId = ImportId,
            Date = date,
            Amount = amount,
            RawDescription = desc,
            NormalizedDescription = desc.Trim().ToLowerInvariant(),
            CategoryId = categoryId,
            CreatedAt = DateTime.UtcNow
        });
    }

    // ── Spending by category ─────────────────────────────────────────

    [Fact]
    public async Task WhenGettingSpendingByCategory_ShouldRankByTotalSpent()
    {
        var db = CreateDb();
        var food = SeedCategory(db, "Food");
        var transport = SeedCategory(db, "Transport");

        SeedTransaction(db, -100m, "Groceries", DateTime.UtcNow.AddDays(-5), food.Id);
        SeedTransaction(db, -50m, "Bus", DateTime.UtcNow.AddDays(-3), transport.Id);
        SeedTransaction(db, -200m, "Restaurant", DateTime.UtcNow.AddDays(-1), food.Id);
        await db.SaveChangesAsync();

        var service = new AnalyticsService(db);
        var result = (await service.GetSpendingByCategoryAsync(
            UserId, DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow)).ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal("Food", result[0].CategoryName);
        Assert.Equal(300m, result[0].TotalSpent);
        Assert.Equal(1, result[0].Rank);
        Assert.Equal(2, result[1].Rank);
    }

    [Fact]
    public async Task WhenGettingSpendingByCategory_ShouldExcludeIncome()
    {
        var db = CreateDb();
        var cat = SeedCategory(db, "Salary");

        SeedTransaction(db, 5000m, "Paycheck", DateTime.UtcNow.AddDays(-1), cat.Id);
        SeedTransaction(db, -100m, "Lunch", DateTime.UtcNow.AddDays(-1), cat.Id);
        await db.SaveChangesAsync();

        var service = new AnalyticsService(db);
        var result = (await service.GetSpendingByCategoryAsync(
            UserId, DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow)).ToList();

        Assert.Single(result);
        Assert.Equal(100m, result[0].TotalSpent);
    }

    // ── Category trends ──────────────────────────────────────────────

    [Fact]
    public async Task WhenGettingCategoryTrends_ShouldShowMonthOverMonthChange()
    {
        var db = CreateDb();
        var cat = SeedCategory(db, "Food");
        var now = DateTime.UtcNow;

        SeedTransaction(db, -100m, "Groceries", new DateTime(now.Year, now.Month, 5).AddMonths(-2), cat.Id);
        SeedTransaction(db, -150m, "Groceries", new DateTime(now.Year, now.Month, 5).AddMonths(-1), cat.Id);
        await db.SaveChangesAsync();

        var service = new AnalyticsService(db);
        var result = (await service.GetCategoryTrendsAsync(
            UserId, now.AddMonths(-3), now)).ToList();

        Assert.Single(result);
        Assert.Equal(2, result[0].Months.Count);
        Assert.NotNull(result[0].Months[1].ChangePercent);
        Assert.Equal(50.0m, result[0].Months[1].ChangePercent);
    }

    // ── Recurring charges ────────────────────────────────────────────

    [Fact]
    public async Task WhenDetectingRecurring_ShouldFindMonthlyCharges()
    {
        var db = CreateDb();
        var now = DateTime.UtcNow;

        // Netflix — 3 monthly charges with consistent ~30-day intervals
        for (int i = 0; i < 3; i++)
        {
            SeedTransaction(db, -15.99m, "Netflix", now.AddDays(-30 * (i + 1)));
        }
        await db.SaveChangesAsync();

        var service = new AnalyticsService(db);
        var result = (await service.GetRecurringChargesAsync(
            UserId, now.AddMonths(-6), now)).ToList();

        Assert.Single(result);
        Assert.Equal("monthly", result[0].DetectedFrequency);
        Assert.Equal(3, result[0].OccurrenceCount);
    }

    [Fact]
    public async Task WhenDetectingRecurring_ShouldExcludeIrregularCharges()
    {
        var db = CreateDb();
        var now = DateTime.UtcNow;

        // Irregular charges — stddev > 5 days
        SeedTransaction(db, -20m, "Random Store", now.AddDays(-5));
        SeedTransaction(db, -20m, "Random Store", now.AddDays(-15));
        SeedTransaction(db, -20m, "Random Store", now.AddDays(-80));
        await db.SaveChangesAsync();

        var service = new AnalyticsService(db);
        var result = (await service.GetRecurringChargesAsync(
            UserId, now.AddMonths(-6), now)).ToList();

        Assert.Empty(result);
    }

    [Fact]
    public async Task WhenDetectingRecurring_ShouldRequireMinimumThreeOccurrences()
    {
        var db = CreateDb();
        var now = DateTime.UtcNow;

        SeedTransaction(db, -9.99m, "Spotify", now.AddDays(-30));
        SeedTransaction(db, -9.99m, "Spotify", now.AddDays(-60));
        await db.SaveChangesAsync();

        var service = new AnalyticsService(db);
        var result = (await service.GetRecurringChargesAsync(
            UserId, now.AddMonths(-6), now)).ToList();

        Assert.Empty(result);
    }

    // ── Income vs. expenses ──────────────────────────────────────────

    [Fact]
    public async Task WhenGettingIncomeVsExpenses_ShouldCalculateSavingsRate()
    {
        var db = CreateDb();

        SeedTransaction(db, 5000m, "Salary", DateTime.UtcNow.AddDays(-15));
        SeedTransaction(db, -3000m, "Rent", DateTime.UtcNow.AddDays(-10));
        SeedTransaction(db, -500m, "Food", DateTime.UtcNow.AddDays(-5));
        await db.SaveChangesAsync();

        var service = new AnalyticsService(db);
        var result = await service.GetIncomeExpenseSummaryAsync(
            UserId, DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow);

        Assert.Equal(5000m, result.TotalIncome);
        Assert.Equal(3500m, result.TotalExpenses);
        Assert.Equal(1500m, result.NetSavings);
        Assert.Equal(30.0m, result.SavingsRate);
    }

    [Fact]
    public async Task WhenNoTransactions_ShouldReturnZeros()
    {
        var db = CreateDb();
        var service = new AnalyticsService(db);

        var result = await service.GetIncomeExpenseSummaryAsync(
            UserId, DateTime.UtcNow.AddMonths(-1), DateTime.UtcNow);

        Assert.Equal(0m, result.TotalIncome);
        Assert.Equal(0m, result.TotalExpenses);
        Assert.Equal(0m, result.SavingsRate);
    }
}
