using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MealPreparator.Infrastructure.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class AddImageUrlsToRecipesAndIngredients : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Recipes",
                type: "TEXT",
                maxLength: 500,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "ImageUrl",
                table: "Ingredients",
                type: "TEXT",
                maxLength: 500,
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Recipes");

            migrationBuilder.DropColumn(
                name: "ImageUrl",
                table: "Ingredients");
        }
    }
}
