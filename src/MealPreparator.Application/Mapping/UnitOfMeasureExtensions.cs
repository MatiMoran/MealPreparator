using MealPreparator.Domain.Enums;

namespace MealPreparator.Application.Mapping;

public static class UnitOfMeasureExtensions
{
    public static string ToSpanishDisplay(this UnitOfMeasure unit, decimal quantity)
    {
        var isPlural = Math.Abs(quantity) != 1m;

        return unit switch
        {
            UnitOfMeasure.Gram => "g",
            UnitOfMeasure.Kilogram => isPlural ? "kg" : "kg",
            UnitOfMeasure.Milliliter => "ml",
            UnitOfMeasure.Liter => isPlural ? "l" : "l",
            UnitOfMeasure.Piece => isPlural ? "unidades" : "unidad",
            UnitOfMeasure.Tablespoon => isPlural ? "cucharadas" : "cucharada",
            UnitOfMeasure.Teaspoon => isPlural ? "cucharaditas" : "cucharadita",
            UnitOfMeasure.Cup => isPlural ? "tazas" : "taza",
            UnitOfMeasure.Unknown => "unidad",
            _ => unit.ToString().ToLowerInvariant()
        };
    }
}
