namespace MealPreparator.Application.Validation;

public static class RequestValidator
{
    public static string RequireName(string? value, string fieldName, int maxLength = 200)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            throw new Exceptions.ValidationException($"{fieldName} is required.");
        }

        var trimmed = value.Trim();

        if (trimmed.Length > maxLength)
        {
            throw new Exceptions.ValidationException($"{fieldName} cannot exceed {maxLength} characters.");
        }

        return trimmed;
    }

    public static string? NormalizeOptionalText(string? value, string fieldName, int maxLength)
    {
        if (value is null)
        {
            return null;
        }

        if (value.Length > maxLength)
        {
            throw new Exceptions.ValidationException($"{fieldName} cannot exceed {maxLength} characters.");
        }

        return string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }

    public static void RequirePositive(int value, string fieldName)
    {
        if (value <= 0)
        {
            throw new Exceptions.ValidationException($"{fieldName} must be greater than zero.");
        }
    }

    public static void RequirePositive(decimal value, string fieldName)
    {
        if (value <= 0)
        {
            throw new Exceptions.ValidationException($"{fieldName} must be greater than zero.");
        }
    }
}
