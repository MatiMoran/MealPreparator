using MealPreparator.Application.Dtos;
using MealPreparator.Domain.Entities;
using MealPreparator.Domain.Services;

namespace MealPreparator.Application.Mapping;

public static class MappingExtensions
{
    public static IngredientDto ToDto(this Ingredient ingredient)
    {
        return new IngredientDto(ingredient.Id, ingredient.Name, ingredient.Unit, ingredient.ImageUrl);
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
                recipeIngredient.Ingredient.Unit,
                recipeIngredient.Ingredient.Unit.ToSpanishDisplay(recipeIngredient.Quantity),
                recipeIngredient.Ingredient.Unit == MealPreparator.Domain.Enums.UnitOfMeasure.Binary
                    ? 1m
                    : recipeIngredient.Quantity))
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
        var quantity = Math.Round(entry.TotalQuantity, 2, MidpointRounding.AwayFromZero);
        return new ShoppingListItemDto(
            entry.IngredientId,
            entry.IngredientName,
            entry.Unit,
            entry.Unit.ToSpanishDisplay(quantity),
            quantity);
    }
}
