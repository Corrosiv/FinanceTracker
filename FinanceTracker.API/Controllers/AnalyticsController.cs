using Microsoft.AspNetCore.Mvc;
using FinanceTracker.API.DTOs;
using FinanceTracker.API.Services;

namespace FinanceTracker.API.Controllers;

[ApiController]
[Route("api/v1/analytics")]
public class AnalyticsController : ControllerBase
{
    private readonly IAnalyticsService _analyticsService;

    private static readonly Guid DefaultUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public AnalyticsController(IAnalyticsService analyticsService)
    {
        _analyticsService = analyticsService;
    }

    [HttpGet("spending-by-category")]
    public async Task<IActionResult> SpendingByCategory(
        [FromQuery] AnalyticsPeriod? period,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var (resolvedFrom, resolvedTo) = ResolveDateRange(period, from, to, AnalyticsPeriod.Last6Months);
        var result = await _analyticsService.GetSpendingByCategoryAsync(DefaultUserId, resolvedFrom, resolvedTo);
        return Ok(result);
    }

    [HttpGet("category-trends")]
    public async Task<IActionResult> CategoryTrends(
        [FromQuery] AnalyticsPeriod? period,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var (resolvedFrom, resolvedTo) = ResolveDateRange(period, from, to, AnalyticsPeriod.Last6Months);
        var result = await _analyticsService.GetCategoryTrendsAsync(DefaultUserId, resolvedFrom, resolvedTo);
        return Ok(result);
    }

    [HttpGet("recurring-charges")]
    public async Task<IActionResult> RecurringCharges(
        [FromQuery] AnalyticsPeriod? period,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var (resolvedFrom, resolvedTo) = ResolveDateRange(period, from, to, AnalyticsPeriod.Last6Months);
        var result = await _analyticsService.GetRecurringChargesAsync(DefaultUserId, resolvedFrom, resolvedTo);
        return Ok(result);
    }

    [HttpGet("income-vs-expenses")]
    public async Task<IActionResult> IncomeVsExpenses(
        [FromQuery] AnalyticsPeriod? period,
        [FromQuery] DateTime? from,
        [FromQuery] DateTime? to)
    {
        var (resolvedFrom, resolvedTo) = ResolveDateRange(period, from, to, AnalyticsPeriod.AllTime);
        var result = await _analyticsService.GetIncomeExpenseSummaryAsync(DefaultUserId, resolvedFrom, resolvedTo);
        return Ok(result);
    }

    internal static (DateTime from, DateTime to) ResolveDateRange(
        AnalyticsPeriod? period, DateTime? customFrom, DateTime? customTo, AnalyticsPeriod defaultPeriod)
    {
        if (customFrom.HasValue && customTo.HasValue)
            return (customFrom.Value, customTo.Value);

        var effectivePeriod = period ?? defaultPeriod;
        var now = DateTime.UtcNow;

        var from = effectivePeriod switch
        {
            AnalyticsPeriod.Last30Days => now.AddDays(-30),
            AnalyticsPeriod.Last3Months => now.AddMonths(-3),
            AnalyticsPeriod.Last6Months => now.AddMonths(-6),
            AnalyticsPeriod.LastYear => now.AddYears(-1),
            AnalyticsPeriod.AllTime => DateTime.MinValue,
            _ => now.AddMonths(-6)
        };

        return (from, now);
    }
}
