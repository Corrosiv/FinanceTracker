namespace FinanceTracker.API.Models;

/// <summary>
/// Lightweight view model for expense-type transactions.
/// The persistence entity is <see cref="Transaction"/>.
/// </summary>
public class Expense
{
    public Guid Id { get; set; }
    public decimal Amount { get; set; }
    public string Description { get; set; } = null!;
    public DateTime Date { get; set; }
    public Guid? CategoryId { get; set; }
}
