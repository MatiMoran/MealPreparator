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
        var action = () => new RecipeIngredient(1, quantity);

        action.Should().Throw<DomainValidationException>()
            .WithMessage("*quantity*");
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenIngredientIdIsNotPositive()
    {
        var action = () => new RecipeIngredient(0, 100m);

        action.Should().Throw<DomainValidationException>()
            .WithMessage("*id*");
    }

    [Fact]
    public void SetQuantity_ShouldUpdateQuantity()
    {
        var ingredient = new Ingredient("Arroz", UnitOfMeasure.Gram)
        {
            Id = 1
        };
        var recipeIngredient = new RecipeIngredient(ingredient, 100m);

        recipeIngredient.SetQuantity(250m);

        recipeIngredient.Quantity.Should().Be(250m);
    }
}
