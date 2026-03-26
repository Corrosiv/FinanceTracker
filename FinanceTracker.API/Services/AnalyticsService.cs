using Microsoft.EntityFrameworkCore;
using FinanceTracker.API.Data;
using FinanceTracker.API.DTOs;

namespace FinanceTracker.API.Services;

public class AnalyticsService : IAnalyticsService
{
    private readonly FinanceDbContext _db;

    public AnalyticsService(FinanceDbContext db)
    {
        _db = db;
    }

    public async Task<IEnumerable<SpendingByCategoryDto>> GetSpendingByCategoryAsync(
        Guid userId, DateTime from, DateTime to)
    {
        var results = await _db.Transactions
            .Where(t => t.UserId == userId
                     && t.Amount < 0
                     && t.Date >= from
                     && t.Date <= to
                     && t.CategoryId != null)
            .GroupBy(t => new { t.CategoryId, t.Category!.Name })
            .Select(g => new SpendingByCategoryDto
            {
                CategoryId = g.Key.CategoryId!.Value,
                CategoryName = g.Key.Name,
                TotalSpent = Math.Abs(g.Sum(t => t.Amount)),
                TransactionCount = g.Count()
            })
            .OrderByDescending(s => s.TotalSpent)
            .ToListAsync();

        for (int i = 0; i < results.Count; i++)
            results[i].Rank = i + 1;

        return results;
    }

    public async Task<IEnumerable<CategoryTrendDto>> GetCategoryTrendsAsync(
        Guid userId, DateTime from, DateTime to)
    {
        var monthlyData = await _db.Transactions
            .Where(t => t.UserId == userId
                     && t.Amount < 0
                     && t.Date >= from
                     && t.Date <= to
                     && t.CategoryId != null)
            .GroupBy(t => new { t.CategoryId, t.Category!.Name, t.Date.Year, t.Date.Month })
            .Select(g => new
            {
                g.Key.CategoryId,
                g.Key.Name,
                g.Key.Year,
                g.Key.Month,
                TotalSpent = Math.Abs(g.Sum(t => t.Amount))
            })
            .OrderBy(x => x.CategoryId)
            .ThenBy(x => x.Year)
            .ThenBy(x => x.Month)
            .ToListAsync();

        var trends = monthlyData
            .GroupBy(x => new { x.CategoryId, x.Name })
            .Select(g =>
            {
                var months = g.Select(x => new MonthlyAmountDto
                {
                    Year = x.Year,
                    Month = x.Month,
                    TotalSpent = x.TotalSpent
                }).ToList();

                for (int i = 1; i < months.Count; i++)
                {
                    var prev = months[i - 1].TotalSpent;
                    if (prev > 0)
                        months[i].ChangePercent = Math.Round((months[i].TotalSpent - prev) / prev * 100, 1);
                }

                return new CategoryTrendDto
                {
                    CategoryId = g.Key.CategoryId!.Value,
                    CategoryName = g.Key.Name,
                    Months = months
                };
            });

        return trends;
    }

    public async Task<IEnumerable<RecurringChargeDto>> GetRecurringChargesAsync(
        Guid userId, DateTime from, DateTime to)
    {
        var transactions = await _db.Transactions
            .Where(t => t.UserId == userId
                     && t.Amount < 0
                     && t.Date >= from
                     && t.Date <= to)
            .OrderBy(t => t.NormalizedDescription)
            .ThenBy(t => t.Date)
            .ToListAsync();

        var groups = transactions
            .GroupBy(t => t.NormalizedDescription)
            .Where(g => g.Count() >= 3);

        var recurring = new List<RecurringChargeDto>();

        foreach (var group in groups)
        {
            var dates = group.Select(t => t.Date).OrderBy(d => d).ToList();
            var intervals = new List<double>();

            for (int i = 1; i < dates.Count; i++)
                intervals.Add((dates[i] - dates[i - 1]).TotalDays);

            if (intervals.Count == 0) continue;

            var avgInterval = intervals.Average();
            var stddev = Math.Sqrt(intervals.Sum(x => Math.Pow(x - avgInterval, 2)) / intervals.Count);

            if (stddev > 5) continue;

            var frequency = avgInterval switch
            {
                <= 10 => "weekly",
                <= 20 => "biweekly",
                <= 35 => "monthly",
                <= 100 => "quarterly",
                _ => "yearly"
            };

            recurring.Add(new RecurringChargeDto
            {
                Description = group.First().RawDescription,
                AverageAmount = Math.Abs(Math.Round(group.Average(t => t.Amount), 2)),
                OccurrenceCount = group.Count(),
                AverageIntervalDays = Math.Round(avgInterval, 1),
                DetectedFrequency = frequency,
                LastOccurrence = dates.Last()
            });
        }

        return recurring.OrderByDescending(r => r.AverageAmount);
    }

    public async Task<IncomeExpenseSummaryDto> GetIncomeExpenseSummaryAsync(
        Guid userId, DateTime from, DateTime to)
    {
        var totals = await _db.Transactions
            .Where(t => t.UserId == userId && t.Date >= from && t.Date <= to)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                TotalIncome = g.Where(t => t.Amount > 0).Sum(t => t.Amount),
                TotalExpenses = Math.Abs(g.Where(t => t.Amount < 0).Sum(t => t.Amount))
            })
            .FirstOrDefaultAsync();

        var income = totals?.TotalIncome ?? 0;
        var expenses = totals?.TotalExpenses ?? 0;
        var net = income - expenses;
        var savingsRate = income > 0 ? Math.Round(net / income * 100, 1) : 0;

        return new IncomeExpenseSummaryDto
        {
            TotalIncome = income,
            TotalExpenses = expenses,
            NetSavings = net,
            SavingsRate = savingsRate,
            From = from,
            To = to
        };
    }
}
