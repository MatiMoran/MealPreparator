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
    public async Task CreateRecipe_WithUnknownIngredient_ShouldReturnBadRequest()
    {
        var request = new CreateRecipeRequest(
            IntegrationTestHelpers.UniqueName("Receta"),
            2,
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
