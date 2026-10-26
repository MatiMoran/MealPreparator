using MealPreparator.Application.Abstractions;
using MealPreparator.Infrastructure.Persistence;
using MealPreparator.Infrastructure.Repositories;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace MealPreparator.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddDbContext<MealPreparatorDbContext>((serviceProvider, options) =>
        {
            var currentConfiguration = serviceProvider.GetRequiredService<IConfiguration>();
            var connectionString = currentConfiguration.GetConnectionString("Default")
                ?? "Data Source=data/mealpreparator.db";

            var connectionStringBuilder = new SqliteConnectionStringBuilder(connectionString);
            connectionStringBuilder.DataSource = DatabasePathResolver.Resolve(connectionStringBuilder.DataSource);

            var databaseDirectory = Path.GetDirectoryName(connectionStringBuilder.DataSource);

            if (!string.IsNullOrWhiteSpace(databaseDirectory))
            {
                Directory.CreateDirectory(databaseDirectory);
            }

            options.UseSqlite(connectionStringBuilder.ToString());
        });

        services.AddScoped<IIngredientRepository, IngredientRepository>();
        services.AddScoped<IRecipeRepository, RecipeRepository>();
        services.AddScoped<ITagRepository, TagRepository>();
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        return services;
    }
}
