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

    public async Task<IReadOnlyList<Tag>> GetAllAsync(CancellationToken cancellationToken = default) =>
        await _dbContext.Tags.OrderBy(tag => tag.Name).ToListAsync(cancellationToken);

    public Task<Tag?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
        _dbContext.Tags.SingleOrDefaultAsync(tag => tag.Id == id, cancellationToken);

    public Task<bool> ExistsByNameAsync(string name, int? excludedId = null, CancellationToken cancellationToken = default) =>
        _dbContext.Tags.AnyAsync(tag => tag.Name == name && (!excludedId.HasValue || tag.Id != excludedId.Value), cancellationToken);

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

    public async Task AddAsync(Tag tag, CancellationToken cancellationToken = default) =>
        await _dbContext.Tags.AddAsync(tag, cancellationToken);

    public void Remove(Tag tag) => _dbContext.Tags.Remove(tag);
}
