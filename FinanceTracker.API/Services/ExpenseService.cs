using Microsoft.EntityFrameworkCore;
using FinanceTracker.API.Data;
using FinanceTracker.API.Models;

namespace FinanceTracker.API.Services;

public class ExpenseService : IExpenseService
{
    private readonly FinanceDbContext _db;

    public ExpenseService(FinanceDbContext db)
    {
        _db = db;
    }

    public async Task<Transaction> CreateAsync(Transaction transaction)
    {
        transaction.Id = Guid.NewGuid();
        transaction.CreatedAt = DateTime.UtcNow;
        _db.Transactions.Add(transaction);
        await _db.SaveChangesAsync();
        return transaction;
    }

    public async Task<Transaction?> GetByIdAsync(Guid id)
    {
        return await _db.Transactions.FindAsync(id);
    }

    public async Task<IEnumerable<Transaction>> GetAllAsync()
    {
        return await _db.Transactions
            .Where(t => t.Amount < 0)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Transaction?> UpdateAsync(Guid id, decimal? amount, string? description, DateTime? date, Guid? categoryId)
    {
        var tx = await _db.Transactions.FindAsync(id);
        if (tx is null) return null;

        if (amount.HasValue) tx.Amount = amount.Value;
        if (description is not null)
        {
            tx.RawDescription = description;
            tx.NormalizedDescription = description.Trim().ToLowerInvariant();
        }
        if (date.HasValue) tx.Date = date.Value;
        if (categoryId.HasValue) tx.CategoryId = categoryId.Value;

        await _db.SaveChangesAsync();
        return tx;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var tx = await _db.Transactions.FindAsync(id);
        if (tx is null) return false;
        _db.Transactions.Remove(tx);
        await _db.SaveChangesAsync();
        return true;
    }
}
