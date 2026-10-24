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

        builder.HasMany(recipe => recipe.Ingredients)
            .WithOne(recipeIngredient => recipeIngredient.Recipe)
            .HasForeignKey(recipeIngredient => recipeIngredient.RecipeId)
            .OnDelete(DeleteBehavior.Cascade);

        var ingredientsNavigation = builder.Metadata.FindNavigation(nameof(Recipe.Ingredients));

        if (ingredientsNavigation is not null)
        {
            ingredientsNavigation.SetField("_ingredients");
            ingredientsNavigation.SetPropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
