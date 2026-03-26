namespace FinanceTracker.API.DTOs;

public class UpdateExpenseDto
{
    public decimal? Amount { get; set; }
    public string? Description { get; set; }
    public DateTime? Date { get; set; }
    public Guid? CategoryId { get; set; }
}
