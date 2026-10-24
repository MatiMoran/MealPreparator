using MealPreparator.Domain.Enums;

namespace MealPreparator.Domain.Services;

public sealed record ShoppingListEntry(
    int IngredientId,
    string IngredientName,
    UnitOfMeasure Unit,
    decimal TotalQuantity);
