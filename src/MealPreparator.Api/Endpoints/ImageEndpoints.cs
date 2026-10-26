using MealPreparator.Application.Dtos;
using MealPreparator.Api.Services;

namespace MealPreparator.Api.Endpoints;

public static class ImageEndpoints
{
    public static IEndpointRouteBuilder MapImageEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/images")
            .WithTags("Images");

        group.MapPost("/upload", async Task<IResult> (
            HttpRequest request,
            ImageStorageService images,
            CancellationToken cancellationToken) =>
        {
            if (!request.HasFormContentType)
                return Results.BadRequest("Expected multipart/form-data");

            var form = await request.ReadFormAsync(cancellationToken);
            var file = form.Files.GetFile("file");
            if (file is null || file.Length == 0)
                return Results.BadRequest("No file uploaded");

            var type = form["type"].ToString().ToLowerInvariant();
            var url = await images.StoreUploadAsync(file.OpenReadStream(), file.FileName,
                type == "ingredient" ? "ingredient" : "recipe", cancellationToken);
            return Results.Ok(new ImageUploadResponse(url));
        });

        return endpoints;
    }
}
