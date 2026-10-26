using FluentAssertions;
using MealPreparator.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using Xunit;

namespace MealPreparator.IntegrationTests;

public sealed class MeasurementMigrationTests
{
    [Fact]
    public async Task Migration_ShouldNormalizeStorageAndDown_ShouldRestoreLegacyValues()
    {
        var path = Path.Combine(Path.GetTempPath(), $"meal-migration-{Guid.NewGuid():N}.db");
        var options = new DbContextOptionsBuilder<MealPreparatorDbContext>()
            .UseSqlite($"Data Source={path}")
            .Options;
        try
        {
            await using var db = new MealPreparatorDbContext(options);
            var migrator = db.GetService<IMigrator>();
            await migrator.MigrateAsync("20261025190000_RecipeTags");
            await db.Database.ExecuteSqlRawAsync("""
                INSERT INTO Ingredients (Id, Name, Unit, IsBinary) VALUES
                    (1, 'Harina de prueba', 'Gram', 0),
                    (2, 'Aceite de prueba', 'Milliliter', 0);
                INSERT INTO Recipes (Id, Name, Servings) VALUES
                    (1, 'Receta de prueba', 2),
                    (2, 'Receta alternativa de prueba', 2);
                INSERT INTO RecipeIngredients (RecipeId, IngredientId, Quantity, Unit) VALUES
                    (1, 1, '400', 'Gram'),
                    (2, 1, '1', 'Kilogram'),
                    (1, 2, '2', 'Tablespoon');
                INSERT INTO IngredientUnitConversions (IngredientId, Unit, Factor, Source, Note)
                    VALUES (1, 'Cup', '185', NULL, NULL);
                INSERT INTO IngredientUnitConversions (IngredientId, Unit, Factor, Source, Note)
                    VALUES (1, 'Kilogram', '1000', 'legacy source', 'legacy note');
                """);

            await migrator.MigrateAsync();

            (await ScalarStringAsync(db, "SELECT Unit FROM Ingredients WHERE Id = 1")).Should().Be("Kilogram");
            (await ScalarDecimalAsync(db, "SELECT CAST(Quantity AS REAL) FROM RecipeIngredients WHERE IngredientId = 1 AND Unit = 'Gram'")).Should().BeApproximately(0.4m, 0.0000001m);
            (await ScalarDecimalAsync(db, "SELECT CAST(Quantity AS REAL) FROM RecipeIngredients WHERE RecipeId = 2 AND IngredientId = 1")).Should().BeApproximately(1m, 0.0000001m);
            (await ScalarDecimalAsync(db, "SELECT CAST(Factor AS REAL) FROM IngredientUnitConversions WHERE IngredientId = 1 AND Unit = 'Cup'")).Should().BeApproximately(0.185m, 0.0000001m);
            (await ScalarDecimalAsync(db, "SELECT COUNT(*) FROM IngredientUnitConversions WHERE IngredientId = 1 AND Unit = 'Kilogram'")).Should().Be(0m);
            (await ScalarDecimalAsync(db, "SELECT CAST(Quantity AS REAL) FROM RecipeIngredients WHERE IngredientId = 2")).Should().BeApproximately(0.03m, 0.0000001m);

            await migrator.MigrateAsync("20261025190000_RecipeTags");

            (await ScalarStringAsync(db, "SELECT Unit FROM Ingredients WHERE Id = 1")).Should().Be("Gram");
            (await ScalarDecimalAsync(db, "SELECT CAST(Quantity AS REAL) FROM RecipeIngredients WHERE IngredientId = 1 AND Unit = 'Gram'")).Should().BeApproximately(400m, 0.0000001m);
            (await ScalarDecimalAsync(db, "SELECT CAST(Factor AS REAL) FROM IngredientUnitConversions WHERE IngredientId = 1 AND Unit = 'Cup'")).Should().BeApproximately(185m, 0.0000001m);
            (await ScalarStringAsync(db, "SELECT Source || ':' || Note FROM IngredientUnitConversions WHERE IngredientId = 1 AND Unit = 'Kilogram'")).Should().Be("legacy source:legacy note");
        }
        finally
        {
            foreach (var suffix in new[] { "", "-wal", "-shm" })
            {
                var file = path + suffix;
                if (File.Exists(file)) File.Delete(file);
            }
        }
    }

    private static async Task<string> ScalarStringAsync(MealPreparatorDbContext db, string sql)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        if (command.Connection!.State != System.Data.ConnectionState.Open) await command.Connection.OpenAsync();
        return Convert.ToString(await command.ExecuteScalarAsync())!;
    }

    private static async Task<decimal> ScalarDecimalAsync(MealPreparatorDbContext db, string sql)
    {
        await using var command = db.Database.GetDbConnection().CreateCommand();
        command.CommandText = sql;
        if (command.Connection!.State != System.Data.ConnectionState.Open) await command.Connection.OpenAsync();
        return Convert.ToDecimal(await command.ExecuteScalarAsync(), System.Globalization.CultureInfo.InvariantCulture);
    }
}
