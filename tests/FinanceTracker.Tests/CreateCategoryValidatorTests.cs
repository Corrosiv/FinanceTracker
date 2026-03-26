using Xunit;
using FinanceTracker.API.DTOs;
using FinanceTracker.API.Validators;

namespace FinanceTracker.Tests;

public class CreateCategoryValidatorTests
{
    [Fact]
    public void WhenValidatingValidDto_ShouldReturnNoErrors()
    {
        var dto = new CreateCategoryDto { Name = "Groceries", Description = "Food" };
        var errors = CreateCategoryValidator.Validate(dto);
        Assert.Empty(errors);
    }

    [Fact]
    public void WhenNameIsNull_ShouldReturnError()
    {
        var dto = new CreateCategoryDto { Name = null! };
        var errors = CreateCategoryValidator.Validate(dto);
        Assert.Single(errors);
        Assert.Contains("Name is required.", errors);
    }

    [Fact]
    public void WhenNameIsEmpty_ShouldReturnError()
    {
        var dto = new CreateCategoryDto { Name = "" };
        var errors = CreateCategoryValidator.Validate(dto);
        Assert.Single(errors);
        Assert.Contains("Name is required.", errors);
    }

    [Fact]
    public void WhenNameIsWhitespace_ShouldReturnError()
    {
        var dto = new CreateCategoryDto { Name = "   " };
        var errors = CreateCategoryValidator.Validate(dto);
        Assert.Single(errors);
        Assert.Contains("Name is required.", errors);
    }

    [Fact]
    public void WhenNameExceeds255Chars_ShouldReturnError()
    {
        var dto = new CreateCategoryDto { Name = new string('A', 256) };
        var errors = CreateCategoryValidator.Validate(dto);
        Assert.Single(errors);
        Assert.Contains("Name must be 255 characters or fewer.", errors);
    }

    [Fact]
    public void WhenNameIsExactly255Chars_ShouldReturnNoErrors()
    {
        var dto = new CreateCategoryDto { Name = new string('A', 255) };
        var errors = CreateCategoryValidator.Validate(dto);
        Assert.Empty(errors);
    }

    [Fact]
    public void WhenDescriptionIsNull_ShouldBeAllowed()
    {
        var dto = new CreateCategoryDto { Name = "Food", Description = null };
        var errors = CreateCategoryValidator.Validate(dto);
        Assert.Empty(errors);
    }
}
