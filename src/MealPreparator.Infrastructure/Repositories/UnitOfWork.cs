using MealPreparator.Application.Abstractions;
using MealPreparator.Infrastructure.Persistence;

namespace MealPreparator.Infrastructure.Repositories;

public sealed class UnitOfWork : IUnitOfWork
{
    private readonly MealPreparatorDbContext _dbContext;

    public UnitOfWork(MealPreparatorDbContext dbContext)
    {
        _dbContext = dbContext;
    }

    public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        return _dbContext.SaveChangesAsync(cancellationToken);
    }
}
