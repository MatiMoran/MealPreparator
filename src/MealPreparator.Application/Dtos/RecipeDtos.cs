using MealPreparator.Domain.Enums;

namespace MealPreparator.Application.Dtos;

public sealed record RecipeIngredientRequest(
    int IngredientId,
    decimal? Quantity,
    UnitOfMeasure? Unit = null);

public sealed record CreateRecipeRequest(
    string Name,
    int Servings,
    string? Instructions,
    string? ImageUrl,
    List<RecipeIngredientRequest>? Ingredients,
    List<string>? Tags = null);

public sealed record UpdateRecipeRequest(
    string Name,
    int Servings,
    string? Instructions,
    string? ImageUrl,
    List<RecipeIngredientRequest>? Ingredients,
    List<string>? Tags = null);

public sealed record RecipeIngredientDto(
    int IngredientId,
    string IngredientName,
    UnitOfMeasure? Unit,
    bool IsBinary,
    string UnitDisplay,
    decimal? Quantity);

public sealed record RecipeDto(
    int Id,
    string Name,
    int Servings,
    string? Instructions,
    string? ImageUrl,
    IReadOnlyList<RecipeIngredientDto> Ingredients,
    IReadOnlyList<string> Tags);

public sealed record RecipeSummaryDto(
    int Id,
    string Name,
    int Servings,
    string? ImageUrl,
    IReadOnlyList<string> Tags);

public sealed record CreateRecipeIngredientInput(
    int? IngredientId,
    string? Name,
    UnitOfMeasure? Unit,
    decimal? Quantity,
    string? ImageUrl,
    bool CreateNew = false,
    bool IsBinary = false);

public sealed record CreateRecipeRequestV2(
    string? Name,
    int Servings,
    string? Instructions,
    IReadOnlyList<CreateRecipeIngredientInput>? Ingredients,
    string? ImageUrl,
    IReadOnlyList<string>? Tags = null);
