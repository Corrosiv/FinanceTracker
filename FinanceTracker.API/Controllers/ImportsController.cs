using System.Text.Json;
using Microsoft.AspNetCore.Mvc;
using FinanceTracker.API.DTOs;
using FinanceTracker.API.Services;

namespace FinanceTracker.API.Controllers;

[ApiController]
[Route("api/v1/imports")]
public class ImportsController : ControllerBase
{
    private readonly IImportService _importService;

    public ImportsController(IImportService importService)
    {
        _importService = importService;
    }

    [HttpPost]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Import(
        IFormFile file,
        [FromForm] string columnMapping)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { errors = new[] { "A CSV file is required." } });

        if (!file.FileName.EndsWith(".csv", StringComparison.OrdinalIgnoreCase))
            return BadRequest(new { errors = new[] { "Only CSV files are supported." } });

        CsvColumnMappingDto? mapping;
        try
        {
            mapping = JsonSerializer.Deserialize<CsvColumnMappingDto>(columnMapping, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            });
        }
        catch (JsonException)
        {
            return BadRequest(new { errors = new[] { "Invalid column mapping JSON." } });
        }

        if (mapping is null)
            return BadRequest(new { errors = new[] { "Column mapping is required." } });

        var validationErrors = ValidateMapping(mapping);
        if (validationErrors.Count > 0)
            return BadRequest(new { errors = validationErrors });

        using var stream = file.OpenReadStream();
        var result = await _importService.ImportCsvAsync(stream, file.FileName, mapping);

        return Ok(result);
    }

    private static List<string> ValidateMapping(CsvColumnMappingDto mapping)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(mapping.Date))
            errors.Add("Column mapping must include 'date'.");

        if (string.IsNullOrWhiteSpace(mapping.Description))
            errors.Add("Column mapping must include 'description'.");

        if (string.IsNullOrWhiteSpace(mapping.Amount)
            && string.IsNullOrWhiteSpace(mapping.Debit)
            && string.IsNullOrWhiteSpace(mapping.Credit))
        {
            errors.Add("Column mapping must include either 'amount' or 'debit'/'credit'.");
        }

        return errors;
    }
}
