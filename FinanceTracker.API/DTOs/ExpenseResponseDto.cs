namespace FinanceTracker.API.DTOs;

public class ExpenseResponseDto
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = null!;
    public DateTime Date { get; set; }
    public Guid? CategoryId { get; set; }
}
