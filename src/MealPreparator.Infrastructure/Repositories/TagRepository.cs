using MealPreparator.Application.Abstractions;
using MealPreparator.Domain.Entities;
using MealPreparator.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace MealPreparator.Infrastructure.Repositories;

public sealed class TagRepository : ITagRepository
{
    private readonly MealPreparatorDbContext _dbContext;

    public TagRepository(MealPreparatorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public async Task<IReadOnlyList<Tag>> GetByNamesAsync(
        IReadOnlyCollection<string> names,
        CancellationToken cancellationToken = default)
    {
        if (names.Count == 0)
        {
            return [];
        }

        return await _dbContext.Tags
            .Where(tag => names.Contains(tag.Name))
            .ToListAsync(cancellationToken);
    }

    public async Task AddRangeAsync(IEnumerable<Tag> tags, CancellationToken cancellationToken = default)
    {
        await _dbContext.Tags.AddRangeAsync(tags, cancellationToken);
    }
}
