using System;
using System.Threading.Tasks;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Xunit;
using FinanceTracker.API.Data;
using FinanceTracker.API.Models;
using FinanceTracker.API.Services;

namespace FinanceTracker.Tests;

/// <summary>
/// DB-level edge-case tests using SQLite in-memory (real provider, not EF InMemory)
/// to verify unique constraints and FK violations are enforced.
/// </summary>
public class DatabaseEdgeCaseTests : IDisposable
{
    private readonly SqliteConnection _connection;
    private readonly FinanceDbContext _db;

    // Shared test data IDs
    private static readonly Guid DefaultUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");
    private static readonly Guid ManualImportId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    public DatabaseEdgeCaseTests()
    {
        _connection = new SqliteConnection("DataSource=:memory:");
        _connection.Open();

        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseSqlite(_connection)
            .Options;

        _db = new FinanceDbContext(options);
        _db.Database.EnsureCreated();

        // Seed the default user and manual import required by FK constraints
        _db.Users.Add(new User
        {
            Id = DefaultUserId,
            Name = "Default User",
            CreatedAt = DateTime.UtcNow
        });
        _db.Imports.Add(new Import
        {
            Id = ManualImportId,
            UserId = DefaultUserId,
            FileName = "manual-entry",
            UploadDate = DateTime.UtcNow,
            Status = ImportStatus.Completed
        });
        _db.SaveChanges();
    }

    public void Dispose()
    {
        _db.Dispose();
        _connection.Dispose();
    }

    // ── Duplicate category name per user → UNIQUE(UserId, Name) ──────────

    [Fact]
    public async Task WhenAddingDuplicateCategoryNameForSameUser_ShouldThrowDbUpdateException()
    {
        _db.Categories.Add(new Category
        {
            Id = Guid.NewGuid(),
            UserId = DefaultUserId,
            Name = "Groceries",
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        _db.Categories.Add(new Category
        {
            Id = Guid.NewGuid(),
            UserId = DefaultUserId,
            Name = "Groceries",
            CreatedAt = DateTime.UtcNow
        });

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => _db.SaveChangesAsync());
        Assert.Contains("UNIQUE constraint failed", ex.InnerException!.Message);
    }

    [Fact]
    public async Task WhenAddingSameCategoryNameForDifferentUsers_ShouldSucceed()
    {
        var secondUserId = Guid.NewGuid();
        _db.Users.Add(new User
        {
            Id = secondUserId,
            Name = "Second User",
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        _db.Categories.Add(new Category
        {
            Id = Guid.NewGuid(),
            UserId = DefaultUserId,
            Name = "Groceries",
            CreatedAt = DateTime.UtcNow
        });
        _db.Categories.Add(new Category
        {
            Id = Guid.NewGuid(),
            UserId = secondUserId,
            Name = "Groceries",
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(); // should not throw

        Assert.Equal(2, await _db.Categories.CountAsync(c => c.Name == "Groceries"));
    }

    // ── Duplicate transaction → UNIQUE(UserId, Date, Amount, NormalizedDescription) ──

    [Fact]
    public async Task WhenAddingDuplicateTransaction_ShouldThrowDbUpdateException()
    {
        var date = new DateTime(2026, 3, 15);
        var tx1 = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = DefaultUserId,
            ImportId = ManualImportId,
            Date = date,
            Amount = -50.00m,
            RawDescription = "Coffee Shop",
            NormalizedDescription = "coffee shop",
            CreatedAt = DateTime.UtcNow
        };
        _db.Transactions.Add(tx1);
        await _db.SaveChangesAsync();

        var tx2 = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = DefaultUserId,
            ImportId = ManualImportId,
            Date = date,
            Amount = -50.00m,
            RawDescription = "Coffee Shop",
            NormalizedDescription = "coffee shop",
            CreatedAt = DateTime.UtcNow
        };
        _db.Transactions.Add(tx2);

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => _db.SaveChangesAsync());
        Assert.Contains("UNIQUE constraint failed", ex.InnerException!.Message);
    }

    [Fact]
    public async Task WhenAddingTransactionWithDifferentAmount_ShouldSucceed()
    {
        var date = new DateTime(2026, 3, 15);

        _db.Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = DefaultUserId,
            ImportId = ManualImportId,
            Date = date,
            Amount = -50.00m,
            RawDescription = "Coffee Shop",
            NormalizedDescription = "coffee shop",
            CreatedAt = DateTime.UtcNow
        });
        _db.Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = DefaultUserId,
            ImportId = ManualImportId,
            Date = date,
            Amount = -25.00m,
            RawDescription = "Coffee Shop",
            NormalizedDescription = "coffee shop",
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(); // should not throw
        Assert.Equal(2, await _db.Transactions.CountAsync());
    }

    // ── Expense with non-existent CategoryId → FK violation ──────────────

    [Fact]
    public async Task WhenAddingExpenseWithNonExistentCategoryId_ShouldThrowDbUpdateException()
    {
        var tx = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = DefaultUserId,
            ImportId = ManualImportId,
            Date = new DateTime(2026, 3, 15),
            Amount = -30.00m,
            RawDescription = "Lunch",
            NormalizedDescription = "lunch",
            CategoryId = Guid.NewGuid(), // non-existent category
            CreatedAt = DateTime.UtcNow
        };
        _db.Transactions.Add(tx);

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() => _db.SaveChangesAsync());
        Assert.Contains("FOREIGN KEY constraint failed", ex.InnerException!.Message);
    }

    [Fact]
    public async Task WhenAddingExpenseWithValidCategoryId_ShouldSucceed()
    {
        var categoryId = Guid.NewGuid();
        _db.Categories.Add(new Category
        {
            Id = categoryId,
            UserId = DefaultUserId,
            Name = "Food",
            CreatedAt = DateTime.UtcNow
        });
        await _db.SaveChangesAsync();

        _db.Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = DefaultUserId,
            ImportId = ManualImportId,
            Date = new DateTime(2026, 3, 15),
            Amount = -30.00m,
            RawDescription = "Lunch",
            NormalizedDescription = "lunch",
            CategoryId = categoryId,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(); // should not throw
        Assert.Equal(1, await _db.Transactions.CountAsync());
    }

    [Fact]
    public async Task WhenAddingExpenseWithNullCategoryId_ShouldSucceed()
    {
        _db.Transactions.Add(new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = DefaultUserId,
            ImportId = ManualImportId,
            Date = new DateTime(2026, 3, 15),
            Amount = -30.00m,
            RawDescription = "Miscellaneous",
            NormalizedDescription = "miscellaneous",
            CategoryId = null,
            CreatedAt = DateTime.UtcNow
        });

        await _db.SaveChangesAsync(); // should not throw
        Assert.Equal(1, await _db.Transactions.CountAsync());
    }

    // ── CategoryService edge cases with real SQLite ──────────────────────

    [Fact]
    public async Task CategoryService_WhenCreatingDuplicateName_ShouldThrowDbUpdateException()
    {
        var service = new CategoryService(_db);

        await service.CreateAsync(new Category
        {
            UserId = DefaultUserId,
            Name = "Transport"
        });

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() =>
            service.CreateAsync(new Category
            {
                UserId = DefaultUserId,
                Name = "Transport"
            }));

        Assert.Contains("UNIQUE constraint failed", ex.InnerException!.Message);
    }

    // ── ExpenseService edge cases with real SQLite ───────────────────────

    [Fact]
    public async Task ExpenseService_WhenCreatingDuplicateTransaction_ShouldThrowDbUpdateException()
    {
        var service = new ExpenseService(_db);
        var date = new DateTime(2026, 4, 10);

        await service.CreateAsync(new Transaction
        {
            UserId = DefaultUserId,
            ImportId = ManualImportId,
            Date = date,
            Amount = -100.00m,
            RawDescription = "Electric bill",
            NormalizedDescription = "electric bill"
        });

        var ex = await Assert.ThrowsAsync<DbUpdateException>(() =>
            service.CreateAsync(new Transaction
            {
                UserId = DefaultUserId,
                ImportId = ManualImportId,
                Date = date,
                Amount = -100.00m,
                RawDescription = "Electric bill",
                NormalizedDescription = "electric bill"
            }));

        Assert.Contains("UNIQUE constraint failed", ex.InnerException!.Message);
    }
}
