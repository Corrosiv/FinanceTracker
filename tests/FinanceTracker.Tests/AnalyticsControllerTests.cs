using System;
using Xunit;
using FinanceTracker.API.Controllers;
using FinanceTracker.API.DTOs;

namespace FinanceTracker.Tests;

public class AnalyticsControllerTests
{
    [Fact]
    public void WhenCustomFromAndToProvided_ShouldUseCustomRange()
    {
        var from = new DateTime(2026, 1, 1);
        var to = new DateTime(2026, 3, 31);

        var (resolvedFrom, resolvedTo) = AnalyticsController.ResolveDateRange(
            null, from, to, AnalyticsPeriod.Last6Months);

        Assert.Equal(from, resolvedFrom);
        Assert.Equal(to, resolvedTo);
    }

    [Fact]
    public void WhenPeriodProvided_ShouldUsePresetRange()
    {
        var before = DateTime.UtcNow.AddDays(-31);

        var (resolvedFrom, resolvedTo) = AnalyticsController.ResolveDateRange(
            AnalyticsPeriod.Last30Days, null, null, AnalyticsPeriod.Last6Months);

        Assert.True(resolvedFrom > before);
        Assert.True(resolvedTo <= DateTime.UtcNow.AddSeconds(1));
    }

    [Fact]
    public void WhenNoPeriodAndNoCustomRange_ShouldUseDefault()
    {
        var sixMonthsAgo = DateTime.UtcNow.AddMonths(-6).AddMinutes(-1);

        var (resolvedFrom, resolvedTo) = AnalyticsController.ResolveDateRange(
            null, null, null, AnalyticsPeriod.Last6Months);

        Assert.True(resolvedFrom > sixMonthsAgo);
    }

    [Fact]
    public void WhenAllTimePeriod_ShouldUseDateTimeMinValue()
    {
        var (resolvedFrom, _) = AnalyticsController.ResolveDateRange(
            AnalyticsPeriod.AllTime, null, null, AnalyticsPeriod.Last6Months);

        Assert.Equal(DateTime.MinValue, resolvedFrom);
    }
}
