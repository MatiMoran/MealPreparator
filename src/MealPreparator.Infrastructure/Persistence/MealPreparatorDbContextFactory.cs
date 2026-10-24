using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace MealPreparator.Infrastructure.Persistence;

public sealed class MealPreparatorDbContextFactory : IDesignTimeDbContextFactory<MealPreparatorDbContext>
{
    public MealPreparatorDbContext CreateDbContext(string[] args)
    {
        var connectionStringBuilder = new SqliteConnectionStringBuilder
        {
            DataSource = DatabasePathResolver.Resolve("data/mealpreparator.db")
        };

        var databaseDirectory = Path.GetDirectoryName(connectionStringBuilder.DataSource);

        if (!string.IsNullOrWhiteSpace(databaseDirectory))
        {
            Directory.CreateDirectory(databaseDirectory);
        }

        var options = new DbContextOptionsBuilder<MealPreparatorDbContext>()
            .UseSqlite(connectionStringBuilder.ToString())
            .Options;

        return new MealPreparatorDbContext(options);
    }
}
