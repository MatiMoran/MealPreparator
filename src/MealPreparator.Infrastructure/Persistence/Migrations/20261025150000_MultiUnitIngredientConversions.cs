using MealPreparator.Domain.Enums;
using MealPreparator.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MealPreparator.Infrastructure.Persistence.Migrations;

[DbContext(typeof(MealPreparatorDbContext))]
[Migration("20261025150000_MultiUnitIngredientConversions")]
public sealed class MultiUnitIngredientConversions : Migration
{
    private const string FoodDataCentralSource = "https://fdc.nal.usda.gov/food-search/";
    private const string ArgentinaMeasuresSource = "https://www.argentina.gob.ar/normativa/nacional/resoluci%C3%B3n-684-2005-109796/texto";

    protected override void Up(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.AddColumn<string>(
            name: "Unit",
            table: "RecipeIngredients",
            type: "TEXT",
            maxLength: 32,
            nullable: true);

        migrationBuilder.Sql(
            "UPDATE RecipeIngredients SET Unit = (SELECT Unit FROM Ingredients WHERE Ingredients.Id = RecipeIngredients.IngredientId) " +
            "WHERE IngredientId IN (SELECT Id FROM Ingredients WHERE IsBinary = 0);");

        migrationBuilder.CreateTable(
            name: "IngredientUnitConversions",
            columns: table => new
            {
                IngredientId = table.Column<int>(type: "INTEGER", nullable: false),
                Unit = table.Column<string>(type: "TEXT", maxLength: 32, nullable: false),
                Factor = table.Column<decimal>(type: "TEXT", nullable: false),
                Source = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: true),
                Note = table.Column<string>(type: "TEXT", maxLength: 500, nullable: true)
            },
            constraints: table =>
            {
                table.PrimaryKey("PK_IngredientUnitConversions", x => new { x.IngredientId, x.Unit });
                table.ForeignKey(
                    name: "FK_IngredientUnitConversions_Ingredients_IngredientId",
                    column: x => x.IngredientId,
                    principalTable: "Ingredients",
                    principalColumn: "Id",
                    onDelete: ReferentialAction.Cascade);
            });

        Seed(migrationBuilder, "Zanahoria", UnitOfMeasure.Piece, 100m, null,
            "Valor práctico acordado para esta aplicación; el tamaño real de una zanahoria varía.");
        Seed(migrationBuilder, "Ajo", UnitOfMeasure.Piece, 3m, FoodDataCentralSource,
            "Diente de ajo mediano aproximado, en gramos comestibles.");
        Seed(migrationBuilder, "Cebolla", UnitOfMeasure.Gram, 1m / 110m, FoodDataCentralSource,
            "Una cebolla mediana se estima en 110 g comestibles.");
        Seed(migrationBuilder, "Morron", UnitOfMeasure.Gram, 1m / 119m, FoodDataCentralSource,
            "Un morrón mediano se estima en 119 g comestibles.");
        Seed(migrationBuilder, "Tomate", UnitOfMeasure.Piece, 123m, FoodDataCentralSource,
            "Un tomate mediano se estima en 123 g comestibles.");
        Seed(migrationBuilder, "Lechuga", UnitOfMeasure.Gram, 1m / 70m, FoodDataCentralSource,
            "Una planta chica se estima en 70 g comestibles; el tamaño varía mucho.");
        Seed(migrationBuilder, "Manzana", UnitOfMeasure.Gram, 1m / 182m, FoodDataCentralSource,
            "Una manzana mediana se estima en 182 g comestibles.");
        Seed(migrationBuilder, "Banana", UnitOfMeasure.Gram, 1m / 118m, FoodDataCentralSource,
            "Una banana mediana se estima en 118 g comestibles sin cáscara.");
        Seed(migrationBuilder, "Naranja", UnitOfMeasure.Gram, 1m / 131m, FoodDataCentralSource,
            "Una naranja mediana se estima en 131 g comestibles.");
        Seed(migrationBuilder, "Palta", UnitOfMeasure.Piece, 150m, FoodDataCentralSource,
            "Una palta mediana se estima en 150 g comestibles sin carozo ni cáscara.");
        Seed(migrationBuilder, "Pan", UnitOfMeasure.Piece, 25m, FoodDataCentralSource,
            "Una rodaja estándar se estima en 25 g.");
        Seed(migrationBuilder, "Papa", UnitOfMeasure.Piece, 213m, FoodDataCentralSource,
            "Una papa mediana se estima en 213 g comestibles.");
        Seed(migrationBuilder, "Huevo", UnitOfMeasure.Gram, 1m / 50m, FoodDataCentralSource,
            "Un huevo mediano sin cáscara se estima en 50 g.");
        Seed(migrationBuilder, "Choclo", UnitOfMeasure.Piece, 90m, FoodDataCentralSource,
            "Un choclo mediano se estima en 90 g de granos comestibles.");
        Seed(migrationBuilder, "Pollo", UnitOfMeasure.Piece, 174m, FoodDataCentralSource,
            "Una pechuga cocida deshuesada se estima en 174 g; el corte y la cocción cambian el peso.");

        Seed(migrationBuilder, "Arroz", UnitOfMeasure.Tablespoon, 12m, FoodDataCentralSource,
            "Arroz seco; el peso cambia si está cocido o según el tipo de grano.");
        Seed(migrationBuilder, "Arroz", UnitOfMeasure.Teaspoon, 4m, FoodDataCentralSource,
            "Arroz seco; el peso cambia si está cocido o según el tipo de grano.");
        Seed(migrationBuilder, "Arroz", UnitOfMeasure.Cup, 185m, FoodDataCentralSource,
            "Arroz seco, taza de referencia; el peso cambia si está cocido.");
        Seed(migrationBuilder, "Azucar", UnitOfMeasure.Tablespoon, 12.5m, FoodDataCentralSource,
            "Azúcar granulada, medida rasa aproximada.");
        Seed(migrationBuilder, "Azucar", UnitOfMeasure.Teaspoon, 4.2m, FoodDataCentralSource,
            "Azúcar granulada, medida rasa aproximada.");
        Seed(migrationBuilder, "Azucar", UnitOfMeasure.Cup, 200m, FoodDataCentralSource,
            "Azúcar granulada, taza de referencia.");
        Seed(migrationBuilder, "Fideos", UnitOfMeasure.Cup, 85m, FoodDataCentralSource,
            "Pasta seca corta; la forma de la pasta cambia el peso por taza.");
        Seed(migrationBuilder, "Yogur", UnitOfMeasure.Tablespoon, 15m, FoodDataCentralSource,
            "Yogur natural, peso aproximado por medida rasa.");
        Seed(migrationBuilder, "Yogur", UnitOfMeasure.Teaspoon, 5m, FoodDataCentralSource,
            "Yogur natural, peso aproximado por medida rasa.");
        Seed(migrationBuilder, "Yogur", UnitOfMeasure.Cup, 245m, FoodDataCentralSource,
            "Yogur natural, taza de referencia.");
        Seed(migrationBuilder, "Miel", UnitOfMeasure.Tablespoon, 10m, ArgentinaMeasuresSource,
            "Volumen según medida argentina para porciones de rotulado nutricional.");
        Seed(migrationBuilder, "Miel", UnitOfMeasure.Teaspoon, 5m, ArgentinaMeasuresSource,
            "Volumen según medida argentina para porciones de rotulado nutricional.");
        Seed(migrationBuilder, "Salsa de tomate", UnitOfMeasure.Tablespoon, 10m, ArgentinaMeasuresSource,
            "Volumen según medida argentina para porciones de rotulado nutricional.");
        Seed(migrationBuilder, "Salsa de tomate", UnitOfMeasure.Teaspoon, 5m, ArgentinaMeasuresSource,
            "Volumen según medida argentina para porciones de rotulado nutricional.");
        Seed(migrationBuilder, "Salsa de tomate", UnitOfMeasure.Cup, 200m, ArgentinaMeasuresSource,
            "Taza de 200 ml según medida argentina para porciones de rotulado nutricional.");
        Seed(migrationBuilder, "Bicarbonato", UnitOfMeasure.Tablespoon, 2m, ArgentinaMeasuresSource,
            "Conversión volumétrica desde cucharadita: 5 ml por cucharadita y 10 ml por cucharada.");
        Seed(migrationBuilder, "Bicarbonato", UnitOfMeasure.Milliliter, 0.2m, ArgentinaMeasuresSource,
            "Conversión volumétrica; no representa el peso del bicarbonato.");
    }

    protected override void Down(MigrationBuilder migrationBuilder)
    {
        migrationBuilder.DropTable(name: "IngredientUnitConversions");
        migrationBuilder.DropColumn(name: "Unit", table: "RecipeIngredients");
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

        modelBuilder.Entity("MealPreparator.Domain.Entities.IngredientUnitConversion", entity =>
        {
            entity.Property<int>("IngredientId").HasColumnType("INTEGER");
            entity.Property<decimal>("Factor").HasColumnType("TEXT");
            entity.Property<string>("Note").HasMaxLength(500).HasColumnType("TEXT");
            entity.Property<string>("Source").HasMaxLength(1000).HasColumnType("TEXT");
            entity.Property<string>("Unit").IsRequired().HasMaxLength(32).HasColumnType("TEXT");
            entity.HasKey("IngredientId", "Unit");
            entity.ToTable("IngredientUnitConversions");
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
            entity.Property<string>("Unit").HasMaxLength(32).HasColumnType("TEXT");
            entity.HasKey("RecipeId", "IngredientId");
            entity.HasIndex("IngredientId");
            entity.ToTable("RecipeIngredients");
        });

        modelBuilder.Entity("MealPreparator.Domain.Entities.IngredientUnitConversion", entity =>
        {
            entity.HasOne("MealPreparator.Domain.Entities.Ingredient", "Ingredient")
                .WithMany("UnitConversions").HasForeignKey("IngredientId")
                .OnDelete(DeleteBehavior.Cascade).IsRequired();
            entity.Navigation("Ingredient");
        });

        modelBuilder.Entity("MealPreparator.Domain.Entities.RecipeIngredient", entity =>
        {
            entity.HasOne("MealPreparator.Domain.Entities.Ingredient", "Ingredient")
                .WithMany().HasForeignKey("IngredientId")
                .OnDelete(DeleteBehavior.Restrict).IsRequired();
            entity.HasOne("MealPreparator.Domain.Entities.Recipe", "Recipe")
                .WithMany("Ingredients").HasForeignKey("RecipeId")
                .OnDelete(DeleteBehavior.Cascade).IsRequired();
            entity.Navigation("Ingredient");
            entity.Navigation("Recipe");
        });

        modelBuilder.Entity("MealPreparator.Domain.Entities.Ingredient", entity => entity.Navigation("UnitConversions"));
        modelBuilder.Entity("MealPreparator.Domain.Entities.Recipe", entity => entity.Navigation("Ingredients"));
#pragma warning restore 612, 618
    }

    private static void Seed(
        MigrationBuilder migrationBuilder,
        string ingredientName,
        UnitOfMeasure unit,
        decimal factor,
        string source,
        string note)
    {
        var escapedName = ingredientName.Replace("'", "''", StringComparison.Ordinal);
        var escapedSource = source?.Replace("'", "''", StringComparison.Ordinal);
        var escapedNote = note.Replace("'", "''", StringComparison.Ordinal);
        var sourceSql = escapedSource is null ? "NULL" : $"'{escapedSource}'";
        migrationBuilder.Sql(
            $"INSERT INTO IngredientUnitConversions (IngredientId, Unit, Factor, Source, Note) " +
            $"SELECT Id, '{unit}', '{factor.ToString(System.Globalization.CultureInfo.InvariantCulture)}', {sourceSql}, '{escapedNote}' " +
            $"FROM Ingredients WHERE Name = '{escapedName}' COLLATE NOCASE AND IsBinary = 0 " +
            $"AND Unit <> '{unit}' AND NOT EXISTS (SELECT 1 FROM IngredientUnitConversions WHERE IngredientId = Ingredients.Id AND Unit = '{unit}');");
    }
}
