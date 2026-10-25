using MealPreparator.Domain.Enums;
using MealPreparator.Domain.Exceptions;

namespace MealPreparator.Domain.Entities;

public sealed class Ingredient
{
    public int Id { get; internal set; }

    public string Name { get; private set; } = string.Empty;

    public UnitOfMeasure Unit { get; private set; }

    public string? ImageUrl { get; private set; }

    private Ingredient()
    {
    }

    public Ingredient(string name, UnitOfMeasure unit, string? imageUrl = null)
    {
        Update(name, unit);
        SetImageUrl(imageUrl);
    }

    public void Update(string name, UnitOfMeasure unit)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainValidationException("Ingredient name is required.");
        }

        var trimmedName = name.Trim();

        if (trimmedName.Length > 200)
        {
            throw new DomainValidationException("Ingredient name cannot exceed 200 characters.");
        }

        if (unit == UnitOfMeasure.Unknown || !Enum.IsDefined(unit))
        {
            throw new DomainValidationException("Ingredient unit is not valid.");
        }

        Name = trimmedName;
        Unit = unit;
    }

    public void SetImageUrl(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            ImageUrl = null;
            return;
        }

        var trimmed = imageUrl.Trim();

        if (trimmed.Length > 500)
        {
            throw new DomainValidationException("Ingredient image URL cannot exceed 500 characters.");
        }

        ImageUrl = trimmed;
    }
}
