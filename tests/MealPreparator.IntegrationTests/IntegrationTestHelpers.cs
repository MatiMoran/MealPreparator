using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using FluentAssertions;
using MealPreparator.Application.Dtos;
using MealPreparator.Domain.Enums;

namespace MealPreparator.IntegrationTests;

internal static class IntegrationTestHelpers
{
    public static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web)
    {
        Converters = { new JsonStringEnumConverter() }
    };

    public static async Task<IngredientDto> CreateIngredientAsync(
        HttpClient client,
        string name,
        UnitOfMeasure unit,
        string? imageUrl = null)
    {
        var response = await client.PostAsJsonAsync(
            "/api/ingredients",
            new CreateIngredientRequest(name, unit, imageUrl),
            JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        return (await response.Content.ReadFromJsonAsync<IngredientDto>(JsonOptions))!;
    }

    public static async Task<RecipeDto> CreateRecipeAsync(
        HttpClient client,
        string name,
        int servings,
        string? instructions,
        params (int IngredientId, decimal Quantity)[] ingredients)
    {
        var request = new CreateRecipeRequest(
            name,
            servings,
            instructions,
            null,
            ingredients
                .Select(item => new RecipeIngredientRequest(item.IngredientId, item.Quantity))
                .ToList());

        var response = await client.PostAsJsonAsync("/api/recipes", request, JsonOptions);

        response.StatusCode.Should().Be(HttpStatusCode.Created);

        return (await response.Content.ReadFromJsonAsync<RecipeDto>(JsonOptions))!;
    }

    public static string UniqueName(string prefix)
    {
        return $"{prefix}-{Guid.NewGuid():N}";
    }
}
