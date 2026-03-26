namespace FinanceTracker.API.DTOs;

public class CategoryResponseDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
}
