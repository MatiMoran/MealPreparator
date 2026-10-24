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
            return;
        }

        await using var transaction = await dbContext.Database.BeginTransactionAsync(cancellationToken);

        var rice = new Ingredient("Arroz", UnitOfMeasure.Gram);
        var chicken = new Ingredient("Pollo", UnitOfMeasure.Gram);
        var oil = new Ingredient("Aceite", UnitOfMeasure.Milliliter);
        var salt = new Ingredient("Sal", UnitOfMeasure.Gram);
        var onion = new Ingredient("Cebolla", UnitOfMeasure.Piece);
        var bellPepper = new Ingredient("Morron", UnitOfMeasure.Piece);
        var tomato = new Ingredient("Tomate", UnitOfMeasure.Gram);
        var lettuce = new Ingredient("Lechuga", UnitOfMeasure.Piece);
        var apple = new Ingredient("Manzana", UnitOfMeasure.Piece);
        var banana = new Ingredient("Banana", UnitOfMeasure.Piece);
        var orange = new Ingredient("Naranja", UnitOfMeasure.Piece);
        var sugar = new Ingredient("Azucar", UnitOfMeasure.Gram);
        var yogurt = new Ingredient("Yogur", UnitOfMeasure.Gram);

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

        var chickenWithRice = new Recipe(
            "Arroz con pollo",
            4,
            "1. Cocinar el arroz. 2. Saltear la cebolla y el morron. 3. Agregar el pollo y cocinar hasta dorar. 4. Mezclar todo y condimentar.");

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
            "1. Cortar la fruta en cubos. 2. Mezclar con el yogur y el azucar. 3. Servir fria.");

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
}
