using MealPreparator.Application.Abstractions;
using MealPreparator.Application.Dtos;
using MealPreparator.Application.Exceptions;
using MealPreparator.Application.Mapping;
using MealPreparator.Application.Validation;
using MealPreparator.Domain.Entities;
using MealPreparator.Domain.Enums;

namespace MealPreparator.Application.Services;

public sealed class RecipeService
{
    private readonly IRecipeRepository _recipes;
    private readonly ITagRepository _tags;
    private readonly IIngredientRepository _ingredients;
    private readonly IUnitOfWork _unitOfWork;

    public RecipeService(
        IRecipeRepository recipes,
        ITagRepository tags,
        IIngredientRepository ingredients,
        IUnitOfWork unitOfWork)
    {
        _recipes = recipes;
        _tags = tags;
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

        var recipe = new Recipe(validated.Name, validated.Servings, validated.Instructions, request.ImageUrl);
        recipe.ReplaceTags(await ResolveTagsAsync(request.Tags ?? [], cancellationToken));
        recipe.ReplaceIngredients(validated.Ingredients.Select(item =>
            new RecipeIngredient(
                ingredientsById[item.IngredientId],
                GetQuantity(item.Quantity, ingredientsById[item.IngredientId]),
                GetUnit(item.Unit, ingredientsById[item.IngredientId]))));

        await _recipes.AddAsync(recipe, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return recipe.ToDto();
    }

    public async Task<RecipeDto> CreateFromInputsAsync(
        CreateRecipeRequestV2 request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var name = RequestValidator.RequireName(request.Name, nameof(request.Name));
        RequestValidator.RequirePositive(request.Servings, nameof(request.Servings));
        var instructions = RequestValidator.NormalizeOptionalText(request.Instructions, nameof(request.Instructions), 4000);

        if (request.Ingredients is null || request.Ingredients.Count == 0)
            throw new ValidationException("A recipe must contain at least one ingredient.");

        var ingredientIds = new HashSet<int>();
        var recipe = new Recipe(name, request.Servings, instructions, request.ImageUrl);
        recipe.ReplaceTags(await ResolveTagsAsync(request.Tags ?? [], cancellationToken));
        await _recipes.AddAsync(recipe, cancellationToken);

        foreach (var input in request.Ingredients)
        {
            if (input is null) throw new ValidationException("Recipe ingredients cannot contain null entries.");
            Ingredient ingredient;
            if (input.CreateNew || input.IngredientId is null || input.IngredientId == 0)
            {
                var ingName = RequestValidator.RequireName(input.Name, nameof(input.Name));
                if (await _ingredients.ExistsByNameAsync(ingName, cancellationToken: cancellationToken))
                    throw new ConflictException($"An ingredient named '{ingName}' already exists.");
                ingredient = new Ingredient(ingName, input.Unit, input.ImageUrl, input.IsBinary);
                await _ingredients.AddAsync(ingredient, cancellationToken);
                await _unitOfWork.SaveChangesAsync(cancellationToken);
            }
            else
            {
                ingredient = await _ingredients.GetByIdAsync(input.IngredientId.Value, cancellationToken)
                    ?? throw new NotFoundException($"Ingredient with id {input.IngredientId} was not found.");
            }

            if (!ingredientIds.Add(ingredient.Id))
                throw new ValidationException("A recipe cannot contain the same ingredient more than once.");

            recipe.AddIngredient(new RecipeIngredient(
                ingredient,
                GetQuantity(input.Quantity, ingredient),
                GetUnit(input.Unit, ingredient)));
        }

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
                recipe.AddIngredient(new RecipeIngredient(
                    ingredientsById[item.IngredientId],
                    GetQuantity(item.Quantity, ingredientsById[item.IngredientId]),
                    GetUnit(item.Unit, ingredientsById[item.IngredientId])));
            }
            else
            {
                existing.SetMeasurement(
                    GetQuantity(item.Quantity, ingredientsById[item.IngredientId]),
                    GetUnit(item.Unit, ingredientsById[item.IngredientId]));
            }
        }

        recipe.UpdateDetails(validated.Name, validated.Servings, validated.Instructions);
        recipe.SetImageUrl(request.ImageUrl);
        recipe.ReplaceTags(await ResolveTagsAsync(request.Tags ?? [], cancellationToken));
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

    private async Task<IReadOnlyList<Tag>> ResolveTagsAsync(
        IReadOnlyCollection<string> requestedNames,
        CancellationToken cancellationToken)
    {
        var names = requestedNames
            .Where(name => !string.IsNullOrWhiteSpace(name))
            .Select(name => name.Trim())
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (names.Any(name => name.Length > 50))
        {
            throw new ValidationException("Tag names cannot exceed 50 characters.");
        }

        var existingTags = await _tags.GetByNamesAsync(names, cancellationToken);
        var existingNames = existingTags
            .Select(tag => tag.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        var newTags = names
            .Where(name => !existingNames.Contains(name))
            .Select(name => new Tag(name))
            .ToList();

        if (newTags.Count > 0)
        {
            await _tags.AddRangeAsync(newTags, cancellationToken);
        }

        return existingTags.Concat(newTags).ToList();
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
            if (!ingredientIds.Add(item.IngredientId))
            {
                throw new ValidationException("A recipe cannot contain the same ingredient more than once.");
            }
        }

        return new ValidatedRecipe(normalizedName, servings, normalizedInstructions, items);
    }

    private static decimal? GetQuantity(decimal? quantity, Ingredient ingredient)
    {
        if (ingredient.IsBinary)
        {
            return null;
        }

        if (!quantity.HasValue || quantity <= 0)
        {
            throw new ValidationException("Ingredient quantity must be greater than zero.");
        }

        return quantity;
    }

    private static UnitOfMeasure? GetUnit(UnitOfMeasure? requestedUnit, Ingredient ingredient)
    {
        if (ingredient.IsBinary)
        {
            if (requestedUnit.HasValue)
            {
                throw new ValidationException("Binary ingredients cannot have a unit.");
            }

            return null;
        }

        var unit = requestedUnit ?? ingredient.Unit;
        if (!ingredient.SupportsUnit(unit))
        {
            throw new ValidationException("The selected unit has no conversion for this ingredient.");
        }

        return unit;
    }

    private sealed record ValidatedRecipe(
        string Name,
        int Servings,
        string? Instructions,
        IReadOnlyList<RecipeIngredientRequest> Ingredients);
}
