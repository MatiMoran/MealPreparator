using MealPreparator.Application.Dtos;
using MealPreparator.Application.Services;

namespace MealPreparator.Api.Endpoints;

public static class TagEndpoints
{
    public static IEndpointRouteBuilder MapTagEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/tags").WithTags("Tags");
        group.MapGet("/", async Task<IResult> (TagService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.GetAllAsync(cancellationToken)));
        group.MapPost("/", async Task<IResult> (TagNameRequest request, TagService service, CancellationToken cancellationToken) =>
        {
            var tag = await service.CreateAsync(request, cancellationToken);
            return Results.Created($"/api/tags/{tag.Id}", tag);
        });
        group.MapPut("/{id:int}", async Task<IResult> (int id, TagNameRequest request, TagService service, CancellationToken cancellationToken) =>
            Results.Ok(await service.UpdateAsync(id, request, cancellationToken)));
        group.MapDelete("/{id:int}", async Task<IResult> (int id, TagService service, CancellationToken cancellationToken) =>
        {
            await service.DeleteAsync(id, cancellationToken);
            return Results.NoContent();
        });
        return endpoints;
    }
}
