using MealPreparator.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace MealPreparator.Infrastructure.Persistence.Configurations;

public sealed class IngredientUnitConversionConfiguration : IEntityTypeConfiguration<IngredientUnitConversion>
{
    public void Configure(EntityTypeBuilder<IngredientUnitConversion> builder)
    {
        builder.ToTable("IngredientUnitConversions");

        builder.HasKey(conversion => new { conversion.IngredientId, conversion.Unit });

        builder.Property(conversion => conversion.Unit)
            .HasConversion<string>()
            .HasMaxLength(32);

        builder.Property(conversion => conversion.Factor)
            .IsRequired();

        builder.Property(conversion => conversion.Source)
            .HasMaxLength(1000);

        builder.Property(conversion => conversion.Note)
            .HasMaxLength(500);

        builder.HasOne(conversion => conversion.Ingredient)
            .WithMany(ingredient => ingredient.UnitConversions)
            .HasForeignKey(conversion => conversion.IngredientId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}
