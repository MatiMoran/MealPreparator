using MealPreparator.Domain.Entities;

namespace MealPreparator.Application.Abstractions;

public interface IIngredientRepository
{
    Task<IReadOnlyList<Ingredient>> GetAllAsync(CancellationToken cancellationToken = default);

    Task<Ingredient?> GetByIdAsync(int id, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<Ingredient>> GetByIdsAsync(
        IReadOnlyCollection<int> ids,
        CancellationToken cancellationToken = default);

    Task<bool> ExistsByNameAsync(
        string name,
        int? excludeId = null,
        CancellationToken cancellationToken = default);

    Task<bool> IsUsedByRecipesAsync(int id, CancellationToken cancellationToken = default);

    Task AddAsync(Ingredient ingredient, CancellationToken cancellationToken = default);

    void Remove(Ingredient ingredient);
}
