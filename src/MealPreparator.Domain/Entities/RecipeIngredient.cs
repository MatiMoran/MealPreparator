using MealPreparator.Domain.Exceptions;

namespace MealPreparator.Domain.Entities;

public sealed class RecipeIngredient
{
    public int RecipeId { get; private set; }

    public Recipe Recipe { get; private set; } = null!;

    public int IngredientId { get; private set; }

    public Ingredient Ingredient { get; private set; } = null!;

    public decimal Quantity { get; private set; }

    private RecipeIngredient()
    {
    }

    public RecipeIngredient(int ingredientId, decimal quantity)
    {
        if (ingredientId <= 0)
        {
            throw new DomainValidationException("Ingredient id must be greater than zero.");
        }

        IngredientId = ingredientId;
        SetQuantity(quantity);
    }

    public RecipeIngredient(Ingredient ingredient, decimal quantity)
        : this(ingredient?.Id ?? throw new ArgumentNullException(nameof(ingredient)), quantity)
    {
        Ingredient = ingredient;
    }

    public void SetQuantity(decimal quantity)
    {
        if (quantity <= 0)
        {
            throw new DomainValidationException("Ingredient quantity must be greater than zero.");
        }

        if (quantity > 1_000_000)
        {
            throw new DomainValidationException("Ingredient quantity is too large.");
        }

        Quantity = quantity;
    }
}
