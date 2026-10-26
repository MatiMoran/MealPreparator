namespace MealPreparator.Application.Dtos;

public sealed record TagDto(int Id, string Name);

public sealed record TagNameRequest(string Name);
