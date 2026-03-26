namespace FinanceTracker.API.Models;

public enum BudgetPeriod
{
    Weekly,
    Monthly,
    Yearly
}

public class Budget
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid CategoryId { get; set; }
    public BudgetPeriod Period { get; set; }
    public decimal LimitAmount { get; set; }
    public DateTime CreatedAt { get; set; }

    public User User { get; set; } = null!;
    public Category Category { get; set; } = null!;
}
