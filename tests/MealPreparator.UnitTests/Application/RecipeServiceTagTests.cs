using FluentAssertions;
using MealPreparator.Application.Abstractions;
using MealPreparator.Application.Dtos;
using MealPreparator.Application.Services;
using MealPreparator.Domain.Entities;
using MealPreparator.Domain.Enums;
using Xunit;

namespace MealPreparator.UnitTests.Application;

public sealed class RecipeServiceTagTests
{
    [Fact]
    public async Task CreateAsync_ShouldAttachRequestedTagsToRecipe()
    {
        var fixture = new RecipeServiceFixture();
        var request = new CreateRecipeRequest(
            "Receta nueva",
            2,
            null,
            null,
            [new RecipeIngredientRequest(fixture.Rice.Id, 100m)],
            [" Pollo ", "Postre"]);

        var created = await fixture.Service.CreateAsync(request);

        created.Tags.Should().BeEquivalentTo("Pollo", "Postre");
        fixture.Tags.StoredTags.Select(tag => tag.Name)
            .Should().BeEquivalentTo("Pollo", "Postre");
        fixture.Recipes.Recipes.Should().ContainSingle()
            .Which.Tags.Select(recipeTag => recipeTag.Tag.Name)
            .Should().BeEquivalentTo("Pollo", "Postre");
    }

    [Fact]
    public async Task UpdateAsync_ShouldReplaceOrClearRecipeTags()
    {
        var fixture = new RecipeServiceFixture();
        var recipe = new Recipe("Receta existente", 2, null);
        recipe.ReplaceIngredients([new RecipeIngredient(fixture.Rice, 100m)]);
        recipe.ReplaceTags([new Tag("Desayuno")]);
        recipe.Id = 1;
        fixture.Recipes.Recipes.Add(recipe);

        var updated = await fixture.Service.UpdateAsync(1, new UpdateRecipeRequest(
            recipe.Name,
            recipe.Servings,
            null,
            null,
            [new RecipeIngredientRequest(fixture.Rice.Id, 100m)],
            ["Meriendas y Desayunos", "Casero"]));

        updated.Tags.Should().BeEquivalentTo("Meriendas y Desayunos", "Casero");
        recipe.Tags.Select(recipeTag => recipeTag.Tag.Name)
            .Should().BeEquivalentTo("Meriendas y Desayunos", "Casero");

        var cleared = await fixture.Service.UpdateAsync(1, new UpdateRecipeRequest(
            recipe.Name,
            recipe.Servings,
            null,
            null,
            [new RecipeIngredientRequest(fixture.Rice.Id, 100m)],
            []));

        cleared.Tags.Should().BeEmpty();
        recipe.Tags.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateAsync_ShouldPreserveBinaryPresenceAndAcceptSupportedAlternateUnits()
    {
        var fixture = new RecipeServiceFixture();
        var created = await fixture.Service.CreateAsync(new CreateRecipeRequest(
            "Arroz con sal",
            2,
            null,
            null,
            [
                new RecipeIngredientRequest(fixture.Rice.Id, 0.5m, UnitOfMeasure.Kilogram),
                new RecipeIngredientRequest(fixture.Salt.Id, null)
            ]));

        created.Ingredients.Should().ContainSingle(item => item.IngredientId == fixture.Rice.Id)
            .Which.Unit.Should().Be(UnitOfMeasure.Kilogram);
        var salt = created.Ingredients.Should().ContainSingle(item => item.IngredientId == fixture.Salt.Id).Which;
        salt.IsBinary.Should().BeTrue();
        salt.Quantity.Should().BeNull();
        salt.Unit.Should().BeNull();
    }

    private sealed class RecipeServiceFixture
    {
        public Ingredient Rice { get; } = new("Arroz", UnitOfMeasure.Kilogram) { Id = 1 };

        public Ingredient Salt { get; } = new("Sal", null, isBinary: true) { Id = 2 };

        public FakeRecipeRepository Recipes { get; } = new();

        public FakeTagRepository Tags { get; } = new();

        public RecipeService Service { get; }

        public RecipeServiceFixture()
        {
            Service = new RecipeService(
                Recipes,
                Tags,
                new FakeIngredientRepository([Rice, Salt]),
                new FakeUnitOfWork());
        }
    }

    private sealed class FakeRecipeRepository : IRecipeRepository
    {
        public List<Recipe> Recipes { get; } = [];

        public Task<IReadOnlyList<Recipe>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Recipe>>(Recipes);

        public Task<Recipe?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Recipes.SingleOrDefault(recipe => recipe.Id == id));

        public Task<IReadOnlyList<Recipe>> GetByIdsAsync(
            IReadOnlyCollection<int> ids,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Recipe>>(Recipes.Where(recipe => ids.Contains(recipe.Id)).ToList());

        public Task AddAsync(Recipe recipe, CancellationToken cancellationToken = default)
        {
            recipe.Id = Recipes.Count + 1;
            Recipes.Add(recipe);
            return Task.CompletedTask;
        }

        public void Remove(Recipe recipe) => Recipes.Remove(recipe);
    }

    private sealed class FakeTagRepository : ITagRepository
    {
        public List<Tag> StoredTags { get; } = [];

        public Task<IReadOnlyList<Tag>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Tag>>(StoredTags);

        public Task<Tag?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(StoredTags.SingleOrDefault(tag => tag.Id == id));

        public Task<bool> ExistsByNameAsync(string name, int? excludedId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(StoredTags.Any(tag => tag.Id != excludedId && tag.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));

        public Task<IReadOnlyList<Tag>> GetByNamesAsync(
            IReadOnlyCollection<string> names,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Tag>>(StoredTags
                .Where(tag => names.Contains(tag.Name, StringComparer.OrdinalIgnoreCase))
                .ToList());

        public Task AddRangeAsync(IEnumerable<Tag> tags, CancellationToken cancellationToken = default)
        {
            foreach (var tag in tags)
            {
                StoredTags.Add(tag);
            }
            return Task.CompletedTask;
        }

        public Task AddAsync(Tag tag, CancellationToken cancellationToken = default) => AddRangeAsync([tag], cancellationToken);

        public void Remove(Tag tag) => StoredTags.Remove(tag);
    }

    private sealed class FakeIngredientRepository : IIngredientRepository
    {
        private readonly IReadOnlyList<Ingredient> _ingredients;

        public FakeIngredientRepository(IReadOnlyList<Ingredient> ingredients)
        {
            _ingredients = ingredients;
        }

        public Task<IReadOnlyList<Ingredient>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult(_ingredients);

        public Task<Ingredient?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(_ingredients.SingleOrDefault(ingredient => ingredient.Id == id));

        public Task<IReadOnlyList<Ingredient>> GetByIdsAsync(
            IReadOnlyCollection<int> ids,
            CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Ingredient>>(
                _ingredients.Where(ingredient => ids.Contains(ingredient.Id)).ToList());

        public Task<bool> ExistsByNameAsync(
            string name,
            int? excludeId = null,
            CancellationToken cancellationToken = default) => Task.FromResult(false);

        public Task<bool> IsUsedByRecipesAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(false);

        public Task AddAsync(Ingredient ingredient, CancellationToken cancellationToken = default) =>
            Task.CompletedTask;

        public void Remove(Ingredient ingredient)
        {
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default) => Task.FromResult(1);
    }
}
