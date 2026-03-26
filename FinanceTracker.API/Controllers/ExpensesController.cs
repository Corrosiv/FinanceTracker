using Microsoft.AspNetCore.Mvc;
using FinanceTracker.API.DTOs;
using FinanceTracker.API.Models;
using FinanceTracker.API.Services;
using FinanceTracker.API.Validators;

namespace FinanceTracker.API.Controllers;

[ApiController]
[Route("api/v1/expenses")]
public class ExpensesController : ControllerBase
{
    private readonly IExpenseService _expenseService;

    // V1: single implicit user
    private static readonly Guid DefaultUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    // V1: expenses entered manually use a placeholder import
    private static readonly Guid ManualImportId = Guid.Parse("00000000-0000-0000-0000-000000000002");

    public ExpensesController(IExpenseService expenseService)
    {
        _expenseService = expenseService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var expenses = await _expenseService.GetAllAsync();
        var result = expenses.Select(t => ToDto(t));
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var tx = await _expenseService.GetByIdAsync(id);
        if (tx is null) return NotFound();
        return Ok(ToDto(tx));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateExpenseDto dto)
    {
        var errors = CreateExpenseValidator.Validate(dto);
        if (errors.Count > 0)
            return BadRequest(new { errors });

        var transaction = new Transaction
        {
            UserId = DefaultUserId,
            ImportId = ManualImportId,
            Date = dto.Date,
            Amount = -Math.Abs(dto.Amount), // expenses are negative
            RawDescription = dto.Description,
            NormalizedDescription = dto.Description.Trim().ToLowerInvariant(),
            CategoryId = dto.CategoryId
        };

        var created = await _expenseService.CreateAsync(transaction);
        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ToDto(created));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateExpenseDto dto)
    {
        var errors = UpdateExpenseValidator.Validate(dto);
        if (errors.Count > 0)
            return BadRequest(new { errors });

        var updated = await _expenseService.UpdateAsync(
            id,
            dto.Amount.HasValue ? -Math.Abs(dto.Amount.Value) : null,
            dto.Description,
            dto.Date,
            dto.CategoryId);

        if (updated is null) return NotFound();
        return Ok(ToDto(updated));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _expenseService.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }

    private static ExpenseResponseDto ToDto(Transaction t) => new()
    {
        Id = t.Id,
        Amount = Math.Abs(t.Amount),
        Description = t.RawDescription,
        Date = t.Date,
        CategoryId = t.CategoryId
    };
}
