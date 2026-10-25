using MealPreparator.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MealPreparator.Infrastructure.Persistence.Configurations;

public sealed class IngredientConfiguration : IEntityTypeConfiguration<Ingredient>
{
    public void Configure(EntityTypeBuilder<Ingredient> builder)
    {
        builder.ToTable("Ingredients");

        builder.HasKey(ingredient => ingredient.Id);

        builder.Property(ingredient => ingredient.Name)
            .IsRequired()
            .HasMaxLength(200)
            .UseCollation("NOCASE");

        builder.HasIndex(ingredient => ingredient.Name)
            .IsUnique();

        builder.Property(ingredient => ingredient.Unit)
            .IsRequired()
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(ingredient => ingredient.ImageUrl)
            .HasMaxLength(500);
    }
}
