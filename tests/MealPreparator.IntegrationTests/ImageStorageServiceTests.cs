using System.Net;
using FluentAssertions;
using MealPreparator.Api.Services;
using MealPreparator.Application.Exceptions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.FileProviders;
using Xunit;

namespace MealPreparator.IntegrationTests;

public sealed class ImageStorageServiceTests
{
    [Fact]
    public async Task StoreAsync_ShouldDownloadPublicImageToLocalStorage()
    {
        var root = Path.Combine(Path.GetTempPath(), $"meal-images-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new ByteArrayContent([137, 80, 78, 71, 13, 10, 26, 10])
                {
                    Headers = { ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("image/png") }
                }
            });
            using var client = new HttpClient(handler);
            var storage = new ImageStorageService(client, new TestEnvironment(root));

            var path = await storage.StoreAsync("https://1.1.1.1/picture", "recipe", CancellationToken.None);

            path.Should().StartWith("images/recipes/").And.EndWith(".png");
            File.Exists(Path.Combine(root, path!.Replace('/', Path.DirectorySeparatorChar))).Should().BeTrue();
            handler.RequestCount.Should().Be(1);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Theory]
    [InlineData("http://127.0.0.1/image.png")]
    [InlineData("http://192.168.1.10/image.png")]
    [InlineData("file:///etc/passwd")]
    public async Task StoreAsync_ShouldRejectPrivateOrUnsupportedAddresses(string url)
    {
        var root = Path.Combine(Path.GetTempPath(), $"meal-images-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        try
        {
            using var client = new HttpClient(new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.OK)));
            var storage = new ImageStorageService(client, new TestEnvironment(root));

            var act = () => storage.StoreAsync(url, "ingredient", CancellationToken.None);

            await act.Should().ThrowAsync<ValidationException>();
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    [Fact]
    public async Task StoreAsync_ShouldRevalidateRedirectTargets()
    {
        var root = Path.Combine(Path.GetTempPath(), $"meal-images-{Guid.NewGuid():N}");
        Directory.CreateDirectory(root);
        var handler = new StubHandler(_ => new HttpResponseMessage(HttpStatusCode.Redirect)
        {
            Headers = { Location = new Uri("http://127.0.0.1/private.png") }
        });
        try
        {
            using var client = new HttpClient(handler);
            var storage = new ImageStorageService(client, new TestEnvironment(root));

            var act = () => storage.StoreAsync("https://1.1.1.1/redirect", "recipe", CancellationToken.None);

            await act.Should().ThrowAsync<ValidationException>();
            handler.RequestCount.Should().Be(1);
        }
        finally
        {
            Directory.Delete(root, recursive: true);
        }
    }

    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> responseFactory) : HttpMessageHandler
    {
        public int RequestCount { get; private set; }

        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            RequestCount++;
            return Task.FromResult(responseFactory(request));
        }
    }

    private sealed class TestEnvironment(string root) : IWebHostEnvironment
    {
        public string ApplicationName { get; set; } = "ImageStorageServiceTests";
        public IFileProvider ContentRootFileProvider { get; set; } = new NullFileProvider();
        public string ContentRootPath { get; set; } = root;
        public string EnvironmentName { get; set; } = "Testing";
        public string WebRootPath { get; set; } = root;
        public IFileProvider WebRootFileProvider { get; set; } = new NullFileProvider();
    }
}
