using MealPreparator.Application.Dtos;
using MealPreparator.Domain.Entities;
using MealPreparator.Domain.Services;

namespace MealPreparator.Application.Mapping;

public static class MappingExtensions
{
    public static IngredientDto ToDto(this Ingredient ingredient)
    {
        return new IngredientDto(
            ingredient.Id,
            ingredient.Name,
            ingredient.Unit,
            ingredient.IsBinary,
            ingredient.ImageUrl,
            ingredient.UnitConversions
                .OrderBy(conversion => conversion.Unit)
                .Select(conversion => new IngredientUnitConversionDto(
                    conversion.Unit,
                    conversion.Factor,
                    conversion.Source,
                    conversion.Note))
                .ToList());
    }

    public static RecipeSummaryDto ToSummaryDto(this Recipe recipe)
    {
        return new RecipeSummaryDto(recipe.Id, recipe.Name, recipe.Servings, recipe.ImageUrl);
    }

    public static RecipeDto ToDto(this Recipe recipe)
    {
        var ingredients = recipe.Ingredients
            .Select(recipeIngredient => new RecipeIngredientDto(
                recipeIngredient.IngredientId,
                recipeIngredient.Ingredient.Name,
                recipeIngredient.Unit,
                recipeIngredient.Ingredient.IsBinary,
                recipeIngredient.Unit.ToSpanishDisplay(recipeIngredient.Quantity),
                recipeIngredient.Quantity))
            .OrderBy(ingredient => ingredient.IngredientName, StringComparer.OrdinalIgnoreCase)
            .ToList();

        return new RecipeDto(
            recipe.Id,
            recipe.Name,
            recipe.Servings,
            recipe.Instructions,
            recipe.ImageUrl,
            ingredients);
    }

    public static ShoppingListItemDto ToDto(this ShoppingListEntry entry)
    {
        decimal? quantity = entry.TotalQuantity is null
            ? null
            : Math.Round(entry.TotalQuantity.Value, 2, MidpointRounding.AwayFromZero);
        return new ShoppingListItemDto(
            entry.IngredientId,
            entry.IngredientName,
            entry.Unit,
            entry.IsBinary,
            entry.Unit.ToSpanishDisplay(quantity),
            quantity);
    }
}
