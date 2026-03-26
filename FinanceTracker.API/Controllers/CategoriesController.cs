using Microsoft.AspNetCore.Mvc;
using FinanceTracker.API.DTOs;
using FinanceTracker.API.Models;
using FinanceTracker.API.Services;
using FinanceTracker.API.Validators;

namespace FinanceTracker.API.Controllers;

[ApiController]
[Route("api/v1/categories")]
public class CategoriesController : ControllerBase
{
    private readonly ICategoryService _categoryService;

    // V1: single implicit user
    private static readonly Guid DefaultUserId = Guid.Parse("00000000-0000-0000-0000-000000000001");

    public CategoriesController(ICategoryService categoryService)
    {
        _categoryService = categoryService;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var categories = await _categoryService.GetAllAsync();
        var result = categories.Select(c => new CategoryResponseDto
        {
            Id = c.Id,
            Name = c.Name,
            Description = c.Description
        });
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetById(Guid id)
    {
        var category = await _categoryService.GetByIdAsync(id);
        if (category is null) return NotFound();

        return Ok(new CategoryResponseDto
        {
            Id = category.Id,
            Name = category.Name,
            Description = category.Description
        });
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateCategoryDto dto)
    {
        var errors = CreateCategoryValidator.Validate(dto);
        if (errors.Count > 0)
            return BadRequest(new { errors });

        var category = new Category
        {
            UserId = DefaultUserId,
            Name = dto.Name,
            Description = dto.Description
        };

        var created = await _categoryService.CreateAsync(category);

        var response = new CategoryResponseDto
        {
            Id = created.Id,
            Name = created.Name,
            Description = created.Description
        };

        return CreatedAtAction(nameof(GetById), new { id = created.Id }, response);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        var deleted = await _categoryService.DeleteAsync(id);
        if (!deleted) return NotFound();
        return NoContent();
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateCategoryDto dto)
    {
        if (dto.Name is null && dto.Description is null)
            return BadRequest(new { errors = new[] { "At least one field (name or description) must be provided." } });

        var updated = await _categoryService.UpdateAsync(id, dto.Name, dto.Description);
        if (updated is null) return NotFound();

        return Ok(new CategoryResponseDto
        {
            Id = updated.Id,
            Name = updated.Name,
            Description = updated.Description
        });
    }
}
