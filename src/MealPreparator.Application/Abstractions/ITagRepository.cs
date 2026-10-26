using MealPreparator.Domain.Entities;

namespace MealPreparator.Application.Abstractions;

public interface ITagRepository
{
    Task<IReadOnlyList<Tag>> GetByNamesAsync(
        IReadOnlyCollection<string> names,
        CancellationToken cancellationToken = default);

    Task AddRangeAsync(IEnumerable<Tag> tags, CancellationToken cancellationToken = default);
}
