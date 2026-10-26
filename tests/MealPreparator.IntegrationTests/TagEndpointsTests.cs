using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MealPreparator.Application.Dtos;
using MealPreparator.Domain.Enums;
using Xunit;

namespace MealPreparator.IntegrationTests;

public sealed class TagEndpointsTests : IClassFixture<MealPreparatorApiFactory>
{
    private readonly HttpClient _client;

    public TagEndpointsTests(MealPreparatorApiFactory factory) => _client = factory.CreateClient();

    [Fact]
    public async Task TagEndpoints_ShouldCreateRenameAndDeleteTagFromAttachedRecipes()
    {
        var originalName = IntegrationTestHelpers.UniqueName("Tag");
        var createResponse = await _client.PostAsJsonAsync("/api/tags", new TagNameRequest(originalName), IntegrationTestHelpers.JsonOptions);
        createResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var created = (await createResponse.Content.ReadFromJsonAsync<TagDto>(IntegrationTestHelpers.JsonOptions))!;

        var listed = await _client.GetFromJsonAsync<List<TagDto>>("/api/tags", IntegrationTestHelpers.JsonOptions);
        listed.Should().Contain(tag => tag.Id == created.Id && tag.Name == originalName);

        var rice = await IntegrationTestHelpers.CreateIngredientAsync(
            _client,
            IntegrationTestHelpers.UniqueName("Arroz"),
            UnitOfMeasure.Gram);
        var recipeResponse = await _client.PostAsJsonAsync(
            "/api/recipes",
            new CreateRecipeRequest(
                IntegrationTestHelpers.UniqueName("Receta"), 2, null, null,
                [new RecipeIngredientRequest(rice.Id, 100m)], [originalName]),
            IntegrationTestHelpers.JsonOptions);
        recipeResponse.StatusCode.Should().Be(HttpStatusCode.Created);
        var recipe = (await recipeResponse.Content.ReadFromJsonAsync<RecipeDto>(IntegrationTestHelpers.JsonOptions))!;
        recipe.Tags.Should().ContainSingle().Which.Should().Be(originalName);

        var renamedName = IntegrationTestHelpers.UniqueName("Renombrado");
        var updateResponse = await _client.PutAsJsonAsync(
            $"/api/tags/{created.Id}", new TagNameRequest(renamedName), IntegrationTestHelpers.JsonOptions);
        updateResponse.StatusCode.Should().Be(HttpStatusCode.OK);
        var updatedRecipe = await _client.GetFromJsonAsync<RecipeDto>($"/api/recipes/{recipe.Id}", IntegrationTestHelpers.JsonOptions);
        updatedRecipe!.Tags.Should().ContainSingle().Which.Should().Be(renamedName);

        var duplicateResponse = await _client.PostAsJsonAsync(
            "/api/tags", new TagNameRequest(renamedName.ToUpperInvariant()), IntegrationTestHelpers.JsonOptions);
        duplicateResponse.StatusCode.Should().Be(HttpStatusCode.Conflict);

        var deleteResponse = await _client.DeleteAsync($"/api/tags/{created.Id}");
        deleteResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);
        var recipeAfterDelete = await _client.GetFromJsonAsync<RecipeDto>($"/api/recipes/{recipe.Id}", IntegrationTestHelpers.JsonOptions);
        recipeAfterDelete!.Tags.Should().BeEmpty();

        (await _client.DeleteAsync("/api/tags/999999")).StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
