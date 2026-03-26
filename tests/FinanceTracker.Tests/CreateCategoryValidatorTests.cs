using Xunit;
using FinanceTracker.API.DTOs;
using FinanceTracker.API.Validators;

namespace FinanceTracker.Tests;

public class CreateCategoryValidatorTests
{
    [Fact]
    public void ValidDto_ReturnsNoErrors()
    {
        var dto = new CreateCategoryDto { Name = "Groceries", Description = "Food" };
        var errors = CreateCategoryValidator.Validate(dto);
        Assert.Empty(errors);
    }

    [Fact]
    public void NullName_ReturnsError()
    {
        var dto = new CreateCategoryDto { Name = null! };
        var errors = CreateCategoryValidator.Validate(dto);
        Assert.Single(errors);
        Assert.Contains("Name is required.", errors);
    }

    [Fact]
    public void EmptyName_ReturnsError()
    {
        var dto = new CreateCategoryDto { Name = "" };
        var errors = CreateCategoryValidator.Validate(dto);
        Assert.Single(errors);
        Assert.Contains("Name is required.", errors);
    }

    [Fact]
    public void WhitespaceName_ReturnsError()
    {
        var dto = new CreateCategoryDto { Name = "   " };
        var errors = CreateCategoryValidator.Validate(dto);
        Assert.Single(errors);
        Assert.Contains("Name is required.", errors);
    }

    [Fact]
    public void NameExceeds255Chars_ReturnsError()
    {
        var dto = new CreateCategoryDto { Name = new string('A', 256) };
        var errors = CreateCategoryValidator.Validate(dto);
        Assert.Single(errors);
        Assert.Contains("Name must be 255 characters or fewer.", errors);
    }

    [Fact]
    public void NameExactly255Chars_ReturnsNoErrors()
    {
        var dto = new CreateCategoryDto { Name = new string('A', 255) };
        var errors = CreateCategoryValidator.Validate(dto);
        Assert.Empty(errors);
    }

    [Fact]
    public void NullDescription_IsAllowed()
    {
        var dto = new CreateCategoryDto { Name = "Food", Description = null };
        var errors = CreateCategoryValidator.Validate(dto);
        Assert.Empty(errors);
    }
}
