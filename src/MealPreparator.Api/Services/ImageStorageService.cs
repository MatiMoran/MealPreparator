using System.Net;
using System.Net.Sockets;
using MealPreparator.Application.Exceptions;
using Microsoft.EntityFrameworkCore;

namespace MealPreparator.Api.Services;

public sealed class ImageStorageService(HttpClient httpClient, IWebHostEnvironment environment)
{
    private const int MaxImageBytes = 5 * 1024 * 1024;
    private static readonly string[] AllowedContentTypes = ["image/jpeg", "image/png", "image/webp", "image/gif"];

    public async Task<string?> StoreAsync(string? imageUrl, string category, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(imageUrl)) return null;
        var value = imageUrl.Trim();
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri))
        {
            var localPath = value.TrimStart('/');
            if (!localPath.StartsWith("images/", StringComparison.Ordinal) || localPath.Contains("..", StringComparison.Ordinal))
                throw new ValidationException("La ruta de la imagen no es válida.");
            return localPath;
        }

        var currentUri = uri;
        HttpResponseMessage? response = null;
        for (var redirect = 0; redirect <= 3; redirect++)
        {
            if (currentUri.Scheme is not ("http" or "https") || !string.IsNullOrEmpty(currentUri.UserInfo) ||
                !await IsPublicHostAsync(currentUri.Host, cancellationToken))
                throw new ValidationException("La URL de imagen debe apuntar a un servidor público HTTP o HTTPS.");

            try
            {
                response = await httpClient.GetAsync(currentUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            }
            catch (HttpRequestException exception)
            {
                throw new ValidationException("No se pudo descargar la imagen desde esa dirección.", exception);
            }
            catch (TaskCanceledException exception) when (!cancellationToken.IsCancellationRequested)
            {
                throw new ValidationException("La descarga de la imagen superó el tiempo permitido.", exception);
            }
            if ((int)response.StatusCode is >= 300 and < 400 && response.Headers.Location is not null)
            {
                var location = response.Headers.Location;
                response.Dispose();
                currentUri = location.IsAbsoluteUri ? location : new Uri(currentUri, location);
                continue;
            }
            break;
        }

        using (response)
        {
        if (response is null) throw new ValidationException("No se pudo descargar la imagen.");
        if (!response.IsSuccessStatusCode || response.Content.Headers.ContentLength is > MaxImageBytes)
            throw new ValidationException("No se pudo descargar la imagen o supera el límite de 5 MB.");
        var contentType = response.Content.Headers.ContentType?.MediaType?.ToLowerInvariant();
        if (contentType is null || !AllowedContentTypes.Contains(contentType))
            throw new ValidationException("La URL no devolvió una imagen compatible.");

        await using var input = await response.Content.ReadAsStreamAsync(cancellationToken);
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await input.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + read > MaxImageBytes) throw new ValidationException("La imagen supera el límite de 5 MB.");
            await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
        }

        if (!HasValidImageSignature(buffer.ToArray(), contentType))
            throw new ValidationException("El contenido descargado no es una imagen válida.");
        var extension = contentType switch { "image/jpeg" => ".jpg", "image/png" => ".png", "image/webp" => ".webp", _ => ".gif" };
        return await SaveAsync(buffer.ToArray(), extension, category, cancellationToken);
        }
    }

    public async Task LocalizeExistingImagesAsync(MealPreparator.Infrastructure.Persistence.MealPreparatorDbContext dbContext, CancellationToken cancellationToken)
    {
        var recipes = await dbContext.Recipes.ToListAsync(cancellationToken);
        var ingredients = await dbContext.Ingredients.ToListAsync(cancellationToken);
        var recipePaths = await Task.WhenAll(recipes.Select(recipe => LocalizeAsync(recipe.ImageUrl, "recipe", cancellationToken)));
        var ingredientPaths = await Task.WhenAll(ingredients.Select(ingredient => LocalizeAsync(ingredient.ImageUrl, "ingredient", cancellationToken)));
        var changed = false;
        for (var index = 0; index < recipes.Count; index++)
        {
            var recipe = recipes[index];
            var path = recipePaths[index];
            if (path != recipe.ImageUrl) { recipe.SetImageUrl(path); changed = true; }
        }
        for (var index = 0; index < ingredients.Count; index++)
        {
            var ingredient = ingredients[index];
            var path = ingredientPaths[index];
            if (path != ingredient.ImageUrl) { ingredient.SetImageUrl(path); changed = true; }
        }
        if (changed) await dbContext.SaveChangesAsync(cancellationToken);
    }

    private async Task<string> LocalizeAsync(string? value, string category, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(value)) return "images/placeholder.svg";
        if (Uri.TryCreate(value, UriKind.Absolute, out _))
        {
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeout.CancelAfter(TimeSpan.FromSeconds(5));
            try { return await StoreAsync(value, category, timeout.Token) ?? "images/placeholder.svg"; }
            catch (Exception exception) when (exception is HttpRequestException or OperationCanceledException or ValidationException)
            { return "images/placeholder.svg"; }
        }

        var normalized = value.TrimStart('/');
        var root = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        return normalized.StartsWith("images/", StringComparison.Ordinal) &&
               File.Exists(Path.Combine(root, normalized.Replace('/', Path.DirectorySeparatorChar)))
            ? normalized
            : "images/placeholder.svg";
    }

    public async Task<string> StoreUploadAsync(Stream input, string fileName, string category, CancellationToken cancellationToken)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        if (extension is not (".jpg" or ".jpeg" or ".png" or ".webp" or ".gif"))
            throw new ValidationException("El formato de imagen no es compatible.");
        using var buffer = new MemoryStream();
        var chunk = new byte[81920];
        int read;
        while ((read = await input.ReadAsync(chunk, cancellationToken)) > 0)
        {
            if (buffer.Length + read > MaxImageBytes) throw new ValidationException("La imagen supera el límite de 5 MB.");
            await buffer.WriteAsync(chunk.AsMemory(0, read), cancellationToken);
        }
        if (buffer.Length == 0) throw new ValidationException("La imagen está vacía.");
        var bytes = buffer.ToArray();
        var contentType = extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg", ".png" => "image/png", ".webp" => "image/webp", _ => "image/gif"
        };
        if (!HasValidImageSignature(bytes, contentType)) throw new ValidationException("El archivo no contiene una imagen válida.");
        return await SaveAsync(bytes, extension, category, cancellationToken);
    }

    private async Task<string> SaveAsync(byte[] bytes, string extension, string category, CancellationToken cancellationToken)
    {
        var folder = category == "ingredient" ? "ingredients" : "recipes";
        var root = environment.WebRootPath ?? Path.Combine(environment.ContentRootPath, "wwwroot");
        var directory = Path.Combine(root, "images", folder);
        Directory.CreateDirectory(directory);
        var fileName = $"{Guid.NewGuid():N}{extension}";
        await File.WriteAllBytesAsync(Path.Combine(directory, fileName), bytes, cancellationToken);
        return $"images/{folder}/{fileName}";
    }

    private static async Task<bool> IsPublicHostAsync(string host, CancellationToken cancellationToken)
    {
        IPAddress[] addresses;
        if (IPAddress.TryParse(host, out var address)) addresses = [address];
        else
        {
            try { addresses = await Dns.GetHostAddressesAsync(host, cancellationToken); }
            catch (SocketException) { return false; }
        }

        return addresses.Length > 0 && addresses.All(IsPublicAddress);
    }

    private static bool IsPublicAddress(IPAddress address)
    {
        if (IPAddress.IsLoopback(address) || address.IsIPv6LinkLocal || address.IsIPv6SiteLocal || address.IsIPv6Multicast) return false;
        if (address.AddressFamily == AddressFamily.InterNetworkV6 && address.IsIPv4MappedToIPv6) return IsPublicAddress(address.MapToIPv4());
        if (address.AddressFamily == AddressFamily.InterNetworkV6)
        {
            var ipv6Bytes = address.GetAddressBytes();
            return (ipv6Bytes[0] & 0xE0) == 0x20;
        }
        var bytes = address.GetAddressBytes();
        return !(bytes[0] == 0 || bytes[0] == 10 || bytes[0] == 127 || bytes[0] >= 224 ||
            (bytes[0] == 100 && bytes[1] is >= 64 and <= 127) ||
            (bytes[0] == 169 && bytes[1] == 254) || (bytes[0] == 172 && bytes[1] is >= 16 and <= 31) ||
            (bytes[0] == 192 && (bytes[1] == 168 || bytes[1] == 0)) ||
            (bytes[0] == 198 && bytes[1] is 18 or 19));
    }

    private static bool HasValidImageSignature(byte[] bytes, string contentType) => contentType switch
    {
        "image/jpeg" => bytes.Length >= 3 && bytes[0] == 0xFF && bytes[1] == 0xD8 && bytes[2] == 0xFF,
        "image/png" => bytes.Length >= 8 && bytes.AsSpan(0, 8).SequenceEqual(new byte[] { 137, 80, 78, 71, 13, 10, 26, 10 }),
        "image/gif" => bytes.Length >= 6 && (System.Text.Encoding.ASCII.GetString(bytes, 0, 6) is "GIF87a" or "GIF89a"),
        "image/webp" => bytes.Length >= 12 && System.Text.Encoding.ASCII.GetString(bytes, 0, 4) == "RIFF" && System.Text.Encoding.ASCII.GetString(bytes, 8, 4) == "WEBP",
        _ => false
    };
}
