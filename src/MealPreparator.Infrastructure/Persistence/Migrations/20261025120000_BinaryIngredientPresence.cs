using MealPreparator.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MealPreparator.Infrastructure.Persistence.Migrations;

[DbContext(typeof(MealPreparatorDbContext))]
[Migration("20261025120000_BinaryIngredientPresence")]
public sealed class BinaryIngredientPresence : Migration
{
    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<bool>(
            name: "IsBinary",
            table: "Ingredients",
            type: "INTEGER",
            nullable: false,
            defaultValue: false);

        migrationBuilder.Sql("UPDATE Ingredients SET IsBinary = 1 WHERE Unit = 'Binary';");

        migrationBuilder.AlterColumn<string>(
            name: "Unit",
            table: "Ingredients",
            type: "TEXT",
            maxLength: 32,
            nullable: true,
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 32);

        migrationBuilder.AlterColumn<decimal>(
            name: "Quantity",
            table: "RecipeIngredients",
            type: "TEXT",
            nullable: true,
            oldClrType: typeof(decimal),
            oldType: "TEXT");

        migrationBuilder.Sql("UPDATE Ingredients SET Unit = NULL WHERE IsBinary = 1;");
        migrationBuilder.Sql(
            "UPDATE RecipeIngredients SET Quantity = NULL WHERE IngredientId IN (SELECT Id FROM Ingredients WHERE IsBinary = 1);");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.Sql(
            "UPDATE RecipeIngredients SET Quantity = 1 WHERE IngredientId IN (SELECT Id FROM Ingredients WHERE IsBinary = 1);");
        migrationBuilder.AlterColumn<decimal>(
            name: "Quantity",
            table: "RecipeIngredients",
            type: "TEXT",
            nullable: false,
            oldClrType: typeof(decimal),
            oldType: "TEXT",
            oldNullable: true);

        migrationBuilder.Sql("UPDATE Ingredients SET Unit = 'Binary' WHERE IsBinary = 1;");
        migrationBuilder.AlterColumn<string>(
            name: "Unit",
            table: "Ingredients",
            type: "TEXT",
            maxLength: 32,
            nullable: false,
            oldClrType: typeof(string),
            oldType: "TEXT",
            oldMaxLength: 32,
            oldNullable: true);

        migrationBuilder.DropColumn(name: "IsBinary", table: "Ingredients");
    }

    protected override void BuildTargetModel(ModelBuilder modelBuilder)
    {
#pragma warning disable 612, 618
        modelBuilder.HasAnnotation("ProductVersion", "10.0.13");

        modelBuilder.Entity("MealPreparator.Domain.Entities.Ingredient", entity =>
        {
            entity.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
            entity.Property<string>("ImageUrl").HasMaxLength(500).HasColumnType("TEXT");
            entity.Property<bool>("IsBinary").HasColumnType("INTEGER");
            entity.Property<string>("Name").IsRequired().HasMaxLength(200).HasColumnType("TEXT").UseCollation("NOCASE");
            entity.Property<string>("Unit").HasMaxLength(32).HasColumnType("TEXT");
            entity.HasKey("Id");
            entity.HasIndex("Name").IsUnique();
            entity.ToTable("Ingredients");
        });

        modelBuilder.Entity("MealPreparator.Domain.Entities.Recipe", entity =>
        {
            entity.Property<int>("Id").ValueGeneratedOnAdd().HasColumnType("INTEGER");
            entity.Property<string>("ImageUrl").HasMaxLength(500).HasColumnType("TEXT");
            entity.Property<string>("Instructions").HasMaxLength(4000).HasColumnType("TEXT");
            entity.Property<string>("Name").IsRequired().HasMaxLength(200).HasColumnType("TEXT");
            entity.Property<int>("Servings").HasColumnType("INTEGER");
            entity.HasKey("Id");
            entity.ToTable("Recipes");
        });

        modelBuilder.Entity("MealPreparator.Domain.Entities.RecipeIngredient", entity =>
        {
            entity.Property<int>("RecipeId").HasColumnType("INTEGER");
            entity.Property<int>("IngredientId").HasColumnType("INTEGER");
            entity.Property<decimal?>("Quantity").HasColumnType("TEXT");
            entity.HasKey("RecipeId", "IngredientId");
            entity.HasIndex("IngredientId");
            entity.ToTable("RecipeIngredients");
        });

        modelBuilder.Entity("MealPreparator.Domain.Entities.RecipeIngredient", entity =>
        {
            entity.HasOne("MealPreparator.Domain.Entities.Ingredient", "Ingredient")
                .WithMany().HasForeignKey("IngredientId").OnDelete(DeleteBehavior.Restrict).IsRequired();
            entity.HasOne("MealPreparator.Domain.Entities.Recipe", "Recipe")
                .WithMany("Ingredients").HasForeignKey("RecipeId").OnDelete(DeleteBehavior.Cascade).IsRequired();
            entity.Navigation("Ingredient");
            entity.Navigation("Recipe");
        });

        modelBuilder.Entity("MealPreparator.Domain.Entities.Recipe", entity => entity.Navigation("Ingredients"));
#pragma warning restore 612, 618
    }
}
