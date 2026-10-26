using MealPreparator.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MealPreparator.Infrastructure.Persistence;

public sealed class MealPreparatorDbContext : DbContext
{
    public MealPreparatorDbContext(DbContextOptions<MealPreparatorDbContext> options)
        : base(options)
    {
    }

    public DbSet<Ingredient> Ingredients => Set<Ingredient>();

    public DbSet<Recipe> Recipes => Set<Recipe>();

    public DbSet<RecipeIngredient> RecipeIngredients => Set<RecipeIngredient>();

    public DbSet<IngredientUnitConversion> IngredientUnitConversions => Set<IngredientUnitConversion>();

    public DbSet<Tag> Tags => Set<Tag>();

    public DbSet<RecipeTag> RecipeTags => Set<RecipeTag>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(MealPreparatorDbContext).Assembly);
    }
}
