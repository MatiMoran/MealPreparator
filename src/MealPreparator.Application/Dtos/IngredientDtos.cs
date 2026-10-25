using MealPreparator.Domain.Enums;

namespace MealPreparator.Application.Dtos;

public sealed record IngredientUnitConversionDto(
    UnitOfMeasure Unit,
    decimal Factor,
    string? Source,
    string? Note);

public sealed record IngredientUnitConversionInput(
    UnitOfMeasure Unit,
    decimal Factor,
    string? Source = null,
    string? Note = null);

public sealed record IngredientDto(
    int Id,
    string Name,
    UnitOfMeasure? Unit,
    bool IsBinary,
    string? ImageUrl,
    IReadOnlyList<IngredientUnitConversionDto> UnitConversions);

public sealed record CreateIngredientRequest(
    string Name,
    UnitOfMeasure? Unit,
    string? ImageUrl,
    bool IsBinary = false,
    List<IngredientUnitConversionInput>? UnitConversions = null);

public sealed record UpdateIngredientRequest(
    string Name,
    UnitOfMeasure? Unit,
    string? ImageUrl,
    bool IsBinary = false,
    List<IngredientUnitConversionInput>? UnitConversions = null);
