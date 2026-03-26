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

public class CategoryAssignmentServiceTests
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

    private static Transaction SeedTransaction(FinanceDbContext db, string desc = "Coffee")
    {
        var tx = new Transaction
        {
            Id = Guid.NewGuid(),
            UserId = UserId,
            ImportId = ImportId,
            Date = DateTime.UtcNow,
            Amount = -5m,
            RawDescription = desc,
            NormalizedDescription = desc.ToLowerInvariant(),
            CreatedAt = DateTime.UtcNow
        };
        db.Transactions.Add(tx);
        db.SaveChanges();
        return tx;
    }

    [Fact]
    public async Task WhenAssigningCategory_ShouldUpdateAllMatchingTransactions()
    {
        var db = CreateDb();
        var tx1 = SeedTransaction(db, "Coffee1");
        var tx2 = SeedTransaction(db, "Coffee2");
        var catId = Guid.NewGuid();
        db.Categories.Add(new Category { Id = catId, UserId = UserId, Name = "Food", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var service = new CategoryAssignmentService(db);
        var result = await service.AssignCategoryAsync(UserId, [tx1.Id, tx2.Id], catId);

        Assert.Equal(2, result.UpdatedCount);
        Assert.Empty(result.NotFoundIds);

        var updated1 = await db.Transactions.FindAsync(tx1.Id);
        Assert.Equal(catId, updated1!.CategoryId);
    }

    [Fact]
    public async Task WhenSomeIdsNotFound_ShouldReturnNotFoundIds()
    {
        var db = CreateDb();
        var tx = SeedTransaction(db);
        var catId = Guid.NewGuid();
        db.Categories.Add(new Category { Id = catId, UserId = UserId, Name = "Food", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var fakeId = Guid.NewGuid();
        var service = new CategoryAssignmentService(db);
        var result = await service.AssignCategoryAsync(UserId, [tx.Id, fakeId], catId);

        Assert.Equal(1, result.UpdatedCount);
        Assert.Single(result.NotFoundIds);
        Assert.Contains(fakeId, result.NotFoundIds);
    }

    [Fact]
    public async Task WhenAllIdsNotFound_ShouldReturnZeroUpdated()
    {
        var db = CreateDb();
        var catId = Guid.NewGuid();
        db.Categories.Add(new Category { Id = catId, UserId = UserId, Name = "Food", CreatedAt = DateTime.UtcNow });
        await db.SaveChangesAsync();

        var service = new CategoryAssignmentService(db);
        var result = await service.AssignCategoryAsync(UserId, [Guid.NewGuid(), Guid.NewGuid()], catId);

        Assert.Equal(0, result.UpdatedCount);
        Assert.Equal(2, result.NotFoundIds.Count);
    }
}
