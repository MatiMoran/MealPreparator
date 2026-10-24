using MealPreparator.Domain.Entities;

namespace MealPreparator.Domain.Services;

public sealed record RecipePortion(Recipe Recipe, decimal Portions);
