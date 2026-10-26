using MealPreparator.Domain.Entities;

namespace MealPreparator.Application.Abstractions;

public interface ITagRepository
{
    Task<IReadOnlyList<Tag>> GetAllAsync(CancellationToken cancellationToken = default);
    Task<Tag?> GetByIdAsync(int id, CancellationToken cancellationToken = default);
    Task<bool> ExistsByNameAsync(string name, int? excludedId = null, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Tag>> GetByNamesAsync(
        IReadOnlyCollection<string> names,
        CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<Tag> tags, CancellationToken cancellationToken = default);
    Task AddAsync(Tag tag, CancellationToken cancellationToken = default);
    void Remove(Tag tag);
}
