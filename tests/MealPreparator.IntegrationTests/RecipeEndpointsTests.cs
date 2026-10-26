using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MealPreparator.Application.Dtos;
using MealPreparator.Domain.Enums;
using Xunit;

namespace MealPreparator.IntegrationTests;

public sealed class RecipeEndpointsTests : IClassFixture<MealPreparatorApiFactory>
{
    private readonly HttpClient _client;

    public RecipeEndpointsTests(MealPreparatorApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task CreateRecipe_ShouldReturnRecipeWithIngredients()
    {
        var rice = await IntegrationTestHelpers.CreateIngredientAsync(
            _client,
            IntegrationTestHelpers.UniqueName("Arroz"),
            UnitOfMeasure.Gram);

        var recipe = await IntegrationTestHelpers.CreateRecipeAsync(
            _client,
            IntegrationTestHelpers.UniqueName("Receta"),
            4,
            "Paso 1",
            (rice.Id, 400m));

        recipe.Servings.Should().Be(4);
        recipe.Instructions.Should().Be("Paso 1");
        recipe.Ingredients.Should().ContainSingle()
            .Which.IngredientId.Should().Be(rice.Id);

        var response = await _client.GetAsync($"/api/recipes/{recipe.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var fetched = await response.Content
            .ReadFromJsonAsync<RecipeDto>(IntegrationTestHelpers.JsonOptions);

        fetched.Should().NotBeNull();
        fetched!.Ingredients.Should().ContainSingle()
            .Which.Quantity.Should().Be(400m);
    }

    [Fact]
    public async Task CreateAndUpdateRecipe_ShouldPersistTagsInSummaryAndDetail()
    {
        var rice = await IntegrationTestHelpers.CreateIngredientAsync(
            _client,
            IntegrationTestHelpers.UniqueName("Arroz"),
            UnitOfMeasure.Gram);
        var createRequest = new CreateRecipeRequest(
            IntegrationTestHelpers.UniqueName("Receta"),
            2,
            null,
            null,
            [new RecipeIngredientRequest(rice.Id, 100m)],
            ["Desayuno"]);
        var createResponse = await _client.PostAsJsonAsync(
            "/api/recipes",
            createRequest,
            IntegrationTestHelpers.JsonOptions);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var recipe = (await createResponse.Content.ReadFromJsonAsync<RecipeDto>(IntegrationTestHelpers.JsonOptions))!;
        recipe.Tags.Should().ContainSingle().Which.Should().Be("Desayuno");

        var updateRequest = new UpdateRecipeRequest(
            recipe.Name,
            2,
            null,
            null,
            [new RecipeIngredientRequest(rice.Id, 100m)],
            ["Meriendas y Desayunos", "Casero"]);
        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/recipes/{recipe.Id}",
            updateRequest,
            IntegrationTestHelpers.JsonOptions);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updated = await updateResponse.Content.ReadFromJsonAsync<RecipeDto>(IntegrationTestHelpers.JsonOptions);
        updated!.Tags.Should().BeEquivalentTo("Meriendas y Desayunos", "Casero");

        var summaries = await _client.GetFromJsonAsync<List<RecipeSummaryDto>>(
            "/api/recipes",
            IntegrationTestHelpers.JsonOptions);
        summaries!.Single(item => item.Id == recipe.Id).Tags.Should()
            .BeEquivalentTo("Meriendas y Desayunos", "Casero");

        var reuseRequest = updateRequest with { Tags = ["casero"] };
        var reuseResponse = await _client.PutAsJsonAsync(
            $"/api/recipes/{recipe.Id}",
            reuseRequest,
            IntegrationTestHelpers.JsonOptions);
        reuseResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var reused = await reuseResponse.Content.ReadFromJsonAsync<RecipeDto>(IntegrationTestHelpers.JsonOptions);
        reused!.Tags.Should().ContainSingle().Which.Should().Be("Casero");

        var clearTagsRequest = updateRequest with { Tags = [] };
        var clearResponse = await _client.PutAsJsonAsync(
            $"/api/recipes/{recipe.Id}",
            clearTagsRequest,
            IntegrationTestHelpers.JsonOptions);
        clearResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var cleared = await clearResponse.Content.ReadFromJsonAsync<RecipeDto>(IntegrationTestHelpers.JsonOptions);
        cleared!.Tags.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateRecipe_ShouldAcceptBinaryAndAlternateUnitIngredientsAndCalculateShoppingList()
    {
        var flour = await IntegrationTestHelpers.CreateIngredientAsync(
            _client,
            IntegrationTestHelpers.UniqueName("Harina"),
            UnitOfMeasure.Gram);
        var saltName = IntegrationTestHelpers.UniqueName("Sal");
        var saltResponse = await _client.PostAsJsonAsync(
            "/api/ingredients",
            new CreateIngredientRequest(saltName, null, null, IsBinary: true),
            IntegrationTestHelpers.JsonOptions);
        saltResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var salt = (await saltResponse.Content.ReadFromJsonAsync<IngredientDto>(IntegrationTestHelpers.JsonOptions))!;

        var createResponse = await _client.PostAsJsonAsync(
            "/api/recipes",
            new CreateRecipeRequest(
                IntegrationTestHelpers.UniqueName("Receta"), 2, null, null,
                [
                    new RecipeIngredientRequest(flour.Id, 0.5m, UnitOfMeasure.Kilogram),
                    new RecipeIngredientRequest(salt.Id, null)
                ]),
            IntegrationTestHelpers.JsonOptions);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var recipe = (await createResponse.Content.ReadFromJsonAsync<RecipeDto>(IntegrationTestHelpers.JsonOptions))!;
        recipe.Ingredients.Should().ContainSingle(item => item.IngredientId == flour.Id)
            .Which.Unit.Should().Be(UnitOfMeasure.Kilogram);
        recipe.Ingredients.Should().ContainSingle(item => item.IngredientId == salt.Id)
            .Which.IsBinary.Should().BeTrue();
        recipe.Ingredients.Single(item => item.IngredientId == salt.Id).Quantity.Should().BeNull();

        var shoppingResponse = await _client.PostAsJsonAsync(
            "/api/shopping-list",
            new ShoppingListRequest([new ShoppingListItemRequest(recipe.Id, 4m)]),
            IntegrationTestHelpers.JsonOptions);
        shoppingResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var items = await shoppingResponse.Content.ReadFromJsonAsync<List<ShoppingListItemDto>>(IntegrationTestHelpers.JsonOptions)
            ?? throw new InvalidOperationException("Shopping list response was empty.");
        items.Should().ContainSingle(item => item.IngredientId == flour.Id)
            .Which.Quantity.Should().Be(1m);
        items.Should().ContainSingle(item => item.IngredientId == salt.Id)
            .Which.IsBinary.Should().BeTrue();
        items.Single(item => item.IngredientId == salt.Id).Quantity.Should().BeNull();
    }

    [Fact]
    public async Task CreateRecipe_WithUnknownIngredient_ShouldReturnBadRequest()
    {
        var request = new CreateRecipeRequest(
            IntegrationTestHelpers.UniqueName("Receta"),
            2,
            null,
            null,
            [new RecipeIngredientRequest(999999, 100m)]);

        var response = await _client.PostAsJsonAsync(
            "/api/recipes",
            request,
            IntegrationTestHelpers.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task CreateRecipe_WithoutIngredients_ShouldReturnBadRequest()
    {
        var request = new CreateRecipeRequest(
            IntegrationTestHelpers.UniqueName("Receta"),
            2,
            null,
            null,
            []);

        var response = await _client.PostAsJsonAsync(
            "/api/recipes",
            request,
            IntegrationTestHelpers.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task UpdateRecipe_ShouldReplaceIngredientsAndQuantities()
    {
        var rice = await IntegrationTestHelpers.CreateIngredientAsync(
            _client,
            IntegrationTestHelpers.UniqueName("Arroz"),
            UnitOfMeasure.Gram);
        var chicken = await IntegrationTestHelpers.CreateIngredientAsync(
            _client,
            IntegrationTestHelpers.UniqueName("Pollo"),
            UnitOfMeasure.Gram);

        var recipe = await IntegrationTestHelpers.CreateRecipeAsync(
            _client,
            IntegrationTestHelpers.UniqueName("Receta"),
            4,
            null,
            (rice.Id, 400m),
            (chicken.Id, 600m));

        var updateRequest = new UpdateRecipeRequest(
            recipe.Name,
            3,
            "Actualizada",
            null,
            [new RecipeIngredientRequest(rice.Id, 250m)]);

        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/recipes/{recipe.Id}",
            updateRequest,
            IntegrationTestHelpers.JsonOptions);

        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        var updated = await updateResponse.Content
            .ReadFromJsonAsync<RecipeDto>(IntegrationTestHelpers.JsonOptions);

        updated.Should().NotBeNull();
        updated!.Servings.Should().Be(3);
        updated.Instructions.Should().Be("Actualizada");
        updated.Ingredients.Should().ContainSingle()
            .Which.Quantity.Should().Be(250m);
    }

    [Fact]
    public async Task DeleteRecipe_ShouldRemoveRecipe()
    {
        var rice = await IntegrationTestHelpers.CreateIngredientAsync(
            _client,
            IntegrationTestHelpers.UniqueName("Arroz"),
            UnitOfMeasure.Gram);

        var recipe = await IntegrationTestHelpers.CreateRecipeAsync(
            _client,
            IntegrationTestHelpers.UniqueName("Receta"),
            2,
            null,
            (rice.Id, 100m));

        var deleteResponse = await _client.DeleteAsync($"/api/recipes/{recipe.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var getResponse = await _client.GetAsync($"/api/recipes/{recipe.Id}");
        getResponse.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }

    [Fact]
    public async Task DeleteIngredient_WhenUsedByRecipe_ShouldReturnConflict()
    {
        var rice = await IntegrationTestHelpers.CreateIngredientAsync(
            _client,
            IntegrationTestHelpers.UniqueName("Arroz"),
            UnitOfMeasure.Gram);

        await IntegrationTestHelpers.CreateRecipeAsync(
            _client,
            IntegrationTestHelpers.UniqueName("Receta"),
            2,
            null,
            (rice.Id, 100m));

        var response = await _client.DeleteAsync($"/api/ingredients/{rice.Id}");

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }
}
