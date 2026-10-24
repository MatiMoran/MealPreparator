using Microsoft.EntityFrameworkCore;

namespace MealPreparator.Infrastructure.Persistence;

public static class DatabaseInitializer
{
    public static async Task InitializeAsync(
        MealPreparatorDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        await dbContext.Database.MigrateAsync(cancellationToken);
        await DatabaseSeeder.SeedAsync(dbContext, cancellationToken);
    }
}
