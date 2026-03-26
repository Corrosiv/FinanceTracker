using FinanceTracker.API.DTOs;

namespace FinanceTracker.API.Validators;

public static class CreateExpenseValidator
{
    public static List<string> Validate(CreateExpenseDto dto)
    {
        var errors = new List<string>();

        if (dto.Amount == 0)
            errors.Add("Amount must not be zero.");

        if (string.IsNullOrWhiteSpace(dto.Description))
            errors.Add("Description is required.");

        if (dto.Date == default)
            errors.Add("Date is required.");

        return errors;
    }
}
