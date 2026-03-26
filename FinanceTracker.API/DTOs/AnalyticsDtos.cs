namespace FinanceTracker.API.DTOs;

public enum AnalyticsPeriod
{
    Last30Days,
    Last3Months,
    Last6Months,
    LastYear,
    AllTime
}

public class SpendingByCategoryDto
{
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = null!;
    public decimal TotalSpent { get; set; }
    public int TransactionCount { get; set; }
    public int Rank { get; set; }
}

public class CategoryTrendDto
{
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = null!;
    public List<MonthlyAmountDto> Months { get; set; } = [];
}

public class MonthlyAmountDto
{
    public int Year { get; set; }
    public int Month { get; set; }
    public decimal TotalSpent { get; set; }
    public decimal? ChangePercent { get; set; }
}

public class RecurringChargeDto
{
    public string Description { get; set; } = null!;
    public decimal AverageAmount { get; set; }
    public int OccurrenceCount { get; set; }
    public double AverageIntervalDays { get; set; }
    public string DetectedFrequency { get; set; } = null!;
    public DateTime LastOccurrence { get; set; }
}

public class IncomeExpenseSummaryDto
{
    public decimal TotalIncome { get; set; }
    public decimal TotalExpenses { get; set; }
    public decimal NetSavings { get; set; }
    public decimal SavingsRate { get; set; }
    public DateTime From { get; set; }
    public DateTime To { get; set; }
}
