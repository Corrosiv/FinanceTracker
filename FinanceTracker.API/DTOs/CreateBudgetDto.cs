namespace FinanceTracker.API.DTOs;

public class CreateBudgetDto
{
    public Guid CategoryId { get; set; }
    public string Period { get; set; } = null!;
    public decimal LimitAmount { get; set; }
}
