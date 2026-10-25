using MealPreparator.Application.Abstractions;
using MealPreparator.Application.Dtos;
using MealPreparator.Application.Exceptions;
using MealPreparator.Application.Mapping;
using MealPreparator.Application.Validation;
using MealPreparator.Domain.Entities;

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

        var ingredient = new Ingredient(name, request.Unit, request.ImageUrl, request.IsBinary);
        if (request.UnitConversions is not null)
        {
            ingredient.SetUnitConversions(ToConversions(request.UnitConversions));
        }
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

        var previousUnit = ingredient.Unit;
        ingredient.Update(name, request.Unit, request.IsBinary);
        ingredient.SetImageUrl(request.ImageUrl);
        if (request.UnitConversions is not null)
        {
            ingredient.SetUnitConversions(ToConversions(request.UnitConversions));
        }
        else if (ingredient.IsBinary || previousUnit != ingredient.Unit)
        {
            ingredient.SetUnitConversions([]);
        }
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

    private static IReadOnlyList<IngredientUnitConversion> ToConversions(
        IReadOnlyList<IngredientUnitConversionInput> inputs)
    {
        return inputs.Select(input => new IngredientUnitConversion(
            input.Unit,
            input.Factor,
            input.Source,
            input.Note)).ToList();
    }
}
