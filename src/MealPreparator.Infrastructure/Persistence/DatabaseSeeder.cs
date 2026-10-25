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

        var chickenWithRice = new Recipe(
            "Arroz con pollo",
            4,
            "1. Cocinar el arroz. 2. Saltear la cebolla y el morron. 3. Agregar el pollo y cocinar hasta dorar. 4. Mezclar todo y condimentar.",
            "images/recipes/arroz-con-pollo.jpg");

        chickenWithRice.ReplaceIngredients(
        [
            new RecipeIngredient(rice, 400m),
            new RecipeIngredient(chicken, 600m),
            new RecipeIngredient(oil, 30m),
            new RecipeIngredient(salt, 5m),
            new RecipeIngredient(onion, 1m),
            new RecipeIngredient(bellPepper, 1m)
        ]);

        var fruitSalad = new Recipe(
            "Ensalada de frutas",
            2,
            "1. Cortar la fruta en cubos. 2. Mezclar con el yogur y el azucar. 3. Servir fria.",
            "images/recipes/ensalada-de-frutas.jpg");

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

        var existingIngredients = await dbContext.Ingredients.ToListAsync(cancellationToken);
        var normalized = existingIngredients
            .GroupBy(ingredient => NormalizeName(ingredient.Name), StringComparer.Ordinal)
            .ToDictionary(group => group.Key, group => group.ToList(), StringComparer.Ordinal);

        foreach (var (normalizedName, displayName) in pantryIngredients)
        {
            if (normalized.TryGetValue(normalizedName, out var matchingIngredients))
            {
                foreach (var ingredient in matchingIngredients)
                {
                    ingredient.Update(ingredient.Name, UnitOfMeasure.Binary);
                }

                continue;
            }

            dbContext.Ingredients.Add(new Ingredient(
                displayName,
                UnitOfMeasure.Binary,
                $"images/ingredients/{normalizedName.Replace(' ', '-')}.jpg"));
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
