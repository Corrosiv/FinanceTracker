using System;
using Xunit;
using FinanceTracker.API.DTOs;
using FinanceTracker.API.Validators;

namespace FinanceTracker.Tests;

public class CreateExpenseValidatorTests
{
    [Fact]
    public void WhenValidatingValidDto_ShouldReturnNoErrors()
    {
        var dto = new CreateExpenseDto
        {
            Amount = 50m,
            Description = "Lunch",
            Date = new DateTime(2026, 3, 20)
        };
        var errors = CreateExpenseValidator.Validate(dto);
        Assert.Empty(errors);
    }

    [Fact]
    public void WhenAmountIsZero_ShouldReturnError()
    {
        var dto = new CreateExpenseDto
        {
            Amount = 0m,
            Description = "Lunch",
            Date = new DateTime(2026, 3, 20)
        };
        var errors = CreateExpenseValidator.Validate(dto);
        Assert.Single(errors);
        Assert.Contains("Amount must not be zero.", errors);
    }

    [Fact]
    public void WhenDescriptionIsNull_ShouldReturnError()
    {
        var dto = new CreateExpenseDto
        {
            Amount = 10m,
            Description = null!,
            Date = new DateTime(2026, 3, 20)
        };
        var errors = CreateExpenseValidator.Validate(dto);
        Assert.Single(errors);
        Assert.Contains("Description is required.", errors);
    }

    [Fact]
    public void WhenDescriptionIsEmpty_ShouldReturnError()
    {
        var dto = new CreateExpenseDto
        {
            Amount = 10m,
            Description = "",
            Date = new DateTime(2026, 3, 20)
        };
        var errors = CreateExpenseValidator.Validate(dto);
        Assert.Single(errors);
        Assert.Contains("Description is required.", errors);
    }

    [Fact]
    public void WhenDescriptionIsWhitespace_ShouldReturnError()
    {
        var dto = new CreateExpenseDto
        {
            Amount = 10m,
            Description = "   ",
            Date = new DateTime(2026, 3, 20)
        };
        var errors = CreateExpenseValidator.Validate(dto);
        Assert.Single(errors);
        Assert.Contains("Description is required.", errors);
    }

    [Fact]
    public void WhenDateIsDefault_ShouldReturnError()
    {
        var dto = new CreateExpenseDto
        {
            Amount = 10m,
            Description = "Lunch",
            Date = default
        };
        var errors = CreateExpenseValidator.Validate(dto);
        Assert.Single(errors);
        Assert.Contains("Date is required.", errors);
    }

    [Fact]
    public void WhenAllFieldsAreInvalid_ShouldReturnMultipleErrors()
    {
        var dto = new CreateExpenseDto
        {
            Amount = 0m,
            Description = "",
            Date = default
        };
        var errors = CreateExpenseValidator.Validate(dto);
        Assert.Equal(3, errors.Count);
    }

    [Fact]
    public void WhenAmountIsNegative_ShouldBeAllowed()
    {
        var dto = new CreateExpenseDto
        {
            Amount = -25m,
            Description = "Refund",
            Date = new DateTime(2026, 3, 20)
        };
        var errors = CreateExpenseValidator.Validate(dto);
        Assert.Empty(errors);
    }
}
