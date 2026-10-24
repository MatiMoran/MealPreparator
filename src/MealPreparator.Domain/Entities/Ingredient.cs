using MealPreparator.Domain.Enums;
using MealPreparator.Domain.Exceptions;

namespace MealPreparator.Domain.Entities;

public sealed class Ingredient
{
    public int Id { get; internal set; }

    public string Name { get; private set; } = string.Empty;

    public UnitOfMeasure Unit { get; private set; }

    private Ingredient()
    {
    }

    public Ingredient(string name, UnitOfMeasure unit)
    {
        Update(name, unit);
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
}
