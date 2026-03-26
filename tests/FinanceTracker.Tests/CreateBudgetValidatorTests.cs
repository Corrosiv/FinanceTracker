using System;
using Xunit;
using FinanceTracker.API.DTOs;
using FinanceTracker.API.Validators;

namespace FinanceTracker.Tests;

public class CreateBudgetValidatorTests
{
    [Fact]
    public void WhenValidDto_ShouldReturnNoErrors()
    {
        var dto = new CreateBudgetDto
        {
            CategoryId = Guid.NewGuid(),
            Period = "Monthly",
            LimitAmount = 500m
        };
        var errors = CreateBudgetValidator.Validate(dto);
        Assert.Empty(errors);
    }

    [Fact]
    public void WhenCategoryIdIsEmpty_ShouldReturnError()
    {
        var dto = new CreateBudgetDto
        {
            CategoryId = Guid.Empty,
            Period = "Monthly",
            LimitAmount = 500m
        };
        var errors = CreateBudgetValidator.Validate(dto);
        Assert.Contains("CategoryId is required.", errors);
    }

    [Fact]
    public void WhenPeriodIsInvalid_ShouldReturnError()
    {
        var dto = new CreateBudgetDto
        {
            CategoryId = Guid.NewGuid(),
            Period = "Daily",
            LimitAmount = 500m
        };
        var errors = CreateBudgetValidator.Validate(dto);
        Assert.Contains("Period must be one of: Weekly, Monthly, Yearly.", errors);
    }

    [Fact]
    public void WhenLimitAmountIsZero_ShouldReturnError()
    {
        var dto = new CreateBudgetDto
        {
            CategoryId = Guid.NewGuid(),
            Period = "Monthly",
            LimitAmount = 0m
        };
        var errors = CreateBudgetValidator.Validate(dto);
        Assert.Contains("LimitAmount must be greater than zero.", errors);
    }

    [Fact]
    public void WhenLimitAmountIsNegative_ShouldReturnError()
    {
        var dto = new CreateBudgetDto
        {
            CategoryId = Guid.NewGuid(),
            Period = "Monthly",
            LimitAmount = -100m
        };
        var errors = CreateBudgetValidator.Validate(dto);
        Assert.Contains("LimitAmount must be greater than zero.", errors);
    }

    [Fact]
    public void WhenPeriodIsCaseInsensitive_ShouldAccept()
    {
        var dto = new CreateBudgetDto
        {
            CategoryId = Guid.NewGuid(),
            Period = "monthly",
            LimitAmount = 500m
        };
        var errors = CreateBudgetValidator.Validate(dto);
        Assert.Empty(errors);
    }

    [Fact]
    public void WhenAllFieldsInvalid_ShouldReturnMultipleErrors()
    {
        var dto = new CreateBudgetDto
        {
            CategoryId = Guid.Empty,
            Period = "",
            LimitAmount = -1m
        };
        var errors = CreateBudgetValidator.Validate(dto);
        Assert.Equal(3, errors.Count);
    }
}
