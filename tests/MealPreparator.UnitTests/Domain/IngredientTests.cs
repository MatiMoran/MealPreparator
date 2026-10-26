using FluentAssertions;
using MealPreparator.Domain.Entities;
using MealPreparator.Domain.Enums;
using MealPreparator.Domain.Exceptions;
using Xunit;

namespace MealPreparator.UnitTests.Domain;

public sealed class IngredientTests
{
    [Fact]
    public void Constructor_ShouldTrimName()
    {
        var ingredient = new Ingredient("  Arroz  ", UnitOfMeasure.Gram);

        ingredient.Name.Should().Be("Arroz");
        ingredient.Unit.Should().Be(UnitOfMeasure.Kilogram);
    }

    [Fact]
    public void MeasurementConversions_ShouldConvertWithinCategoryWithoutConversionRows()
    {
        var ingredient = new Ingredient("Harina", UnitOfMeasure.Gram);

        ingredient.SupportsUnit(UnitOfMeasure.Gram).Should().BeTrue();
        ingredient.SupportsUnit(UnitOfMeasure.Kilogram).Should().BeTrue();
        ingredient.UnitConversions.Should().BeEmpty();
        ingredient.ConvertToBaseQuantity(250m, UnitOfMeasure.Gram).Should().Be(0.25m);
        ingredient.ConvertFromBaseQuantity(0.25m, UnitOfMeasure.Gram).Should().Be(250m);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_ShouldThrow_WhenNameIsBlank(string name)
    {
        var action = () => new Ingredient(name, UnitOfMeasure.Gram);

        action.Should().Throw<DomainValidationException>()
            .WithMessage("*name is required*");
    }

    [Fact]
    public void Constructor_ShouldThrow_WhenNameIsTooLong()
    {
        var name = new string('a', 201);

        var action = () => new Ingredient(name, UnitOfMeasure.Gram);

        action.Should().Throw<DomainValidationException>()
            .WithMessage("*200*");
    }

    [Theory]
    [InlineData(UnitOfMeasure.Unknown)]
    [InlineData((UnitOfMeasure)999)]
    public void Update_ShouldThrow_WhenUnitIsInvalid(UnitOfMeasure unit)
    {
        var ingredient = new Ingredient("Arroz", UnitOfMeasure.Gram);

        var action = () => ingredient.Update("Arroz", unit);

        action.Should().Throw<DomainValidationException>()
            .WithMessage("*unit is not valid*");
    }
}
