using MealPreparator.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MealPreparator.Infrastructure.Persistence.Migrations;

[DbContext(typeof(MealPreparatorDbContext))]
[Migration("20261026090000_NormalizeMeasurementStorage")]
public sealed class NormalizeMeasurementStorage : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            CREATE TABLE __MeasurementStorageConversionBackup (
                IngredientId INTEGER NOT NULL,
                Unit TEXT NOT NULL,
                Factor TEXT NOT NULL,
                Source TEXT NULL,
                Note TEXT NULL,
                PRIMARY KEY (IngredientId, Unit)
            );
            INSERT INTO __MeasurementStorageConversionBackup (IngredientId, Unit, Factor, Source, Note)
            SELECT IngredientId, Unit, Factor, Source, Note FROM IngredientUnitConversions;

            UPDATE RecipeIngredients
            SET Quantity = ROUND(
                Quantity *
                CASE
                    WHEN Unit = (SELECT Unit FROM Ingredients WHERE Id = RecipeIngredients.IngredientId) THEN 1
                    WHEN Unit IN ('Gram', 'Kilogram') AND
                         (SELECT Unit FROM Ingredients WHERE Id = RecipeIngredients.IngredientId) IN ('Gram', 'Kilogram') THEN
                        (CASE Unit WHEN 'Gram' THEN 0.001 ELSE 1 END) /
                        (CASE (SELECT Unit FROM Ingredients WHERE Id = RecipeIngredients.IngredientId) WHEN 'Gram' THEN 0.001 ELSE 1 END)
                    WHEN Unit IN ('Milliliter', 'Liter', 'Tablespoon', 'Teaspoon', 'Cup') AND
                         (SELECT Unit FROM Ingredients WHERE Id = RecipeIngredients.IngredientId) IN ('Milliliter', 'Liter', 'Tablespoon', 'Teaspoon', 'Cup') THEN
                        (CASE Unit
                            WHEN 'Milliliter' THEN 0.001
                            WHEN 'Liter' THEN 1
                            WHEN 'Tablespoon' THEN 0.015
                            WHEN 'Teaspoon' THEN 0.005
                            WHEN 'Cup' THEN 0.24
                        END) /
                        (CASE (SELECT Unit FROM Ingredients WHERE Id = RecipeIngredients.IngredientId)
                            WHEN 'Milliliter' THEN 0.001
                            WHEN 'Liter' THEN 1
                            WHEN 'Tablespoon' THEN 0.015
                            WHEN 'Teaspoon' THEN 0.005
                            WHEN 'Cup' THEN 0.24
                        END)
                    ELSE COALESCE((
                        SELECT Factor FROM IngredientUnitConversions
                        WHERE IngredientId = RecipeIngredients.IngredientId
                          AND Unit = RecipeIngredients.Unit
                    ), 1)
                END *
                CASE (SELECT Unit FROM Ingredients WHERE Id = RecipeIngredients.IngredientId)
                    WHEN 'Gram' THEN 0.001
                    WHEN 'Milliliter' THEN 0.001
                    WHEN 'Tablespoon' THEN 0.015
                    WHEN 'Teaspoon' THEN 0.005
                    WHEN 'Cup' THEN 0.24
                    ELSE 1
                END,
                12)
            WHERE Quantity IS NOT NULL;

            UPDATE IngredientUnitConversions
            SET Factor = ROUND(Factor * CASE
                WHEN (SELECT Unit FROM Ingredients WHERE Id = IngredientUnitConversions.IngredientId) IN ('Gram', 'Milliliter') THEN 0.001
                WHEN (SELECT Unit FROM Ingredients WHERE Id = IngredientUnitConversions.IngredientId) = 'Tablespoon' THEN 0.015
                WHEN (SELECT Unit FROM Ingredients WHERE Id = IngredientUnitConversions.IngredientId) = 'Teaspoon' THEN 0.005
                WHEN (SELECT Unit FROM Ingredients WHERE Id = IngredientUnitConversions.IngredientId) = 'Cup' THEN 0.24
                ELSE 1
            END, 12);

            DELETE FROM IngredientUnitConversions
            WHERE
                (Unit IN ('Gram', 'Kilogram') AND IngredientId IN
                    (SELECT Id FROM Ingredients WHERE Unit IN ('Gram', 'Kilogram')))
                OR (Unit IN ('Milliliter', 'Liter', 'Tablespoon', 'Teaspoon', 'Cup') AND IngredientId IN
                    (SELECT Id FROM Ingredients WHERE Unit IN ('Milliliter', 'Liter', 'Tablespoon', 'Teaspoon', 'Cup')))
                OR (Unit = 'Piece' AND IngredientId IN
                    (SELECT Id FROM Ingredients WHERE Unit = 'Piece'));

            UPDATE Ingredients
            SET Unit = CASE
                WHEN Unit = 'Gram' THEN 'Kilogram'
                WHEN Unit IN ('Milliliter', 'Tablespoon', 'Teaspoon', 'Cup') THEN 'Liter'
                ELSE Unit
            END
            WHERE Unit IS NOT NULL;
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql("""
            UPDATE RecipeIngredients
            SET Quantity = ROUND(
                Quantity /
                CASE
                    WHEN Unit IN ('Gram', 'Kilogram') AND
                         (SELECT Unit FROM Ingredients WHERE Id = RecipeIngredients.IngredientId) = 'Kilogram' THEN
                        CASE Unit WHEN 'Gram' THEN 0.001 ELSE 1 END
                    WHEN Unit IN ('Milliliter', 'Liter', 'Tablespoon', 'Teaspoon', 'Cup') AND
                         (SELECT Unit FROM Ingredients WHERE Id = RecipeIngredients.IngredientId) = 'Liter' THEN
                        CASE Unit
                            WHEN 'Milliliter' THEN 0.001
                            WHEN 'Liter' THEN 1
                            WHEN 'Tablespoon' THEN 0.015
                            WHEN 'Teaspoon' THEN 0.005
                            WHEN 'Cup' THEN 0.24
                        END
                    ELSE COALESCE((
                        SELECT Factor FROM IngredientUnitConversions
                        WHERE IngredientId = RecipeIngredients.IngredientId
                          AND Unit = RecipeIngredients.Unit
                    ), 1)
                END,
                12)
            WHERE Quantity IS NOT NULL;

            UPDATE Ingredients
            SET Unit = CASE
                WHEN Unit = 'Kilogram' THEN 'Gram'
                WHEN Unit = 'Liter' THEN 'Milliliter'
                ELSE Unit
            END
            WHERE Unit IS NOT NULL;

            DELETE FROM IngredientUnitConversions;
            INSERT INTO IngredientUnitConversions (IngredientId, Unit, Factor, Source, Note)
            SELECT IngredientId, Unit, Factor, Source, Note FROM __MeasurementStorageConversionBackup;
            DROP TABLE __MeasurementStorageConversionBackup;
            """);
    }
}
