namespace FinanceTracker.API.DTOs;

public class BudgetAlertDto
{
    public Guid BudgetId { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = null!;
    public string Period { get; set; } = null!;
    public decimal LimitAmount { get; set; }
    public decimal SpentAmount { get; set; }
    public decimal PercentUsed { get; set; }
    public string Severity { get; set; } = null!;
}
