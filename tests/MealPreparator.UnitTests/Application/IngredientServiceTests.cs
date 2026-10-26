using FluentAssertions;
using MealPreparator.Application.Abstractions;
using MealPreparator.Application.Dtos;
using MealPreparator.Application.Services;
using MealPreparator.Domain.Entities;
using MealPreparator.Domain.Enums;
using Xunit;

namespace MealPreparator.UnitTests.Application;

public sealed class IngredientServiceTests
{
    [Theory]
    [InlineData(UnitOfMeasure.Gram, UnitOfMeasure.Kilogram)]
    [InlineData(UnitOfMeasure.Kilogram, UnitOfMeasure.Kilogram)]
    [InlineData(UnitOfMeasure.Milliliter, UnitOfMeasure.Liter)]
    [InlineData(UnitOfMeasure.Tablespoon, UnitOfMeasure.Liter)]
    [InlineData(UnitOfMeasure.Teaspoon, UnitOfMeasure.Liter)]
    [InlineData(UnitOfMeasure.Cup, UnitOfMeasure.Liter)]
    public async Task CreateAsync_ShouldStoreTheCanonicalUnit(UnitOfMeasure requested, UnitOfMeasure canonical)
    {
        var fixture = new Fixture();

        var created = await fixture.Service.CreateAsync(new CreateIngredientRequest("Ingrediente", requested, null));

        created.Unit.Should().Be(canonical);
        created.UnitConversions.Should().BeEmpty();
        fixture.UnitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_ShouldAllowCrossCategoryConversionInCanonicalUnits()
    {
        var fixture = new Fixture();
        var request = new CreateIngredientRequest(
            "Harina",
            UnitOfMeasure.Gram,
            null,
            UnitConversions: [new IngredientUnitConversionInput(UnitOfMeasure.Cup, 0.125m)]);

        var created = await fixture.Service.CreateAsync(request);

        created.Unit.Should().Be(UnitOfMeasure.Kilogram);
        created.UnitConversions.Should().ContainSingle(conversion => conversion.Unit == UnitOfMeasure.Cup)
            .Which.Factor.Should().Be(0.125m);
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectRedundantSameCategoryConversion()
    {
        var fixture = new Fixture();
        var request = new CreateIngredientRequest(
            "Harina",
            UnitOfMeasure.Gram,
            null,
            UnitConversions: [new IngredientUnitConversionInput(UnitOfMeasure.Kilogram, 1000m)]);

        var act = () => fixture.Service.CreateAsync(request);

        await act.Should().ThrowAsync<MealPreparator.Domain.Exceptions.DomainValidationException>()
            .WithMessage("*base unit*");
    }

    [Fact]
    public async Task CreateAsync_ShouldSupportPieceAndBinaryIngredients()
    {
        var fixture = new Fixture();

        var piece = await fixture.Service.CreateAsync(new CreateIngredientRequest("Huevo", UnitOfMeasure.Piece, null));
        var binary = await fixture.Service.CreateAsync(new CreateIngredientRequest("Sal", null, null, IsBinary: true));

        piece.Unit.Should().Be(UnitOfMeasure.Piece);
        piece.UnitConversions.Should().BeEmpty();
        binary.IsBinary.Should().BeTrue();
        binary.Unit.Should().BeNull();
    }

    [Fact]
    public async Task UpdateAsync_ShouldRetainCrossCategoryConversionsWhenCategoryIsUnchanged()
    {
        var fixture = new Fixture();
        var ingredient = fixture.Repository.AddSeed("Harina", UnitOfMeasure.Gram,
            new IngredientUnitConversion(UnitOfMeasure.Cup, 0.13m, "source"));

        var updated = await fixture.Service.UpdateAsync(
            ingredient.Id,
            new UpdateIngredientRequest("Harina integral", UnitOfMeasure.Kilogram, null));

        updated.Unit.Should().Be(UnitOfMeasure.Kilogram);
        updated.UnitConversions.Should().ContainSingle(conversion => conversion.Unit == UnitOfMeasure.Cup)
            .Which.Factor.Should().Be(0.13m);
    }

    private sealed class Fixture
    {
        public FakeIngredientRepository Repository { get; } = new();
        public FakeUnitOfWork UnitOfWork { get; } = new();
        public IngredientService Service { get; }

        public Fixture() => Service = new IngredientService(Repository, UnitOfWork);
    }

    private sealed class FakeIngredientRepository : IIngredientRepository
    {
        private int _nextId = 1;
        public List<Ingredient> Ingredients { get; } = [];

        public Ingredient AddSeed(string name, UnitOfMeasure unit, params IngredientUnitConversion[] conversions)
        {
            var ingredient = new Ingredient(name, unit, unitConversions: conversions) { Id = _nextId++ };
            Ingredients.Add(ingredient);
            return ingredient;
        }

        public Task<IReadOnlyList<Ingredient>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Ingredient>>(Ingredients);
        public Task<Ingredient?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Ingredients.SingleOrDefault(ingredient => ingredient.Id == id));
        public Task<IReadOnlyList<Ingredient>> GetByIdsAsync(IReadOnlyCollection<int> ids, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Ingredient>>(Ingredients.Where(ingredient => ids.Contains(ingredient.Id)).ToList());
        public Task<bool> ExistsByNameAsync(string name, int? excludeId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(Ingredients.Any(ingredient => ingredient.Id != excludeId && ingredient.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));
        public Task<bool> IsUsedByRecipesAsync(int id, CancellationToken cancellationToken = default) => Task.FromResult(false);
        public Task AddAsync(Ingredient ingredient, CancellationToken cancellationToken = default)
        {
            ingredient.Id = _nextId++;
            Ingredients.Add(ingredient);
            return Task.CompletedTask;
        }
        public void Remove(Ingredient ingredient) => Ingredients.Remove(ingredient);
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }
}
