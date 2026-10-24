using MealPreparator.Application.Dtos;
using MealPreparator.Application.Services;

namespace MealPreparator.Api.Endpoints;

public static class ShoppingListEndpoints
{
    public static IEndpointRouteBuilder MapShoppingListEndpoints(this IEndpointRouteBuilder endpoints)
    {
        endpoints.MapPost("/api/shopping-list", async Task<IResult> (
            ShoppingListRequest request,
            ShoppingListService service,
            CancellationToken cancellationToken) =>
        {
            var shoppingList = await service.CalculateAsync(request, cancellationToken);
            return Results.Ok(shoppingList);
        })
        .WithTags("ShoppingList");

        return endpoints;
    }
}
