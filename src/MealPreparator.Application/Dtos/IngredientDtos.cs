using MealPreparator.Domain.Enums;

namespace MealPreparator.Application.Dtos;

public sealed record IngredientDto(int Id, string Name, UnitOfMeasure Unit);

public sealed record CreateIngredientRequest(string Name, UnitOfMeasure Unit);

public sealed record UpdateIngredientRequest(string Name, UnitOfMeasure Unit);
