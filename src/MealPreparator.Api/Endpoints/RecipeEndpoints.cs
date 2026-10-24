using MealPreparator.Application.Dtos;
using MealPreparator.Application.Services;

namespace MealPreparator.Api.Endpoints;

public static class RecipeEndpoints
{
    public static IEndpointRouteBuilder MapRecipeEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/recipes")
            .WithTags("Recipes");

        group.MapGet("/", async Task<IResult> (
            RecipeService service,
            CancellationToken cancellationToken) =>
        {
            var recipes = await service.GetAllAsync(cancellationToken);
            return Results.Ok(recipes);
        });

        group.MapGet("/{id:int}", async Task<IResult> (
            int id,
            RecipeService service,
            CancellationToken cancellationToken) =>
        {
            var recipe = await service.GetByIdAsync(id, cancellationToken);
            return Results.Ok(recipe);
        });

        group.MapPost("/", async Task<IResult> (
            CreateRecipeRequest request,
            RecipeService service,
            CancellationToken cancellationToken) =>
        {
            var recipe = await service.CreateAsync(request, cancellationToken);
            return Results.Created($"/api/recipes/{recipe.Id}", recipe);
        });

        group.MapPut("/{id:int}", async Task<IResult> (
            int id,
            UpdateRecipeRequest request,
            RecipeService service,
            CancellationToken cancellationToken) =>
        {
            var recipe = await service.UpdateAsync(id, request, cancellationToken);
            return Results.Ok(recipe);
        });

        group.MapDelete("/{id:int}", async Task<IResult> (
            int id,
            RecipeService service,
            CancellationToken cancellationToken) =>
        {
            await service.DeleteAsync(id, cancellationToken);
            return Results.NoContent();
        });

        return endpoints;
    }
}
