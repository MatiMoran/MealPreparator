using MealPreparator.Application.Abstractions;
using MealPreparator.Domain.Entities;
using MealPreparator.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MealPreparator.Infrastructure.Repositories;

public sealed class RecipeRepository : IRecipeRepository
{
    private readonly MealPreparatorDbContext _dbContext;

    public RecipeRepository(MealPreparatorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Recipe>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Recipes
            .AsNoTracking()
            .Include(recipe => recipe.Tags)
            .ThenInclude(recipeTag => recipeTag.Tag)
            .OrderBy(recipe => recipe.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<Recipe?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Recipes
            .Include(recipe => recipe.Ingredients)
            .ThenInclude(recipeIngredient => recipeIngredient.Ingredient)
            .ThenInclude(ingredient => ingredient.UnitConversions)
            .Include(recipe => recipe.Tags)
            .ThenInclude(recipeTag => recipeTag.Tag)
            .FirstOrDefaultAsync(recipe => recipe.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Recipe>> GetByIdsAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Recipes
            .AsNoTracking()
            .Include(recipe => recipe.Ingredients)
            .ThenInclude(recipeIngredient => recipeIngredient.Ingredient)
            .ThenInclude(ingredient => ingredient.UnitConversions)
            .Include(recipe => recipe.Tags)
            .ThenInclude(recipeTag => recipeTag.Tag)
            .Where(recipe => ids.Contains(recipe.Id))
            .ToListAsync(cancellationToken);
    }

    public async Task AddAsync(Recipe recipe, CancellationToken cancellationToken = default)
    {
        await _dbContext.Recipes.AddAsync(recipe, cancellationToken);
    }

    public void Remove(Recipe recipe)
    {
        _dbContext.Recipes.Remove(recipe);
    }
}
