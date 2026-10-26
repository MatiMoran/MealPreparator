namespace MealPreparator.Domain.Enums;

public static class UnitOfMeasureRules
{
    public static IngredientCategory GetCategory(UnitOfMeasure unit) => unit switch
    {
        UnitOfMeasure.Piece => IngredientCategory.Unit,
        UnitOfMeasure.Gram or UnitOfMeasure.Kilogram => IngredientCategory.Weight,
        UnitOfMeasure.Milliliter or UnitOfMeasure.Liter or UnitOfMeasure.Tablespoon or
            UnitOfMeasure.Teaspoon or UnitOfMeasure.Cup => IngredientCategory.Volume,
        _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unknown unit of measure.")
    };

    public static UnitOfMeasure ToCanonicalUnit(UnitOfMeasure unit) => GetCategory(unit) switch
    {
        IngredientCategory.Unit => UnitOfMeasure.Piece,
        IngredientCategory.Weight => UnitOfMeasure.Kilogram,
        IngredientCategory.Volume => UnitOfMeasure.Liter,
        _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unknown unit of measure.")
    };

    public static decimal GetFactorToCanonical(UnitOfMeasure canonicalUnit, UnitOfMeasure unit)
    {
        if (GetCategory(canonicalUnit) != GetCategory(unit))
        {
            throw new ArgumentException("Units from different categories require an ingredient-specific conversion.", nameof(unit));
        }

        return canonicalUnit switch
        {
            UnitOfMeasure.Kilogram => unit switch
            {
                UnitOfMeasure.Kilogram => 1m,
                UnitOfMeasure.Gram => 0.001m,
                _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unsupported weight unit.")
            },
            UnitOfMeasure.Liter => unit switch
            {
                UnitOfMeasure.Liter => 1m,
                UnitOfMeasure.Milliliter => 0.001m,
                UnitOfMeasure.Tablespoon => 0.015m,
                UnitOfMeasure.Teaspoon => 0.005m,
                UnitOfMeasure.Cup => 0.24m,
                _ => throw new ArgumentOutOfRangeException(nameof(unit), unit, "Unsupported volume unit.")
            },
            UnitOfMeasure.Piece when unit == UnitOfMeasure.Piece => 1m,
            _ => throw new ArgumentOutOfRangeException(nameof(canonicalUnit), canonicalUnit, "Unit is not canonical.")
        };
    }
}
