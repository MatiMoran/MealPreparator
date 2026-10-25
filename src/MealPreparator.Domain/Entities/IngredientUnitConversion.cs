using MealPreparator.Domain.Enums;
using MealPreparator.Domain.Exceptions;

namespace MealPreparator.Domain.Entities;

public sealed class IngredientUnitConversion
{
    public int IngredientId { get; internal set; }

    public Ingredient Ingredient { get; internal set; } = null!;

    public UnitOfMeasure Unit { get; private set; }

    public decimal Factor { get; private set; }

    public string? Source { get; private set; }

    public string? Note { get; private set; }

    private IngredientUnitConversion()
    {
    }

    public IngredientUnitConversion(
        UnitOfMeasure unit,
        decimal factor,
        string? source = null,
        string? note = null)
    {
        Update(unit, factor, source, note);
    }

    public void Update(UnitOfMeasure unit, decimal factor, string? source, string? note)
    {
        if (unit == UnitOfMeasure.Unknown || !Enum.IsDefined(unit))
        {
            throw new DomainValidationException("Conversion unit is not valid.");
        }

        if (factor <= 0 || factor > 1_000_000)
        {
            throw new DomainValidationException("Conversion factor must be greater than zero and no larger than 1,000,000.");
        }

        if ((source is not null && source.Length > 1000) || (note is not null && note.Length > 500))
        {
            throw new DomainValidationException("Conversion source or note is too long.");
        }

        Unit = unit;
        Factor = factor;
        Source = string.IsNullOrWhiteSpace(source) ? null : source.Trim();
        Note = string.IsNullOrWhiteSpace(note) ? null : note.Trim();
    }
}
