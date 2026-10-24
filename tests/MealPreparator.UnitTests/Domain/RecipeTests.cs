using FluentAssertions;
using MealPreparator.Domain.Entities;
using MealPreparator.Domain.Enums;
using MealPreparator.Domain.Exceptions;
using Xunit;

namespace MealPreparator.UnitTests.Domain;

public sealed class RecipeTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Constructor_ShouldThrow_WhenServingsIsNotPositive(int servings)
    {
        var action = () => new Recipe("Arroz con pollo", servings, null);

        action.Should().Throw<DomainValidationException>()
            .WithMessage("*servings*");
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenNameIsBlank()
    {
        var action = () => new Recipe(" ", 4, null);

        action.Should().Throw<DomainValidationException>()
            .WithMessage("*name is required*");
    }

    [Fact]
    public void ReplaceIngredients_ShouldThrow_WhenListIsEmpty()
    {
        var recipe = new Recipe("Arroz con pollo", 4, null);

        var action = () => recipe.ReplaceIngredients([]);

        action.Should().Throw<DomainValidationException>()
            .WithMessage("*at least one ingredient*");
    }

    [Fact]
    public void ReplaceIngredients_ShouldThrow_WhenIngredientIsDuplicated()
    {
        var ingredient = CreateIngredient(1, "Arroz", UnitOfMeasure.Gram);
        var recipe = new Recipe("Arroz con pollo", 4, null);

        var action = () => recipe.ReplaceIngredients(
        [
            new RecipeIngredient(ingredient, 100m),
            new RecipeIngredient(ingredient, 200m)
        ]);

        action.Should().Throw<DomainValidationException>()
            .WithMessage("*same ingredient*");
    }

    [Fact]
    public void ReplaceIngredients_ShouldReplacePreviousCollection()
    {
        var rice = CreateIngredient(1, "Arroz", UnitOfMeasure.Gram);
        var chicken = CreateIngredient(2, "Pollo", UnitOfMeasure.Gram);
        var recipe = new Recipe("Arroz con pollo", 4, null);

        recipe.ReplaceIngredients([new RecipeIngredient(rice, 400m)]);
        recipe.ReplaceIngredients([new RecipeIngredient(chicken, 600m)]);

        recipe.Ingredients.Should().ContainSingle()
            .Which.IngredientId.Should().Be(chicken.Id);
    }

    [Fact]
    public void UpdateDetails_ShouldNormalizeBlankInstructions()
    {
        var recipe = new Recipe("Arroz con pollo", 4, "   ");

        recipe.Instructions.Should().BeNull();
    }

    [Fact]
    public void AddIngredient_ShouldThrow_WhenIngredientAlreadyExists()
    {
        var rice = CreateIngredient(1, "Arroz", UnitOfMeasure.Gram);
        var recipe = new Recipe("Arroz con pollo", 4, null);
        recipe.ReplaceIngredients([new RecipeIngredient(rice, 400m)]);

        var action = () => recipe.AddIngredient(new RecipeIngredient(rice, 100m));

        action.Should().Throw<DomainValidationException>()
            .WithMessage("*same ingredient*");
    }

    [Fact]
    public void RemoveIngredient_ShouldRemoveExistingIngredient()
    {
        var rice = CreateIngredient(1, "Arroz", UnitOfMeasure.Gram);
        var recipe = new Recipe("Arroz con pollo", 4, null);
        recipe.ReplaceIngredients([new RecipeIngredient(rice, 400m)]);

        recipe.RemoveIngredient(rice.Id);

        recipe.Ingredients.Should().BeEmpty();
    }

    private static Ingredient CreateIngredient(int id, string name, UnitOfMeasure unit)
    {
        var ingredient = new Ingredient(name, unit)
        {
            Id = id
        };

        return ingredient;
    }
}
