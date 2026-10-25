using MealPreparator.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using Microsoft.EntityFrameworkCore.Metadata;

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
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(ingredient => ingredient.IsBinary)
            .IsRequired();

        builder.Property(ingredient => ingredient.ImageUrl)
            .HasMaxLength(500);

        var conversionsNavigation = builder.Metadata.FindNavigation(nameof(Ingredient.UnitConversions));
        if (conversionsNavigation is not null)
        {
            conversionsNavigation.SetField("_unitConversions");
            conversionsNavigation.SetPropertyAccessMode(PropertyAccessMode.Field);
        }
    }
}
