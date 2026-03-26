using Microsoft.EntityFrameworkCore;
using FinanceTracker.API.Data;
using FinanceTracker.API.DTOs;
using FinanceTracker.API.Models;

namespace FinanceTracker.API.Services;

public class BudgetService : IBudgetService
{
    private readonly FinanceDbContext _db;

    public BudgetService(FinanceDbContext db)
    {
        _db = db;
    }

    public async Task<Budget> CreateAsync(Budget budget)
    {
        budget.Id = Guid.NewGuid();
        budget.CreatedAt = DateTime.UtcNow;
        _db.Budgets.Add(budget);
        await _db.SaveChangesAsync();
        return budget;
    }

    public async Task<Budget?> GetByIdAsync(Guid id)
    {
        return await _db.Budgets
            .Include(b => b.Category)
            .FirstOrDefaultAsync(b => b.Id == id);
    }

    public async Task<IEnumerable<Budget>> GetAllAsync()
    {
        return await _db.Budgets
            .Include(b => b.Category)
            .AsNoTracking()
            .ToListAsync();
    }

    public async Task<Budget?> UpdateAsync(Guid id, decimal? limitAmount)
    {
        var budget = await _db.Budgets.FindAsync(id);
        if (budget is null) return null;

        if (limitAmount.HasValue) budget.LimitAmount = limitAmount.Value;

        await _db.SaveChangesAsync();
        return budget;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var budget = await _db.Budgets.FindAsync(id);
        if (budget is null) return false;
        _db.Budgets.Remove(budget);
        await _db.SaveChangesAsync();
        return true;
    }

    public async Task<decimal> GetSpentAmountAsync(Guid userId, Guid categoryId, BudgetPeriod period)
    {
        var (from, to) = GetPeriodRange(period);

        var spent = await _db.Transactions
            .Where(t => t.UserId == userId
                     && t.CategoryId == categoryId
                     && t.Amount < 0
                     && t.Date >= from
                     && t.Date <= to)
            .SumAsync(t => t.Amount);

        return Math.Abs(spent);
    }

    public async Task<IEnumerable<BudgetSuggestionDto>> SuggestBudgetsAsync(Guid userId)
    {
        var threeMonthsAgo = DateTime.UtcNow.AddMonths(-3);

        var categorySpending = await _db.Transactions
            .Where(t => t.UserId == userId
                     && t.Amount < 0
                     && t.CategoryId != null
                     && t.Date >= threeMonthsAgo)
            .GroupBy(t => new { t.CategoryId, t.Category!.Name })
            .Select(g => new
            {
                g.Key.CategoryId,
                g.Key.Name,
                TotalSpent = Math.Abs(g.Sum(t => t.Amount))
            })
            .ToListAsync();

        var monthCount = Math.Max(1,
            (DateTime.UtcNow.Year - threeMonthsAgo.Year) * 12
            + DateTime.UtcNow.Month - threeMonthsAgo.Month);

        return categorySpending.Select(c => new BudgetSuggestionDto
        {
            CategoryId = c.CategoryId!.Value,
            CategoryName = c.Name,
            AverageMonthlySpending = Math.Round(c.TotalSpent / monthCount, 2),
            SuggestedLimit = Math.Round(c.TotalSpent / monthCount * 1.1m, 2)
        });
    }

    public async Task<IEnumerable<BudgetAlertDto>> GetAlertsAsync(Guid userId, decimal threshold)
    {
        var budgets = await _db.Budgets
            .Include(b => b.Category)
            .Where(b => b.UserId == userId)
            .ToListAsync();

        var alerts = new List<BudgetAlertDto>();

        foreach (var budget in budgets)
        {
            var spent = await GetSpentAmountAsync(userId, budget.CategoryId, budget.Period);
            var percentUsed = budget.LimitAmount > 0
                ? Math.Round(spent / budget.LimitAmount * 100, 1)
                : 0;

            if (percentUsed >= threshold)
            {
                alerts.Add(new BudgetAlertDto
                {
                    BudgetId = budget.Id,
                    CategoryId = budget.CategoryId,
                    CategoryName = budget.Category.Name,
                    Period = budget.Period.ToString(),
                    LimitAmount = budget.LimitAmount,
                    SpentAmount = spent,
                    PercentUsed = percentUsed,
                    Severity = percentUsed >= 100 ? "over" : "warning"
                });
            }
        }

        return alerts.OrderByDescending(a => a.PercentUsed);
    }

    internal static (DateTime from, DateTime to) GetPeriodRange(BudgetPeriod period)
    {
        var now = DateTime.UtcNow;
        return period switch
        {
            BudgetPeriod.Weekly => (now.Date.AddDays(-(int)now.DayOfWeek), now),
            BudgetPeriod.Monthly => (new DateTime(now.Year, now.Month, 1), now),
            BudgetPeriod.Yearly => (new DateTime(now.Year, 1, 1), now),
            _ => throw new ArgumentOutOfRangeException(nameof(period))
        };
    }
}
