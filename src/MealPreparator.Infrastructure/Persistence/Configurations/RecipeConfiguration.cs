using MealPreparator.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MealPreparator.Infrastructure.Persistence.Configurations;

public sealed class RecipeConfiguration : IEntityTypeConfiguration<Recipe>
{
    public void Configure(EntityTypeBuilder<Recipe> builder)
    {
        builder.ToTable("Recipes");

        builder.HasKey(recipe => recipe.Id);

        builder.Property(recipe => recipe.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(recipe => recipe.Servings)
            .IsRequired();

        builder.Property(recipe => recipe.Instructions)
            .HasMaxLength(4000);

        builder.Property(recipe => recipe.ImageUrl)
            .HasMaxLength(500);

        builder.HasMany(recipe => recipe.Ingredients)
            .WithOne(recipeIngredient => recipeIngredient.Recipe)
            .HasForeignKey(recipeIngredient => recipeIngredient.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(recipe => recipe.Tags)
            .WithOne()
            .HasForeignKey(recipeTag => recipeTag.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        var ingredientsNavigation = builder.Metadata.FindNavigation(nameof(Recipe.Ingredients));

        if (ingredientsNavigation is not null)
        {
            ingredientsNavigation.SetField("_ingredients");
            ingredientsNavigation.SetPropertyAccessMode(PropertyAccessMode.Field);
        }

        var tagsNavigation = builder.Metadata.FindNavigation(nameof(Recipe.Tags));
        if (tagsNavigation is not null)
        {
            tagsNavigation.SetField("_tags");
            tagsNavigation.SetPropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
