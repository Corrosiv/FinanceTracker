using FinanceTracker.API.DTOs;

namespace FinanceTracker.API.Services;

public interface IAnalyticsService
{
    Task<IEnumerable<SpendingByCategoryDto>> GetSpendingByCategoryAsync(Guid userId, DateTime from, DateTime to);
    Task<IEnumerable<CategoryTrendDto>> GetCategoryTrendsAsync(Guid userId, DateTime from, DateTime to);
    Task<IEnumerable<RecurringChargeDto>> GetRecurringChargesAsync(Guid userId, DateTime from, DateTime to);
    Task<IncomeExpenseSummaryDto> GetIncomeExpenseSummaryAsync(Guid userId, DateTime from, DateTime to);
}
