using MealPreparator.Application.Dtos;
using MealPreparator.Application.Services;

namespace MealPreparator.Api.Endpoints;

public static class IngredientEndpoints
{
    public static IEndpointRouteBuilder MapIngredientEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/ingredients")
            .WithTags("Ingredients");

        group.MapGet("/", async Task<IResult> (
            IngredientService service,
            CancellationToken cancellationToken) =>
        {
            var ingredients = await service.GetAllAsync(cancellationToken);
            return Results.Ok(ingredients);
        });

        group.MapGet("/{id:int}", async Task<IResult> (
            int id,
            IngredientService service,
            CancellationToken cancellationToken) =>
        {
            var ingredient = await service.GetByIdAsync(id, cancellationToken);
            return Results.Ok(ingredient);
        });

        group.MapPost("/", async Task<IResult> (
            CreateIngredientRequest request,
            IngredientService service,
            CancellationToken cancellationToken) =>
        {
            var ingredient = await service.CreateAsync(request, cancellationToken);
            return Results.Created($"/api/ingredients/{ingredient.Id}", ingredient);
        });

        group.MapPut("/{id:int}", async Task<IResult> (
            int id,
            UpdateIngredientRequest request,
            IngredientService service,
            CancellationToken cancellationToken) =>
        {
            var ingredient = await service.UpdateAsync(id, request, cancellationToken);
            return Results.Ok(ingredient);
        });

        group.MapDelete("/{id:int}", async Task<IResult> (
            int id,
            IngredientService service,
            CancellationToken cancellationToken) =>
        {
            await service.DeleteAsync(id, cancellationToken);
            return Results.NoContent();
        });

        return endpoints;
    }
}
