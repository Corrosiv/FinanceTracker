namespace FinanceTracker.API.DTOs;

public class BudgetResponseDto
{
    public Guid Id { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = null!;
    public string Period { get; set; } = null!;
    public decimal LimitAmount { get; set; }
    public decimal SpentAmount { get; set; }
    public decimal RemainingAmount { get; set; }
    public bool IsOverBudget { get; set; }
}
