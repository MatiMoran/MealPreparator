using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MealPreparator.Application.Dtos;
using MealPreparator.Domain.Enums;
using Xunit;

namespace MealPreparator.IntegrationTests;

public sealed class ShoppingListEndpointTests : IClassFixture<MealPreparatorApiFactory>
{
    private readonly HttpClient _client;

    public ShoppingListEndpointTests(MealPreparatorApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task Calculate_ShouldScaleByPortions()
    {
        var rice = await IntegrationTestHelpers.CreateIngredientAsync(
            _client,
            IntegrationTestHelpers.UniqueName("Arroz"),
            UnitOfMeasure.Gram);

        var recipe = await IntegrationTestHelpers.CreateRecipeAsync(
            _client,
            IntegrationTestHelpers.UniqueName("Receta"),
            4,
            null,
            (rice.Id, 400m));

        var request = new ShoppingListRequest(
        [
            new ShoppingListItemRequest(recipe.Id, 2m)
        ]);

        var response = await _client.PostAsJsonAsync(
            "/api/shopping-list",
            request,
            IntegrationTestHelpers.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var shoppingList = await response.Content
            .ReadFromJsonAsync<List<ShoppingListItemDto>>(IntegrationTestHelpers.JsonOptions);

        shoppingList.Should().ContainSingle();
        shoppingList![0].IngredientId.Should().Be(rice.Id);
        shoppingList[0].Quantity.Should().Be(0.2m);
    }

    [Fact]
    public async Task Calculate_ShouldAggregateSameIngredientAcrossRecipes()
    {
        var rice = await IntegrationTestHelpers.CreateIngredientAsync(
            _client,
            IntegrationTestHelpers.UniqueName("Arroz"),
            UnitOfMeasure.Gram);

        var firstRecipe = await IntegrationTestHelpers.CreateRecipeAsync(
            _client,
            IntegrationTestHelpers.UniqueName("Receta A"),
            2,
            null,
            (rice.Id, 100m));

        var secondRecipe = await IntegrationTestHelpers.CreateRecipeAsync(
            _client,
            IntegrationTestHelpers.UniqueName("Receta B"),
            2,
            null,
            (rice.Id, 50m));

        var request = new ShoppingListRequest(
        [
            new ShoppingListItemRequest(firstRecipe.Id, 2m),
            new ShoppingListItemRequest(secondRecipe.Id, 2m)
        ]);

        var response = await _client.PostAsJsonAsync(
            "/api/shopping-list",
            request,
            IntegrationTestHelpers.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var shoppingList = await response.Content
            .ReadFromJsonAsync<List<ShoppingListItemDto>>(IntegrationTestHelpers.JsonOptions);

        shoppingList.Should().ContainSingle();
        shoppingList![0].Quantity.Should().Be(0.15m);
    }

    [Fact]
    public async Task Calculate_WithUnknownRecipe_ShouldReturnBadRequest()
    {
        var request = new ShoppingListRequest(
        [
            new ShoppingListItemRequest(999999, 2m)
        ]);

        var response = await _client.PostAsJsonAsync(
            "/api/shopping-list",
            request,
            IntegrationTestHelpers.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task Calculate_WithoutSelections_ShouldReturnBadRequest()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/shopping-list",
            new ShoppingListRequest([]),
            IntegrationTestHelpers.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
