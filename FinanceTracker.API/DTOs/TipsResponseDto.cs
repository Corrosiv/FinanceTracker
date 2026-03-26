namespace FinanceTracker.API.DTOs;

public class TipsResponseDto
{
    public List<SpendingByCategoryDto> TopSpendingCategories { get; set; } = [];
    public List<RecurringChargeDto> RecurringCharges { get; set; } = [];
    public IncomeExpenseSummaryDto? Summary { get; set; }
    public List<string> GeneratedTips { get; set; } = [];
}
