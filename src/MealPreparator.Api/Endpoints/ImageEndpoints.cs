using MealPreparator.Application.Dtos;

namespace MealPreparator.Api.Endpoints;

public static class ImageEndpoints
{
    public static IEndpointRouteBuilder MapImageEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/api/images")
            .WithTags("Images");

        group.MapPost("/upload", async Task<IResult> (
            HttpRequest request,
            IWebHostEnvironment env,
            CancellationToken cancellationToken) =>
        {
            if (!request.HasFormContentType)
                return Results.BadRequest("Expected multipart/form-data");

            var form = await request.ReadFormAsync(cancellationToken);
            var file = form.Files.GetFile("file");
            if (file is null || file.Length == 0)
                return Results.BadRequest("No file uploaded");

            if (file.Length > 5 * 1024 * 1024)
                return Results.BadRequest("File too large (max 5MB)");

            var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
                return Results.BadRequest("Invalid file type");

            var type = form["type"].ToString().ToLowerInvariant();
            var folder = type == "ingredient" ? "ingredients" : "recipes";
            var webRoot = env.WebRootPath ?? Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            var dir = Path.Combine(webRoot, "images", folder);
            Directory.CreateDirectory(dir);

            var fileName = $"{Guid.NewGuid():N}{ext}";
            var path = Path.Combine(dir, fileName);
            await using var stream = File.Create(path);
            await file.CopyToAsync(stream, cancellationToken);

            var url = $"images/{folder}/{fileName}";
            return Results.Ok(new ImageUploadResponse(url));
        });

        return endpoints;
    }
}
