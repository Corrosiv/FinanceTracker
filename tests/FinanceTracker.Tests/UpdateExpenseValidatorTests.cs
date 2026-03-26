using System;
using Xunit;
using FinanceTracker.API.DTOs;
using FinanceTracker.API.Validators;

namespace FinanceTracker.Tests;

public class UpdateExpenseValidatorTests
{
    [Fact]
    public void WhenAllFieldsAreNull_ShouldReturnNoErrors()
    {
        var dto = new UpdateExpenseDto();
        var errors = UpdateExpenseValidator.Validate(dto);
        Assert.Empty(errors);
    }

    [Fact]
    public void WhenValidFieldsProvided_ShouldReturnNoErrors()
    {
        var dto = new UpdateExpenseDto
        {
            Amount = 25m,
            Description = "Updated lunch"
        };
        var errors = UpdateExpenseValidator.Validate(dto);
        Assert.Empty(errors);
    }

    [Fact]
    public void WhenAmountIsZero_ShouldReturnError()
    {
        var dto = new UpdateExpenseDto { Amount = 0m };
        var errors = UpdateExpenseValidator.Validate(dto);
        Assert.Single(errors);
        Assert.Contains("Amount must not be zero.", errors);
    }

    [Fact]
    public void WhenDescriptionIsEmpty_ShouldReturnError()
    {
        var dto = new UpdateExpenseDto { Description = "" };
        var errors = UpdateExpenseValidator.Validate(dto);
        Assert.Single(errors);
        Assert.Contains("Description must not be empty.", errors);
    }

    [Fact]
    public void WhenDescriptionIsWhitespace_ShouldReturnError()
    {
        var dto = new UpdateExpenseDto { Description = "   " };
        var errors = UpdateExpenseValidator.Validate(dto);
        Assert.Single(errors);
        Assert.Contains("Description must not be empty.", errors);
    }

    [Fact]
    public void WhenAmountIsZeroAndDescriptionIsEmpty_ShouldReturnTwoErrors()
    {
        var dto = new UpdateExpenseDto
        {
            Amount = 0m,
            Description = ""
        };
        var errors = UpdateExpenseValidator.Validate(dto);
        Assert.Equal(2, errors.Count);
        Assert.Contains("Amount must not be zero.", errors);
        Assert.Contains("Description must not be empty.", errors);
    }

    [Fact]
    public void WhenAmountIsNullAndDescriptionIsNull_ShouldNotValidateThem()
    {
        var dto = new UpdateExpenseDto
        {
            Date = new DateTime(2026, 5, 1),
            CategoryId = Guid.NewGuid()
        };
        var errors = UpdateExpenseValidator.Validate(dto);
        Assert.Empty(errors);
    }
}
