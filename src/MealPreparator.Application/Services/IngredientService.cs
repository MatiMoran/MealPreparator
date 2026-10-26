using MealPreparator.Application.Abstractions;
using MealPreparator.Application.Dtos;
using MealPreparator.Application.Exceptions;
using MealPreparator.Application.Mapping;
using MealPreparator.Application.Validation;
using MealPreparator.Domain.Entities;
using MealPreparator.Domain.Enums;

namespace MealPreparator.Application.Services;

public sealed class IngredientService
{
    private readonly IIngredientRepository _ingredients;
    private readonly IUnitOfWork _unitOfWork;

    public IngredientService(IIngredientRepository ingredients, IUnitOfWork unitOfWork)
    {
        _ingredients = ingredients;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<IngredientDto>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        var ingredients = await _ingredients.GetAllAsync(cancellationToken);
        return ingredients.Select(ingredient => ingredient.ToDto()).ToList();
    }

    public async Task<IngredientDto> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        var ingredient = await GetEntityAsync(id, cancellationToken);
        return ingredient.ToDto();
    }

    public async Task<IngredientDto> CreateAsync(
        CreateIngredientRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var name = RequestValidator.RequireName(request.Name, nameof(request.Name));

        if (await _ingredients.ExistsByNameAsync(name, cancellationToken: cancellationToken))
        {
            throw new ConflictException($"An ingredient named '{name}' already exists.");
        }

        var canonicalUnit = NormalizeUnit(request.Unit, request.IsBinary);
        var ingredient = new Ingredient(name, canonicalUnit, request.ImageUrl, request.IsBinary);
        ingredient.SetUnitConversions(BuildConversions(canonicalUnit, request.IsBinary, request.UnitConversions));
        await _ingredients.AddAsync(ingredient, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ingredient.ToDto();
    }

    public async Task<IngredientDto> UpdateAsync(
        int id,
        UpdateIngredientRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var ingredient = await GetEntityAsync(id, cancellationToken);
        var name = RequestValidator.RequireName(request.Name, nameof(request.Name));

        if (await _ingredients.ExistsByNameAsync(name, id, cancellationToken))
        {
            throw new ConflictException($"An ingredient named '{name}' already exists.");
        }

        IngredientCategory? previousCategory = ingredient.IsBinary ? null : UnitOfMeasureRules.GetCategory(ingredient.Unit!.Value);
        var canonicalUnit = NormalizeUnit(request.Unit, request.IsBinary);
        IngredientCategory? nextCategory = request.IsBinary ? null : UnitOfMeasureRules.GetCategory(canonicalUnit!.Value);
        if (previousCategory != nextCategory && await _ingredients.IsUsedByRecipesAsync(id, cancellationToken))
        {
            throw new ConflictException("The ingredient category cannot change while recipes use it.");
        }

        ingredient.Update(name, canonicalUnit, request.IsBinary);
        ingredient.SetImageUrl(request.ImageUrl);
        ingredient.SetUnitConversions(BuildConversions(
            ingredient.Unit,
            ingredient.IsBinary,
            request.UnitConversions,
            request.UnitConversions is null && previousCategory == nextCategory
                ? ingredient.UnitConversions
                : null));
        await _unitOfWork.SaveChangesAsync(cancellationToken);

        return ingredient.ToDto();
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var ingredient = await GetEntityAsync(id, cancellationToken);

        if (await _ingredients.IsUsedByRecipesAsync(id, cancellationToken))
        {
            throw new ConflictException("The ingredient is used by one or more recipes and cannot be deleted.");
        }

        _ingredients.Remove(ingredient);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Ingredient> GetEntityAsync(int id, CancellationToken cancellationToken)
    {
        return await _ingredients.GetByIdAsync(id, cancellationToken)
            ?? throw new NotFoundException($"Ingredient with id {id} was not found.");
    }

    private static UnitOfMeasure? NormalizeUnit(UnitOfMeasure? unit, bool isBinary)
    {
        if (isBinary)
        {
            return null;
        }

        if (!unit.HasValue || unit == UnitOfMeasure.Unknown || !Enum.IsDefined(unit.Value))
        {
            throw new ValidationException("A valid measurement category is required.");
        }

        return UnitOfMeasureRules.ToCanonicalUnit(unit.Value);
    }

    private IReadOnlyList<IngredientUnitConversion> BuildConversions(
        UnitOfMeasure? baseUnit,
        bool isBinary,
        IReadOnlyList<IngredientUnitConversionInput>? requested,
        IEnumerable<IngredientUnitConversion>? existing = null)
    {
        if (isBinary || !baseUnit.HasValue)
        {
            return [];
        }

        var generated = new Dictionary<UnitOfMeasure, IngredientUnitConversion>();
        foreach (var conversion in existing ?? [])
        {
            generated[conversion.Unit] = new IngredientUnitConversion(
                conversion.Unit,
                conversion.Factor,
                conversion.Source,
                conversion.Note);
        }
        foreach (var conversion in requested ?? [])
        {
            generated[conversion.Unit] = new IngredientUnitConversion(
                conversion.Unit,
                conversion.Factor,
                conversion.Source,
                conversion.Note);
        }

        return generated.Values.ToList();
    }

}
