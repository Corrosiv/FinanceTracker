using FinanceTracker.API.DTOs;
using FinanceTracker.API.Models;

namespace FinanceTracker.API.Services;

public interface IBudgetService
{
    Task<Budget> CreateAsync(Budget budget);
    Task<Budget?> GetByIdAsync(Guid id);
    Task<IEnumerable<Budget>> GetAllAsync();
    Task<Budget?> UpdateAsync(Guid id, decimal? limitAmount);
    Task<bool> DeleteAsync(Guid id);
    Task<decimal> GetSpentAmountAsync(Guid userId, Guid categoryId, BudgetPeriod period);
    Task<IEnumerable<BudgetSuggestionDto>> SuggestBudgetsAsync(Guid userId);
    Task<IEnumerable<BudgetAlertDto>> GetAlertsAsync(Guid userId, decimal threshold);
}
