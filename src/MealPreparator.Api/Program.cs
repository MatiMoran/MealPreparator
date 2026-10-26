using System.Text.Json.Serialization;
using MealPreparator.Api.Endpoints;
using MealPreparator.Api.Middleware;
using MealPreparator.Application;
using MealPreparator.Infrastructure;
using MealPreparator.Infrastructure.Persistence;
using MealPreparator.Api.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddApplication();
builder.Services.AddInfrastructure();
builder.Services.AddHttpClient<ImageStorageService>()
    .ConfigureHttpClient(client => client.Timeout = TimeSpan.FromSeconds(8))
    .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler { AllowAutoRedirect = false });
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi();
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
        policy.AllowAnyOrigin()
            .AllowAnyHeader()
            .AllowAnyMethod());
});

builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var app = builder.Build();

app.UseCors("Frontend");
app.UseStaticFiles();
app.UseMiddleware<ExceptionHandlingMiddleware>();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.MapGet("/health", () => Results.Ok(new { status = "ok" }));
app.MapIngredientEndpoints();
app.MapRecipeEndpoints();
app.MapTagEndpoints();
app.MapShoppingListEndpoints();
app.MapImageEndpoints();

await using (var scope = app.Services.CreateAsyncScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<MealPreparatorDbContext>();
    await DatabaseInitializer.InitializeAsync(dbContext);
    await scope.ServiceProvider.GetRequiredService<ImageStorageService>()
        .LocalizeExistingImagesAsync(dbContext, CancellationToken.None);
}

app.Run();

public partial class Program
{
}
