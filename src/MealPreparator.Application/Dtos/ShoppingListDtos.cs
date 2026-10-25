using MealPreparator.Domain.Enums;

namespace MealPreparator.Application.Dtos;

public sealed record ShoppingListItemRequest(int RecipeId, decimal Portions);

public sealed record ShoppingListRequest(List<ShoppingListItemRequest>? Items);

public sealed record ShoppingListItemDto(
    int IngredientId,
    string IngredientName,
    UnitOfMeasure? Unit,
    bool IsBinary,
    string UnitDisplay,
    decimal? Quantity);
