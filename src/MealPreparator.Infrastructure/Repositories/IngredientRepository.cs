using MealPreparator.Application.Abstractions;
using MealPreparator.Domain.Entities;
using MealPreparator.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MealPreparator.Infrastructure.Repositories;

public sealed class IngredientRepository : IIngredientRepository
{
    private readonly MealPreparatorDbContext _dbContext;

    public IngredientRepository(MealPreparatorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Ingredient>> GetAllAsync(
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Ingredients
            .AsNoTracking()
            .OrderBy(ingredient => ingredient.Name)
            .ToListAsync(cancellationToken);
    }

    public Task<Ingredient?> GetByIdAsync(int id, CancellationToken cancellationToken = default)
    {
        return _dbContext.Ingredients
            .FirstOrDefaultAsync(ingredient => ingredient.Id == id, cancellationToken);
    }

    public async Task<IReadOnlyList<Ingredient>> GetByIdsAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken = default)
    {
        return await _dbContext.Ingredients
            .Where(ingredient => ids.Contains(ingredient.Id))
            .ToListAsync(cancellationToken);
    }

    public Task<bool> ExistsByNameAsync(
        string name,
        int? excludeId = null,
        CancellationToken cancellationToken = default)
    {
        return _dbContext.Ingredients.AnyAsync(
            ingredient => ingredient.Name == name
                && (!excludeId.HasValue || ingredient.Id != excludeId.Value),
            cancellationToken);
    }

    public Task<bool> IsUsedByRecipesAsync(int id, CancellationToken cancellationToken = default)
    {
        return _dbContext.RecipeIngredients
            .AnyAsync(recipeIngredient => recipeIngredient.IngredientId == id, cancellationToken);
    }

    public async Task AddAsync(Ingredient ingredient, CancellationToken cancellationToken = default)
    {
        await _dbContext.Ingredients.AddAsync(ingredient, cancellationToken);
    }

    public void Remove(Ingredient ingredient)
    {
        _dbContext.Ingredients.Remove(ingredient);
    }
}
