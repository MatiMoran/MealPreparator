using MealPreparator.Domain.Enums;
using MealPreparator.Domain.Exceptions;

namespace MealPreparator.Domain.Services;

public static class ShoppingListCalculator
{
    public static IReadOnlyList<ShoppingListEntry> Calculate(IEnumerable<RecipePortion> selections)
    {
        ArgumentNullException.ThrowIfNull(selections);

        var totals = new Dictionary<int, ShoppingListAccumulator>();
        var selectionCount = 0;

        foreach (var selection in selections)
        {
            selectionCount++;

            if (selection.Recipe is null)
            {
                throw new DomainValidationException("Recipe selection cannot be null.");
            }

            if (selection.Portions <= 0)
            {
                throw new DomainValidationException("Portions must be greater than zero.");
            }

            if (selection.Recipe.Servings <= 0)
            {
                throw new DomainValidationException("Recipe servings must be greater than zero.");
            }

            var scaleFactor = selection.Portions / selection.Recipe.Servings;

            foreach (var recipeIngredient in selection.Recipe.Ingredients)
            {
                if (recipeIngredient.Ingredient is null)
                {
                    throw new DomainValidationException("Recipe ingredients must include their ingredient data.");
                }

                if (!totals.TryGetValue(recipeIngredient.IngredientId, out var accumulator))
                {
                    accumulator = new ShoppingListAccumulator(
                        recipeIngredient.IngredientId,
                        recipeIngredient.Ingredient.Name,
                        recipeIngredient.Ingredient.Unit,
                        recipeIngredient.Ingredient.IsBinary);

                    totals.Add(recipeIngredient.IngredientId, accumulator);
                }

                if (!recipeIngredient.Ingredient.IsBinary)
                {
                    if (!recipeIngredient.Unit.HasValue)
                    {
                        throw new DomainValidationException("Measured recipe ingredients must have a display unit.");
                    }

                    accumulator.TotalQuantity += recipeIngredient.Quantity!.Value * scaleFactor;
                }
            }
        }

        if (selectionCount == 0)
        {
            throw new DomainValidationException("At least one recipe selection is required.");
        }

        return totals.Values
            .Select(accumulator => new ShoppingListEntry(
                accumulator.IngredientId,
                accumulator.IngredientName,
                accumulator.Unit,
                accumulator.IsBinary,
                accumulator.TotalQuantity))
            .OrderBy(entry => entry.IngredientName, StringComparer.OrdinalIgnoreCase)
            .ToList();
    }

    private sealed class ShoppingListAccumulator
    {
        public ShoppingListAccumulator(int ingredientId, string ingredientName, UnitOfMeasure? unit, bool isBinary)
        {
            IngredientId = ingredientId;
            IngredientName = ingredientName;
            Unit = unit;
            IsBinary = isBinary;
            TotalQuantity = isBinary ? null : 0m;
        }

        public int IngredientId { get; }

        public string IngredientName { get; }

        public UnitOfMeasure? Unit { get; }

        public bool IsBinary { get; }

        public decimal? TotalQuantity { get; set; }
    }
}
