using MealPreparator.Domain.Enums;

namespace MealPreparator.Application.Dtos;

public sealed record RecipeIngredientRequest(int IngredientId, decimal Quantity);

public sealed record CreateRecipeRequest(
    string Name,
    int Servings,
    string? Instructions,
    List<RecipeIngredientRequest>? Ingredients);

public sealed record UpdateRecipeRequest(
    string Name,
    int Servings,
    string? Instructions,
    List<RecipeIngredientRequest>? Ingredients);

public sealed record RecipeIngredientDto(
    int IngredientId,
    string IngredientName,
    UnitOfMeasure Unit,
    decimal Quantity);

public sealed record RecipeDto(
    int Id,
    string Name,
    int Servings,
    string? Instructions,
    IReadOnlyList<RecipeIngredientDto> Ingredients);

public sealed record RecipeSummaryDto(int Id, string Name, int Servings);
