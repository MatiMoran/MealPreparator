using MealPreparator.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MealPreparator.Infrastructure.Persistence.Configurations;

public sealed class RecipeTagConfiguration : IEntityTypeConfiguration<RecipeTag>
{
    public void Configure(EntityTypeBuilder<RecipeTag> builder)
    {
        builder.ToTable("RecipeTags");
        builder.HasKey(recipeTag => new { recipeTag.RecipeId, recipeTag.TagId });

        builder.HasOne(recipeTag => recipeTag.Tag)
            .WithMany()
            .HasForeignKey(recipeTag => recipeTag.TagId)
            .OnDelete(DeleteBehavior.Cascade);

    }
}
