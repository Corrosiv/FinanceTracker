namespace FinanceTracker.API.Models;

public class User
{
    public Guid Id { get; set; }
    public string? Name { get; set; }
    public string? Email { get; set; }
    public DateTime CreatedAt { get; set; }

    public ICollection<Transaction> Transactions { get; set; } = [];
    public ICollection<Category> Categories { get; set; } = [];
    public ICollection<Budget> Budgets { get; set; } = [];
    public ICollection<Import> Imports { get; set; } = [];
}
