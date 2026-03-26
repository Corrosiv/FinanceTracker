using FinanceTracker.API.DTOs;

namespace FinanceTracker.API.Services;

public class TipsService : ITipsService
{
    private readonly IAnalyticsService _analyticsService;

    public TipsService(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    public async Task<TipsResponseDto> GetTipsAsync(Guid userId)
    {
        var now = DateTime.UtcNow;
        var threeMonthsAgo = now.AddMonths(-3);

        var topSpending = (await _analyticsService
            .GetSpendingByCategoryAsync(userId, threeMonthsAgo, now))
            .Take(5)
            .ToList();

        var recurring = (await _analyticsService
            .GetRecurringChargesAsync(userId, now.AddMonths(-6), now))
            .ToList();

        var summary = await _analyticsService
            .GetIncomeExpenseSummaryAsync(userId, threeMonthsAgo, now);

        var tips = GenerateTips(topSpending, recurring, summary);

        return new TipsResponseDto
        {
            TopSpendingCategories = topSpending,
            RecurringCharges = recurring,
            Summary = summary,
            GeneratedTips = tips
        };
    }

    private static List<string> GenerateTips(
        List<SpendingByCategoryDto> topCategories,
        List<RecurringChargeDto> recurring,
        IncomeExpenseSummaryDto summary)
    {
        var tips = new List<string>();

        if (topCategories.Count > 0)
        {
            var top = topCategories[0];
            tips.Add($"Your top spending category is {top.CategoryName} at ${top.TotalSpent:F2} — consider setting a budget.");
        }

        if (recurring.Count > 0)
        {
            var totalRecurring = recurring.Sum(r => r.AverageAmount);
            tips.Add($"You have {recurring.Count} recurring charge(s) totaling ~${totalRecurring:F2}/cycle. Review for any you no longer need.");
        }

        if (summary.SavingsRate < 0)
        {
            tips.Add($"You're spending more than you earn (savings rate: {summary.SavingsRate}%). Look for areas to cut back.");
        }
        else if (summary.SavingsRate < 10)
        {
            tips.Add($"Your savings rate is {summary.SavingsRate}%. Financial advisors recommend aiming for at least 20%.");
        }
        else if (summary.SavingsRate >= 20)
        {
            tips.Add($"Great job! Your savings rate is {summary.SavingsRate}%. Keep it up!");
        }

        if (topCategories.Count >= 3)
        {
            var topThreeTotal = topCategories.Take(3).Sum(c => c.TotalSpent);
            tips.Add($"Your top 3 categories account for ${topThreeTotal:F2}. Small reductions here have the biggest impact.");
        }

        return tips;
    }
}
