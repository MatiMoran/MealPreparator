using MealPreparator.Application.Services;
using Microsoft.Extensions.DependencyInjection;

namespace MealPreparator.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddScoped<IngredientService>();
        services.AddScoped<RecipeService>();
        services.AddScoped<TagService>();
        services.AddScoped<ShoppingListService>();

        return services;
    }
}
