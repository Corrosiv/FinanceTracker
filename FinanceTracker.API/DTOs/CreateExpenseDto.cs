namespace FinanceTracker.API.DTOs;

public class CreateExpenseDto
{
    public decimal Amount { get; set; }
    public string Description { get; set; } = null!;
    public DateTime Date { get; set; }
    public Guid? CategoryId { get; set; }
}
