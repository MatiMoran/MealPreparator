using MealPreparator.Domain.Entities;

namespace MealPreparator.Application.Abstractions;

public interface IRecipeRepository
{
    Task<IReadOnlyList<Recipe>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Recipe?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Recipe>> GetByIdsAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken = default);

    Task AddAsync(Recipe recipe, CancellationToken cancellationToken = default);

    void Remove(Recipe recipe);
}
