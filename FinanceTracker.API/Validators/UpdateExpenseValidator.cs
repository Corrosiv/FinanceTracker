using FinanceTracker.API.DTOs;

namespace FinanceTracker.API.Validators;

public static class UpdateExpenseValidator
{
    public static List<string> Validate(UpdateExpenseDto dto)
    {
        var errors = new List<string>();

        if (dto.Amount.HasValue && dto.Amount.Value == 0)
            errors.Add("Amount must not be zero.");

        if (dto.Description is not null && string.IsNullOrWhiteSpace(dto.Description))
            errors.Add("Description must not be empty.");

        return errors;
    }
}
