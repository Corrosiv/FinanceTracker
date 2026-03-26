namespace FinanceTracker.API.Models;

public class Transaction
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid ImportId { get; set; }
    public DateTime Date { get; set; }
    public decimal Amount { get; set; }
    public string RawDescription { get; set; } = null!;
    public string NormalizedDescription { get; set; } = null!;
    public decimal? Balance { get; set; }
    public Guid? CategoryId { get; set; }
    public DateTime CreatedAt { get; set; }
    public string? DeduplicationHash { get; set; }

    public User User { get; set; } = null!;
    public Import Import { get; set; } = null!;
    public Category? Category { get; set; }
}
