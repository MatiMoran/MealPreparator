using MealPreparator.Application.Abstractions;
using MealPreparator.Application.Dtos;
using MealPreparator.Application.Exceptions;
using MealPreparator.Application.Mapping;
using MealPreparator.Application.Validation;
using MealPreparator.Domain.Entities;

namespace MealPreparator.Application.Services;

public sealed class RecipeService
{
    private readonly IRecipeRepository _recipes;
    private readonly IIngredientRepository _ingredients;
    private readonly IUnitOfWork _unitOfWork;

    public RecipeService(
        IRecipeRepository recipes,
        IIngredientRepository ingredients,
        IUnitOfWork unitOfWork)
    {
        _recipes = recipes;
        _ingredients = ingredients;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<RecipeSummaryDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var recipes = await _recipes.GetAllAsync(cancellationToken);
        return recipes.Select(recipe => recipe.ToSummaryDto()).ToList();
    }

    public async Task<RecipeDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var recipe = await GetEntityAsync(id, cancellationToken);
        return recipe.ToDto();
    }

    public async Task<RecipeDto> CreateAsync(
        CreateRecipeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var validated = ValidateRequest(
            request.Name,
            request.Servings,
            request.Instructions,
            request.Ingredients);

        var ingredientsById = await LoadIngredientsAsync(validated.Ingredients, cancellationToken);

        var recipe = new Recipe(validated.Name, validated.Servings, validated.Instructions);
        recipe.ReplaceIngredients(validated.Ingredients.Select(item =>
            new RecipeIngredient(ingredientsById[item.IngredientId], item.Quantity)));

        await _recipes.AddAsync(recipe, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return recipe.ToDto();
    }

    public async Task<RecipeDto> UpdateAsync(
        int id,
        UpdateRecipeRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var recipe = await GetEntityAsync(id, cancellationToken);

        var validated = ValidateRequest(
            request.Name,
            request.Servings,
            request.Instructions,
            request.Ingredients);

        var ingredientsById = await LoadIngredientsAsync(validated.Ingredients, cancellationToken);
        var desiredIngredientIds = validated.Ingredients
            .Select(item => item.IngredientId)
            .ToHashSet();

        foreach (var existing in recipe.Ingredients.ToList())
        {
            if (!desiredIngredientIds.Contains(existing.IngredientId))
            {
                recipe.RemoveIngredient(existing.IngredientId);
            }
        }

        foreach (var item in validated.Ingredients)
        {
            var existing = recipe.FindIngredient(item.IngredientId);

            if (existing is null)
            {
                recipe.AddIngredient(new RecipeIngredient(ingredientsById[item.IngredientId], item.Quantity));
            }
            else
            {
                existing.SetQuantity(item.Quantity);
            }
        }

        recipe.UpdateDetails(validated.Name, validated.Servings, validated.Instructions);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return recipe.ToDto();
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var recipe = await GetEntityAsync(id, cancellationToken);
        _recipes.Remove(recipe);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Recipe> GetEntityAsync(int id, CancellationToken cancellationToken)
    {
        return await _recipes.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Recipe with id {id} was not found.");
    }

    private async Task<IReadOnlyDictionary<int, Ingredient>> LoadIngredientsAsync(
        IReadOnlyList<RecipeIngredientRequest> items,
        CancellationToken cancellationToken)
    {
        var ingredientIds = items
            .Select(item => item.IngredientId)
            .Distinct()
            .ToList();

        var ingredients = await _ingredients.GetByIdsAsync(ingredientIds, cancellationToken);
        var ingredientsById = ingredients.ToDictionary(ingredient => ingredient.Id);

        var missingIds = ingredientIds.Where(id => !ingredientsById.ContainsKey(id)).ToList();

        if (missingIds.Count > 0)
        {
            throw new ValidationException(
                $"The following ingredients do not exist: {string.Join(", ", missingIds)}.");
        }

        return ingredientsById;
    }

    private static ValidatedRecipe ValidateRequest(
        string? name,
        int servings,
        string? instructions,
        IReadOnlyList<RecipeIngredientRequest>? items)
    {
        var normalizedName = RequestValidator.RequireName(name, nameof(name));
        RequestValidator.RequirePositive(servings, nameof(servings));
        var normalizedInstructions = RequestValidator.NormalizeOptionalText(
            instructions,
            nameof(instructions),
            4000);

        if (items is null || items.Count == 0)
        {
            throw new ValidationException("A recipe must contain at least one ingredient.");
        }

        var ingredientIds = new HashSet<int>();

        foreach (var item in items)
        {
            if (item is null)
            {
                throw new ValidationException("Recipe ingredients cannot contain null entries.");
            }

            RequestValidator.RequirePositive(item.IngredientId, "Ingredient id");
            RequestValidator.RequirePositive(item.Quantity, "Ingredient quantity");

            if (!ingredientIds.Add(item.IngredientId))
            {
                throw new ValidationException("A recipe cannot contain the same ingredient more than once.");
            }
        }

        return new ValidatedRecipe(normalizedName, servings, normalizedInstructions, items);
    }

    private sealed record ValidatedRecipe(
        string Name,
        int Servings,
        string? Instructions,
        IReadOnlyList<RecipeIngredientRequest> Ingredients);
}
