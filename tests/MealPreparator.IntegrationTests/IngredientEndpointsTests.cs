using System.Net;
using System.Net.Http.Json;
using FluentAssertions;
using MealPreparator.Application.Dtos;
using MealPreparator.Domain.Enums;
using Xunit;

namespace MealPreparator.IntegrationTests;

public sealed class IngredientEndpointsTests : IClassFixture<MealPreparatorApiFactory>
{
    private readonly HttpClient _client;

    public IngredientEndpointsTests(MealPreparatorApiFactory factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetIngredients_ShouldReturnSeededIngredients()
    {
        var response = await _client.GetAsync("/api/ingredients");

        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var ingredients = await response.Content
            .ReadFromJsonAsync<List<IngredientDto>>(IntegrationTestHelpers.JsonOptions);

        ingredients.Should().NotBeNull();
        ingredients!.Should().NotBeEmpty();
    }

    [Fact]
    public async Task CreateIngredient_ShouldBeReadableById()
    {
        var name = IntegrationTestHelpers.UniqueName("Ingrediente");

        var created = await IntegrationTestHelpers.CreateIngredientAsync(
            _client,
            name,
            UnitOfMeasure.Gram);

        var response = await _client.GetAsync($"/api/ingredients/{created.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);

        var fetched = await response.Content
            .ReadFromJsonAsync<IngredientDto>(IntegrationTestHelpers.JsonOptions);

        fetched.Should().NotBeNull();
        fetched!.Name.Should().Be(name);
        fetched.Unit.Should().Be(UnitOfMeasure.Kilogram);
    }

    [Fact]
    public async Task CreateIngredient_ShouldStoreMassInCanonicalUnitsWithoutConversionRows()
    {
        var created = await IntegrationTestHelpers.CreateIngredientAsync(
            _client,
            IntegrationTestHelpers.UniqueName("Harina"),
            UnitOfMeasure.Gram);

        var response = await _client.GetAsync($"/api/ingredients/{created.Id}");
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var fetched = await response.Content.ReadFromJsonAsync<IngredientDto>(IntegrationTestHelpers.JsonOptions);

        fetched!.Unit.Should().Be(UnitOfMeasure.Kilogram);
        fetched.UnitConversions.Should().BeEmpty();
    }

    [Fact]
    public async Task CreateIngredient_WithDuplicateName_ShouldReturnConflict()
    {
        var name = IntegrationTestHelpers.UniqueName("Duplicado");
        await IntegrationTestHelpers.CreateIngredientAsync(_client, name, UnitOfMeasure.Piece);

        var response = await _client.PostAsJsonAsync(
            "/api/ingredients",
            new CreateIngredientRequest(name.ToUpperInvariant(), UnitOfMeasure.Piece, null),
            IntegrationTestHelpers.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Conflict);
    }

    [Fact]
    public async Task CreateIngredient_WithBlankName_ShouldReturnBadRequest()
    {
        var response = await _client.PostAsJsonAsync(
            "/api/ingredients",
            new CreateIngredientRequest("   ", UnitOfMeasure.Piece, null),
            IntegrationTestHelpers.JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }

    [Fact]
    public async Task GetIngredient_WhenMissing_ShouldReturnNotFound()
    {
        var response = await _client.GetAsync("/api/ingredients/999999");

        response.StatusCode.Should().Be(HttpStatusCode.NotFound);
    }
}
