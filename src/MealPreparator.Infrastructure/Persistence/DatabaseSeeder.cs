using System.Globalization;
using System.Text;
using MealPreparator.Domain.Entities;
using MealPreparator.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace MealPreparator.Infrastructure.Persistence;

public static class DatabaseSeeder
{
    public static async Task SeedAsync(
        MealPreparatorDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        if (await dbContext.Ingredients.AnyAsync(cancellationToken))
        {
            await EnsureBinaryIngredientsAsync(dbContext, cancellationToken);
            await EnsureDefaultConversionsAsync(dbContext, cancellationToken);
            return;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var rice = new Ingredient("Arroz", UnitOfMeasure.Gram, "images/ingredients/arroz.jpg");
        var chicken = new Ingredient("Pollo", UnitOfMeasure.Gram, "images/ingredients/pollo.jpg");
        var oil = new Ingredient("Aceite", UnitOfMeasure.Milliliter, "images/ingredients/aceite.jpg");
        var salt = new Ingredient("Sal", UnitOfMeasure.Gram, "images/ingredients/sal.jpg");
        var onion = new Ingredient("Cebolla", UnitOfMeasure.Piece, "images/ingredients/cebolla.jpg");
        var bellPepper = new Ingredient("Morron", UnitOfMeasure.Piece, "images/ingredients/morron.jpg");
        var tomato = new Ingredient("Tomate", UnitOfMeasure.Gram, "images/ingredients/tomate.jpg");
        var lettuce = new Ingredient("Lechuga", UnitOfMeasure.Piece, "images/ingredients/lechuga.jpg");
        var apple = new Ingredient("Manzana", UnitOfMeasure.Piece, "images/ingredients/manzana.jpg");
        var banana = new Ingredient("Banana", UnitOfMeasure.Piece, "images/ingredients/banana.jpg");
        var orange = new Ingredient("Naranja", UnitOfMeasure.Piece, "images/ingredients/naranja.jpg");
        var sugar = new Ingredient("Azucar", UnitOfMeasure.Gram, "images/ingredients/azucar.jpg");
        var yogurt = new Ingredient("Yogur", UnitOfMeasure.Gram, "images/ingredients/yogur.jpg");

        dbContext.Ingredients.AddRange(
            rice,
            chicken,
            oil,
            salt,
            onion,
            bellPepper,
            tomato,
            lettuce,
            apple,
            banana,
            orange,
            sugar,
            yogurt);

        await dbContext.SaveChangesAsync(cancellationToken);

        await EnsureBinaryIngredientsAsync(dbContext, cancellationToken);
        await EnsureDefaultConversionsAsync(dbContext, cancellationToken);

        var chickenWithRice = new Recipe(
            "Arroz con pollo",
            4,
            "1. Cocinar el arroz. 2. Saltear la cebolla y el morron. 3. Agregar el pollo y cocinar hasta dorar. 4. Mezclar todo y condimentar.",
            "images/recipes/arroz-con-pollo.jpg");

        chickenWithRice.ReplaceIngredients(
        [
            new RecipeIngredient(rice, 400m),
            new RecipeIngredient(chicken, 600m),
            new RecipeIngredient(oil, null),
            new RecipeIngredient(salt, null),
            new RecipeIngredient(onion, 1m),
            new RecipeIngredient(bellPepper, 1m)
        ]);

        var fruitSalad = new Recipe(
            "Ensalada de frutas",
            2,
            "1. Cortar la fruta en cubos. 2. Mezclar con el yogur y el azucar. 3. Servir fria.",
            "images/recipes/ensalada-de-frutas.jpg");

        var tags = await dbContext.Tags.ToListAsync(cancellationToken);
        var tagsByName = tags.ToDictionary(tag => tag.Name, StringComparer.OrdinalIgnoreCase);
        chickenWithRice.ReplaceTags([tagsByName["Pollo"]]);
        fruitSalad.ReplaceTags([tagsByName["Postre"], tagsByName["Meriendas y Desayunos"]]);

        fruitSalad.ReplaceIngredients(
        [
            new RecipeIngredient(apple, 2m),
            new RecipeIngredient(banana, 2m),
            new RecipeIngredient(orange, 1m),
            new RecipeIngredient(sugar, 20m),
            new RecipeIngredient(yogurt, 200m)
        ]);

        dbContext.Recipes.AddRange(chickenWithRice, fruitSalad);

        await dbContext.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
    }

    private static async Task EnsureBinaryIngredientsAsync(
        MealPreparatorDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var pantryIngredients = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["aceite"] = "Aceite",
            ["sal"] = "Sal",
            ["oregano"] = "Orégano",
            ["pimenton"] = "Pimentón",
            ["aji molido"] = "Ají molido",
            ["pimienta negra"] = "Pimienta negra",
            ["aceite de oliva"] = "Aceite de oliva"
        };

        var existingIngredients = await dbContext.Ingredients
            .Include(ingredient => ingredient.UnitConversions)
            .ToListAsync(cancellationToken);
        var normalized = existingIngredients
            .GroupBy(ingredient => NormalizeName(ingredient.Name), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);

        foreach (var (normalizedName, displayName) in pantryIngredients)
        {
            if (normalized.TryGetValue(normalizedName, out var matchingIngredients))
            {
                foreach (var ingredient in matchingIngredients)
                {
                    ingredient.Update(ingredient.Name, null, true);
                }

                continue;
            }

            dbContext.Ingredients.Add(new Ingredient(
                displayName,
                null,
                $"images/ingredients/{normalizedName.Replace(' ', '-')}.jpg",
                true));
        }

        await dbContext.SaveChangesAsync(cancellationToken);

        var binaryRecipeIngredients = await dbContext.RecipeIngredients
            .Include(recipeIngredient => recipeIngredient.Ingredient)
            .Where(recipeIngredient => recipeIngredient.Ingredient.IsBinary)
            .ToListAsync(cancellationToken);

        foreach (var recipeIngredient in binaryRecipeIngredients)
        {
            recipeIngredient.SetQuantity(null);
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task EnsureDefaultConversionsAsync(
        MealPreparatorDbContext dbContext,
        CancellationToken cancellationToken)
    {
        const string source = "https://fdc.nal.usda.gov/food-search/";
        var defaults = new Dictionary<string, IngredientUnitConversion[]>(StringComparer.OrdinalIgnoreCase)
        {
            ["Arroz"] =
            [
                new(UnitOfMeasure.Tablespoon, 12m, source, "Arroz seco; el peso cambia si está cocido o según el tipo de grano."),
                new(UnitOfMeasure.Teaspoon, 4m, source, "Arroz seco; el peso cambia si está cocido o según el tipo de grano."),
                new(UnitOfMeasure.Cup, 185m, source, "Arroz seco, taza de referencia; el peso cambia si está cocido.")
            ],
            ["Cebolla"] = [new(UnitOfMeasure.Gram, 1m / 110m, source, "Una cebolla mediana se estima en 110 g comestibles.")],
            ["Morron"] = [new(UnitOfMeasure.Gram, 1m / 119m, source, "Un morrón mediano se estima en 119 g comestibles.")],
            ["Tomate"] = [new(UnitOfMeasure.Piece, 123m, source, "Un tomate mediano se estima en 123 g comestibles.")],
            ["Pollo"] = [new(UnitOfMeasure.Piece, 174m, source, "Una pechuga cocida deshuesada se estima en 174 g; el corte y la cocción cambian el peso.")],
            ["Manzana"] = [new(UnitOfMeasure.Gram, 1m / 182m, source, "Una manzana mediana se estima en 182 g comestibles.")],
            ["Banana"] = [new(UnitOfMeasure.Gram, 1m / 118m, source, "Una banana mediana se estima en 118 g comestibles sin cáscara.")],
            ["Naranja"] = [new(UnitOfMeasure.Gram, 1m / 131m, source, "Una naranja mediana se estima en 131 g comestibles.")],
            ["Azucar"] =
            [
                new(UnitOfMeasure.Tablespoon, 12.5m, source, "Azúcar granulada, medida rasa aproximada."),
                new(UnitOfMeasure.Teaspoon, 4.2m, source, "Azúcar granulada, medida rasa aproximada."),
                new(UnitOfMeasure.Cup, 200m, source, "Azúcar granulada, taza de referencia.")
            ],
            ["Yogur"] =
            [
                new(UnitOfMeasure.Tablespoon, 15m, source, "Yogur natural, peso aproximado por medida rasa."),
                new(UnitOfMeasure.Teaspoon, 5m, source, "Yogur natural, peso aproximado por medida rasa."),
                new(UnitOfMeasure.Cup, 245m, source, "Yogur natural, taza de referencia.")
            ]
        };

        var ingredients = await dbContext.Ingredients
            .Include(ingredient => ingredient.UnitConversions)
            .ToListAsync(cancellationToken);

        foreach (var ingredient in ingredients)
        {
            if (ingredient.IsBinary || !defaults.TryGetValue(ingredient.Name, out var conversions))
            {
                continue;
            }

            var additions = conversions
                .Where(conversion => conversion.Unit != ingredient.Unit &&
                    ingredient.UnitConversions.All(existing => existing.Unit != conversion.Unit))
                .ToList();

            if (additions.Count > 0)
            {
                ingredient.SetUnitConversions(ingredient.UnitConversions.Concat(additions));
            }
        }

        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static string NormalizeName(string name)
    {
        var decomposed = name.Trim().ToLowerInvariant().Normalize(NormalizationForm.FormD);
        var builder = new StringBuilder(decomposed.Length);
        foreach (var character in decomposed)
        {
            if (CharUnicodeInfo.GetUnicodeCategory(character) != UnicodeCategory.NonSpacingMark)
            {
                builder.Append(character);
            }
        }

        return builder.ToString().Normalize(NormalizationForm.FormC);
    }
}
