namespace FinanceTracker.API.DTOs;

public class ImportResponseDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = null!;
    public string Status { get; set; } = null!;
    public int RowCount { get; set; }
    public int ProcessedCount { get; set; }
    public int DuplicateCount { get; set; }
    public List<ImportRowErrorDto> Errors { get; set; } = [];
}

public class ImportRowErrorDto
{
    public int RowNumber { get; set; }
    public string Error { get; set; } = null!;
    public string? RawData { get; set; }
}
