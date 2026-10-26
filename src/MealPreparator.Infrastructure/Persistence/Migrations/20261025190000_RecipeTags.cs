using MealPreparator.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MealPreparator.Infrastructure.Persistence.Migrations;

[DbContext(typeof(MealPreparatorDbContext))]
[Migration("20261025190000_RecipeTags")]
public sealed class RecipeTags : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.CreateTable(
            name: "Tags",
            columns: table => new
            {
                Id = table.Column<int>(type: "INTEGER", nullable: false)
                    .Annotation("Sqlite:Autoincrement", true),
                Name = table.Column<string>(type: "TEXT", maxLength: 50, nullable: false, collation: "NOCASE")
            },
            constraints: table => table.PrimaryKey("PK_Tags", x => x.Id));

        migrationBuilder.CreateTable(
            name: "RecipeTags",
            columns: table => new
            {
                RecipeId = table.Column<int>(type: "INTEGER", nullable: false),
                TagId = table.Column<int>(type: "INTEGER", nullable: false)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_RecipeTags", x => new { x.RecipeId, x.TagId });
                table.ForeignKey(
                    name: "FK_RecipeTags_Recipes_RecipeId",
                    column: x => x.RecipeId,
                    principalTable: "Recipes",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
                table.ForeignKey(
                    name: "FK_RecipeTags_Tags_TagId",
                    column: x => x.TagId,
                    principalTable: "Tags",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        migrationBuilder.CreateIndex(
            name: "IX_RecipeTags_TagId",
            table: "RecipeTags",
            column: "TagId");

        migrationBuilder.CreateIndex(
            name: "IX_Tags_Name",
            table: "Tags",
            column: "Name",
            unique: true);

        migrationBuilder.Sql("""
            INSERT INTO Tags (Name) VALUES
                ('Pollo'),
                ('Postre'),
                ('Acompañamientos'),
                ('Meriendas y Desayunos');

            INSERT INTO RecipeTags (RecipeId, TagId)
            SELECT Recipes.Id, Tags.Id
            FROM Recipes CROSS JOIN Tags
            WHERE (Recipes.Name = 'Arroz con pollo' AND Tags.Name = 'Pollo')
               OR (Recipes.Name = 'Pollo al horno con papas' AND Tags.Name = 'Pollo')
               OR (Recipes.Name = 'Ensalada de frutas' AND Tags.Name IN ('Postre', 'Meriendas y Desayunos'))
               OR (Recipes.Name = 'Yogur con frutas' AND Tags.Name IN ('Postre', 'Meriendas y Desayunos'))
               OR (Recipes.Name = 'Tostadas con palta' AND Tags.Name = 'Meriendas y Desayunos')
               OR (Recipes.Name IN ('Salsa', 'Pure de papa', 'Puré de papa') AND Tags.Name = 'Acompañamientos');
            """);
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "RecipeTags");
        migrationBuilder.DropTable(name: "Tags");
    }
}
