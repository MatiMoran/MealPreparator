using MealPreparator.Application.Abstractions;
using MealPreparator.Application.Dtos;
using MealPreparator.Application.Exceptions;
using MealPreparator.Application.Mapping;
using MealPreparator.Application.Validation;
using MealPreparator.Domain.Services;

namespace MealPreparator.Application.Services;

public sealed class ShoppingListService
{
    private readonly IRecipeRepository _recipes;

    public ShoppingListService(IRecipeRepository recipes)
    {
        _recipes = recipes;
    }

    public async Task<IReadOnlyList<ShoppingListItemDto>> CalculateAsync(
        ShoppingListRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        if (request.Items is null || request.Items.Count == 0)
        {
            throw new ValidationException("At least one recipe selection is required.");
        }

        foreach (var item in request.Items)
        {
            if (item is null)
            {
                throw new ValidationException("Recipe selections cannot contain null entries.");
            }

            RequestValidator.RequirePositive(item.RecipeId, "Recipe id");
            RequestValidator.RequirePositive(item.Portions, "Portions");
        }

        var recipeIds = request.Items
            .Select(item => item.RecipeId)
            .Distinct()
            .ToList();

        var recipes = await _recipes.GetByIdsAsync(recipeIds, cancellationToken);
        var recipesById = recipes.ToDictionary(recipe => recipe.Id);

        var missingRecipeIds = recipeIds.Where(id => !recipesById.ContainsKey(id)).ToList();

        if (missingRecipeIds.Count > 0)
        {
            throw new ValidationException(
                $"The following recipes do not exist: {string.Join(", ", missingRecipeIds)}.");
        }

        var selections = request.Items.Select(item =>
            new RecipePortion(recipesById[item.RecipeId], item.Portions));

        var entries = ShoppingListCalculator.Calculate(selections);

        return entries.Select(entry => entry.ToDto()).ToList();
    }
}
