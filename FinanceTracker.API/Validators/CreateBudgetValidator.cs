using FinanceTracker.API.DTOs;
using FinanceTracker.API.Models;

namespace FinanceTracker.API.Validators;

public static class CreateBudgetValidator
{
    private static readonly HashSet<string> ValidPeriods = new(StringComparer.OrdinalIgnoreCase)
    {
        nameof(BudgetPeriod.Weekly),
        nameof(BudgetPeriod.Monthly),
        nameof(BudgetPeriod.Yearly)
    };

    public static List<string> Validate(CreateBudgetDto dto)
    {
        var errors = new List<string>();

        if (dto.CategoryId == Guid.Empty)
            errors.Add("CategoryId is required.");

        if (!ValidPeriods.Contains(dto.Period ?? ""))
            errors.Add("Period must be one of: Weekly, Monthly, Yearly.");

        if (dto.LimitAmount <= 0)
            errors.Add("LimitAmount must be greater than zero.");

        return errors;
    }
}
