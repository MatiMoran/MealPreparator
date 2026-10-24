using MealPreparator.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MealPreparator.Infrastructure.Persistence.Configurations;

public sealed class RecipeIngredientConfiguration : IEntityTypeConfiguration<RecipeIngredient>
{
    public void Configure(EntityTypeBuilder<RecipeIngredient> builder)
    {
        builder.ToTable("RecipeIngredients");

        builder.HasKey(recipeIngredient => new
        {
            recipeIngredient.RecipeId,
            recipeIngredient.IngredientId
        });

        builder.Property(recipeIngredient => recipeIngredient.Quantity)
            .IsRequired();

        builder.HasOne(recipeIngredient => recipeIngredient.Ingredient)
            .WithMany()
            .HasForeignKey(recipeIngredient => recipeIngredient.IngredientId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
