using FluentAssertions;
using MealPreparator.Domain.Entities;
using MealPreparator.Domain.Enums;
using MealPreparator.Domain.Exceptions;
using Xunit;

namespace MealPreparator.UnitTests.Domain;

public sealed class RecipeIngredientTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_ShouldThrow_WhenQuantityIsNotPositive(decimal quantity)
    {
        var ingredient = new Ingredient("Arroz", UnitOfMeasure.Gram) { Id = 1 };
        var action = () => new RecipeIngredient(ingredient, quantity, UnitOfMeasure.Gram);

        action.Should().Throw<DomainValidationException>()
            .WithMessage("*quantity*");
    }

    [Fact]
    public void SetQuantity_ShouldUpdateQuantity()
    {
        var ingredient = new Ingredient("Arroz", UnitOfMeasure.Gram)
        {
            Id = 1
        };
        var recipeIngredient = new RecipeIngredient(ingredient, 100m, UnitOfMeasure.Gram);

        recipeIngredient.SetQuantity(250m);

        recipeIngredient.Quantity.Should().Be(0.25m);
    }
}
