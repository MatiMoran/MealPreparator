using MealPreparator.Domain.Exceptions;
using MealPreparator.Domain.Enums;

namespace MealPreparator.Domain.Entities;

public sealed class RecipeIngredient
{
    public int RecipeId { get; private set; }

    public Recipe Recipe { get; private set; } = null!;

    public int IngredientId { get; private set; }

    public Ingredient Ingredient { get; private set; } = null!;

    public decimal? Quantity { get; private set; }

    public UnitOfMeasure? Unit { get; private set; }

    private RecipeIngredient()
    {
    }

    public RecipeIngredient(Ingredient ingredient, decimal? quantity, UnitOfMeasure? unit = null)
    {
        ArgumentNullException.ThrowIfNull(ingredient);
        if (ingredient.Id <= 0)
        {
            throw new DomainValidationException("Ingredient id must be greater than zero.");
        }

        IngredientId = ingredient.Id;
        Ingredient = ingredient;
        SetMeasurement(quantity, ingredient.IsBinary ? null : unit ?? ingredient.Unit);
    }

    public void SetQuantity(decimal? quantity)
    {
        SetMeasurement(quantity, Unit);
    }

    public void SetMeasurement(decimal? quantity, UnitOfMeasure? unit)
    {
        if (Ingredient?.IsBinary == true)
        {
            if (quantity.HasValue)
            {
                throw new DomainValidationException("Binary ingredients cannot have a quantity.");
            }

            Quantity = null;
            Unit = null;
            return;
        }

        if (!quantity.HasValue || quantity <= 0)
        {
            throw new DomainValidationException("Ingredient quantity must be greater than zero.");
        }

        if (quantity > 1_000_000)
        {
            throw new DomainValidationException("Ingredient quantity is too large.");
        }

        if (Ingredient is not null && (!unit.HasValue || !Ingredient.SupportsUnit(unit)))
        {
            throw new DomainValidationException("The selected unit has no conversion for this ingredient.");
        }

        Quantity = Ingredient is null || !unit.HasValue
            ? quantity
            : Ingredient.ConvertToBaseQuantity(quantity.Value, unit.Value);
        Unit = unit;
    }
}
