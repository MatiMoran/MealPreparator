using MealPreparator.Domain.Exceptions;

namespace MealPreparator.Domain.Entities;

public sealed class Tag
{
    public int Id { get; internal set; }

    public string Name { get; private set; } = string.Empty;

    private Tag()
    {
    }

    public Tag(string name)
    {
        Name = ValidateName(name);
    }

    public void Rename(string name)
    {
        Name = ValidateName(name);
    }

    private static string ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainValidationException("Tag name is required.");
        }

        var trimmedName = name.Trim();
        if (trimmedName.Length > 50)
        {
            throw new DomainValidationException("Tag name cannot exceed 50 characters.");
        }

        return trimmedName;
    }
}
