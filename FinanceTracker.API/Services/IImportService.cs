using FinanceTracker.API.DTOs;

namespace FinanceTracker.API.Services;

public interface IImportService
{
    Task<ImportResponseDto> ImportCsvAsync(Stream csvStream, string fileName, CsvColumnMappingDto mapping);
}
