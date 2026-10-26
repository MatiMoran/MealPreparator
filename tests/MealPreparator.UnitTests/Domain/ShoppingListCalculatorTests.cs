using FluentAssertions;
using MealPreparator.Domain.Entities;
using MealPreparator.Domain.Enums;
using MealPreparator.Domain.Exceptions;
using MealPreparator.Domain.Services;
using Xunit;

namespace MealPreparator.UnitTests.Domain;

public sealed class ShoppingListCalculatorTests
{
    [Fact]
    public void Calculate_ShouldScaleQuantitiesByPortions()
    {
        var rice = CreateIngredient(1, "Arroz", UnitOfMeasure.Gram);
        var recipe = CreateRecipe(1, "Arroz con pollo", 4, (rice, 400m));

        var result = ShoppingListCalculator.Calculate([new RecipePortion(recipe, 2m)]);

        result.Should().ContainSingle();
        result[0].TotalQuantity.Should().Be(0.2m);
    }

    [Fact]
    public void Calculate_ShouldScaleUp_WhenPortionsExceedServings()
    {
        var rice = CreateIngredient(1, "Arroz", UnitOfMeasure.Gram);
        var recipe = CreateRecipe(1, "Arroz con pollo", 4, (rice, 400m));

        var result = ShoppingListCalculator.Calculate([new RecipePortion(recipe, 6m)]);

        result[0].TotalQuantity.Should().Be(0.6m);
    }

    [Fact]
    public void Calculate_ShouldAggregateSameIngredientAcrossRecipes()
    {
        var rice = CreateIngredient(1, "Arroz", UnitOfMeasure.Gram);
        var chicken = CreateIngredient(2, "Pollo", UnitOfMeasure.Gram);
        var firstRecipe = CreateRecipe(1, "Arroz con pollo", 4, (rice, 400m), (chicken, 600m));
        var secondRecipe = CreateRecipe(2, "Arroz extra", 2, (rice, 100m));

        var result = ShoppingListCalculator.Calculate(
        [
            new RecipePortion(firstRecipe, 2m),
            new RecipePortion(secondRecipe, 2m)
        ]);

        result.Should().HaveCount(2);

        var riceEntry = result.Single(entry => entry.IngredientId == rice.Id);
        riceEntry.TotalQuantity.Should().Be(0.3m);

        var chickenEntry = result.Single(entry => entry.IngredientId == chicken.Id);
        chickenEntry.TotalQuantity.Should().Be(0.3m);
    }

    [Fact]
    public void Calculate_ShouldOrderEntriesByName()
    {
        var tomato = CreateIngredient(1, "Tomate", UnitOfMeasure.Gram);
        var rice = CreateIngredient(2, "Arroz", UnitOfMeasure.Gram);
        var recipe = CreateRecipe(1, "Mezcla", 2, (tomato, 100m), (rice, 200m));

        var result = ShoppingListCalculator.Calculate([new RecipePortion(recipe, 2m)]);

        result.Select(entry => entry.IngredientName)
            .Should()
            .ContainInOrder("Arroz", "Tomate");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Calculate_ShouldThrow_WhenPortionsAreNotPositive(decimal portions)
    {
        var rice = CreateIngredient(1, "Arroz", UnitOfMeasure.Gram);
        var recipe = CreateRecipe(1, "Arroz con pollo", 4, (rice, 400m));

        var action = () => ShoppingListCalculator.Calculate([new RecipePortion(recipe, portions)]);

        action.Should().Throw<DomainValidationException>()
            .WithMessage("*Portions*");
    }

    [Fact]
    public void Calculate_ShouldThrow_WhenThereAreNoSelections()
    {
        var action = () => ShoppingListCalculator.Calculate([]);

        action.Should().Throw<DomainValidationException>()
            .WithMessage("*At least one*");
    }

    private static Ingredient CreateIngredient(int id, string name, UnitOfMeasure unit)
    {
        return new Ingredient(name, unit)
        {
            Id = id
        };
    }

    private static Recipe CreateRecipe(
        int id,
        string name,
        int servings,
        params (Ingredient Ingredient, decimal Quantity)[] ingredients)
    {
        var recipe = new Recipe(name, servings, null)
        {
            Id = id
        };

        recipe.ReplaceIngredients(ingredients.Select(item =>
            new RecipeIngredient(item.Ingredient, item.Quantity, UnitOfMeasure.Gram)));

        return recipe;
    }
}
