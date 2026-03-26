namespace FinanceTracker.API.DTOs;

public class BudgetSuggestionDto
{
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = null!;
    public decimal AverageMonthlySpending { get; set; }
    public decimal SuggestedLimit { get; set; }
}
