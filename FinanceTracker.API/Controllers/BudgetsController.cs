using Microsoft.AspNetCore.Mvc;
using FinanceTracker.API.DTOs;
using FinanceTracker.API.Models;
using FinanceTracker.API.Services;
using FinanceTracker.API.Validators;

namespace FinanceTracker.API.Controllers;

[ApiController]
[Route("api/v1/budgets")]
public class BudgetsController : ControllerBase
{
    private readonly IBudgetService _budgetService;

    private static readonly Guid DefaultUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public BudgetsController(IBudgetService budgetService)
    {
        _budgetService = budgetService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var budgets = await _budgetService.GetAllAsync();
        var result = new List<BudgetResponseDto>();

        foreach (var b in budgets)
        {
            var spent = await _budgetService.GetSpentAmountAsync(b.UserId, b.CategoryId, b.Period);
            result.Add(ToDto(b, spent));
        }

        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var budget = await _budgetService.GetByIdAsync(id);
        if (budget is null) return NotFound();

        var spent = await _budgetService.GetSpentAmountAsync(budget.UserId, budget.CategoryId, budget.Period);
        return Ok(ToDto(budget, spent));
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateBudgetDto dto)
    {
        var errors = CreateBudgetValidator.Validate(dto);
        if (errors.Count > 0)
            return BadRequest(new { errors });

        var period = Enum.Parse<BudgetPeriod>(dto.Period, ignoreCase: true);

        var budget = new Budget
        {
            UserId = DefaultUserId,
            CategoryId = dto.CategoryId,
            Period = period,
            LimitAmount = dto.LimitAmount
        };

        var created = await _budgetService.CreateAsync(budget);
        var createdWithCategory = await _budgetService.GetByIdAsync(created.Id);
        var spent = await _budgetService.GetSpentAmountAsync(created.UserId, created.CategoryId, created.Period);

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, ToDto(createdWithCategory!, spent));
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateBudgetDto dto)
    {
        if (dto.LimitAmount.HasValue && dto.LimitAmount.Value <= 0)
            return BadRequest(new { errors = new[] { "LimitAmount must be greater than zero." } });

        var updated = await _budgetService.UpdateAsync(id, dto.LimitAmount);
        if (updated is null) return NotFound();

        var withCategory = await _budgetService.GetByIdAsync(id);
        var spent = await _budgetService.GetSpentAmountAsync(updated.UserId, updated.CategoryId, updated.Period);
        return Ok(ToDto(withCategory!, spent));
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _budgetService.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }

    [HttpGet("suggestions")]
    public async Task<IActionResult> GetSuggestions()
    {
        var suggestions = await _budgetService.SuggestBudgetsAsync(DefaultUserId);
        return Ok(suggestions);
    }

    [HttpGet("alerts")]
    public async Task<IActionResult> GetAlerts([FromQuery] decimal threshold = 80)
    {
        var alerts = await _budgetService.GetAlertsAsync(DefaultUserId, threshold);
        return Ok(alerts);
    }

    private static BudgetResponseDto ToDto(Budget b, decimal spent) => new()
    {
        Id = b.Id,
        CategoryId = b.CategoryId,
        CategoryName = b.Category?.Name ?? "Unknown",
        Period = b.Period.ToString(),
        LimitAmount = b.LimitAmount,
        SpentAmount = spent,
        RemainingAmount = Math.Max(0, b.LimitAmount - spent),
        IsOverBudget = spent > b.LimitAmount
    };
}
