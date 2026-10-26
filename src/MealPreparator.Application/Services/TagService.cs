using MealPreparator.Application.Abstractions;
using MealPreparator.Application.Dtos;
using MealPreparator.Application.Exceptions;
using MealPreparator.Application.Validation;
using MealPreparator.Domain.Entities;

namespace MealPreparator.Application.Services;

public sealed class TagService
{
    private readonly ITagRepository _tags;
    private readonly IUnitOfWork _unitOfWork;

    public TagService(ITagRepository tags, IUnitOfWork unitOfWork)
    {
        _tags = tags;
        _unitOfWork = unitOfWork;
    }

    public async Task<IReadOnlyList<TagDto>> GetAllAsync(CancellationToken cancellationToken = default) =>
        (await _tags.GetAllAsync(cancellationToken)).Select(tag => new TagDto(tag.Id, tag.Name)).ToList();

    public async Task<TagDto> CreateAsync(TagNameRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var name = RequestValidator.RequireName(request.Name, nameof(request.Name));
        ValidateLength(name);
        if (await _tags.ExistsByNameAsync(name, cancellationToken: cancellationToken))
        {
            throw new ConflictException($"A tag named '{name}' already exists.");
        }

        var tag = new Tag(name);
        await _tags.AddAsync(tag, cancellationToken);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new TagDto(tag.Id, tag.Name);
    }

    public async Task<TagDto> UpdateAsync(int id, TagNameRequest request, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);
        var tag = await GetEntityAsync(id, cancellationToken);
        var name = RequestValidator.RequireName(request.Name, nameof(request.Name));
        ValidateLength(name);
        if (await _tags.ExistsByNameAsync(name, id, cancellationToken))
        {
            throw new ConflictException($"A tag named '{name}' already exists.");
        }

        tag.Rename(name);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
        return new TagDto(tag.Id, tag.Name);
    }

    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        var tag = await GetEntityAsync(id, cancellationToken);
        _tags.Remove(tag);
        await _unitOfWork.SaveChangesAsync(cancellationToken);
    }

    private async Task<Tag> GetEntityAsync(int id, CancellationToken cancellationToken) =>
        await _tags.GetByIdAsync(id, cancellationToken)
        ?? throw new NotFoundException($"Tag with id {id} was not found.");

    private static void ValidateLength(string name)
    {
        if (name.Length > 50)
        {
            throw new Application.Exceptions.ValidationException("Tag name cannot exceed 50 characters.");
        }
    }
}
