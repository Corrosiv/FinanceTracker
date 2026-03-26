using FinanceTracker.API.DTOs;

namespace FinanceTracker.API.Validators;

public static class CreateCategoryValidator
{
    public static List<string> Validate(CreateCategoryDto dto)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(dto.Name))
            errors.Add("Name is required.");

        if (dto.Name?.Length > 255)
            errors.Add("Name must be 255 characters or fewer.");

        return errors;
    }
}
