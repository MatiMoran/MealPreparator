using FluentAssertions;
using MealPreparator.Application.Abstractions;
using MealPreparator.Application.Dtos;
using MealPreparator.Application.Exceptions;
using MealPreparator.Application.Services;
using MealPreparator.Domain.Entities;
using Xunit;

namespace MealPreparator.UnitTests.Application;

public sealed class TagServiceTests
{
    [Fact]
    public async Task CreateAsync_ShouldTrimNameAndPersistTag()
    {
        var fixture = new Fixture();

        var created = await fixture.Service.CreateAsync(new TagNameRequest("  Pollo  "));

        created.Should().Be(new TagDto(1, "Pollo"));
        fixture.Repository.Tags.Should().ContainSingle().Which.Name.Should().Be("Pollo");
        fixture.UnitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task CreateAsync_ShouldRejectDuplicateNamesIgnoringCase()
    {
        var fixture = new Fixture();
        fixture.Repository.AddSeed("Postre");

        var action = () => fixture.Service.CreateAsync(new TagNameRequest(" postre "));

        await action.Should().ThrowAsync<ConflictException>();
        fixture.UnitOfWork.SaveCount.Should().Be(0);
    }

    [Theory]
    [InlineData(" ")]
    [InlineData("abcdefghijklmnopqrstuvwxyzabcdefghijklmnopqrstuvwxyz")]
    public async Task CreateAsync_ShouldRejectBlankOrOverlongNames(string name)
    {
        var fixture = new Fixture();

        var action = () => fixture.Service.CreateAsync(new TagNameRequest(name));

        await action.Should().ThrowAsync<MealPreparator.Application.Exceptions.ValidationException>();
    }

    [Fact]
    public async Task UpdateAsync_ShouldRenameAndAllowCurrentNameIgnoringCase()
    {
        var fixture = new Fixture();
        var tag = fixture.Repository.AddSeed("Desayuno");

        var updated = await fixture.Service.UpdateAsync(tag.Id, new TagNameRequest(" desayuno "));

        updated.Name.Should().Be("desayuno");
        fixture.UnitOfWork.SaveCount.Should().Be(1);
    }

    [Fact]
    public async Task UpdateAsync_ShouldRejectNameOwnedByAnotherTag()
    {
        var fixture = new Fixture();
        var first = fixture.Repository.AddSeed("Pollo");
        var second = fixture.Repository.AddSeed("Postre");

        var action = () => fixture.Service.UpdateAsync(second.Id, new TagNameRequest("POLLO"));

        await action.Should().ThrowAsync<ConflictException>();
        first.Name.Should().Be("Pollo");
        second.Name.Should().Be("Postre");
        fixture.UnitOfWork.SaveCount.Should().Be(0);
    }

    [Fact]
    public async Task UpdateAndDeleteAsync_ShouldThrowWhenTagDoesNotExist()
    {
        var fixture = new Fixture();

        var update = () => fixture.Service.UpdateAsync(99, new TagNameRequest("Otro"));
        var delete = () => fixture.Service.DeleteAsync(99);

        await update.Should().ThrowAsync<NotFoundException>();
        await delete.Should().ThrowAsync<NotFoundException>();
    }

    [Fact]
    public async Task DeleteAsync_ShouldRemoveTagAndPersist()
    {
        var fixture = new Fixture();
        var tag = fixture.Repository.AddSeed("Pollo");

        await fixture.Service.DeleteAsync(tag.Id);

        fixture.Repository.Tags.Should().BeEmpty();
        fixture.UnitOfWork.SaveCount.Should().Be(1);
    }

    private sealed class Fixture
    {
        public FakeTagRepository Repository { get; } = new();
        public FakeUnitOfWork UnitOfWork { get; } = new();
        public TagService Service { get; }

        public Fixture() => Service = new TagService(Repository, UnitOfWork);
    }

    private sealed class FakeTagRepository : ITagRepository
    {
        private int _nextId = 1;
        public List<Tag> Tags { get; } = [];

        public Tag AddSeed(string name)
        {
            var tag = new Tag(name) { Id = _nextId++ };
            Tags.Add(tag);
            return tag;
        }

        public Task<IReadOnlyList<Tag>> GetAllAsync(CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Tag>>(Tags);

        public Task<Tag?> GetByIdAsync(int id, CancellationToken cancellationToken = default) =>
            Task.FromResult(Tags.SingleOrDefault(tag => tag.Id == id));

        public Task<bool> ExistsByNameAsync(string name, int? excludedId = null, CancellationToken cancellationToken = default) =>
            Task.FromResult(Tags.Any(tag => tag.Id != excludedId && tag.Name.Equals(name, StringComparison.OrdinalIgnoreCase)));

        public Task<IReadOnlyList<Tag>> GetByNamesAsync(IReadOnlyCollection<string> names, CancellationToken cancellationToken = default) =>
            Task.FromResult<IReadOnlyList<Tag>>(Tags.Where(tag => names.Contains(tag.Name, StringComparer.OrdinalIgnoreCase)).ToList());

        public Task AddRangeAsync(IEnumerable<Tag> tags, CancellationToken cancellationToken = default)
        {
            foreach (var tag in tags) Add(tag);
            return Task.CompletedTask;
        }

        public Task AddAsync(Tag tag, CancellationToken cancellationToken = default)
        {
            Add(tag);
            return Task.CompletedTask;
        }

        public void Remove(Tag tag) => Tags.Remove(tag);

        private void Add(Tag tag)
        {
            tag.Id = _nextId++;
            Tags.Add(tag);
        }
    }

    private sealed class FakeUnitOfWork : IUnitOfWork
    {
        public int SaveCount { get; private set; }
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveCount++;
            return Task.FromResult(1);
        }
    }
}
