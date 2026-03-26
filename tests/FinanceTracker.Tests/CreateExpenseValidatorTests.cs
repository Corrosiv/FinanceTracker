using System;
using Xunit;
using FinanceTracker.API.DTOs;
using FinanceTracker.API.Validators;

namespace FinanceTracker.Tests;

public class CreateExpenseValidatorTests
{
    [Fact]
    public void ValidDto_ReturnsNoErrors()
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
    public void ZeroAmount_ReturnsError()
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
    public void NullDescription_ReturnsError()
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
    public void EmptyDescription_ReturnsError()
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
    public void WhitespaceDescription_ReturnsError()
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
    public void DefaultDate_ReturnsError()
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
    public void AllFieldsInvalid_ReturnsMultipleErrors()
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
    public void NegativeAmount_IsAllowed()
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
