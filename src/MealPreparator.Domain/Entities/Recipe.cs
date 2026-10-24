using MealPreparator.Domain.Exceptions;

namespace MealPreparator.Domain.Entities;

public sealed class Recipe
{
    private readonly List<RecipeIngredient> _ingredients = new();

    public int Id { get; internal set; }

    public string Name { get; private set; } = string.Empty;

    public int Servings { get; private set; }

    public string? Instructions { get; private set; }

    public IReadOnlyCollection<RecipeIngredient> Ingredients => _ingredients;

    private Recipe()
    {
    }

    public Recipe(string name, int servings, string? instructions)
    {
        UpdateDetails(name, servings, instructions);
    }

    public void UpdateDetails(string name, int servings, string? instructions)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainValidationException("Recipe name is required.");
        }

        var trimmedName = name.Trim();

        if (trimmedName.Length > 200)
        {
            throw new DomainValidationException("Recipe name cannot exceed 200 characters.");
        }

        if (servings <= 0)
        {
            throw new DomainValidationException("Recipe servings must be greater than zero.");
        }

        if (instructions is not null && instructions.Length > 4000)
        {
            throw new DomainValidationException("Recipe instructions cannot exceed 4000 characters.");
        }

        Name = trimmedName;
        Servings = servings;
        Instructions = string.IsNullOrWhiteSpace(instructions) ? null : instructions.Trim();
    }

    public void ReplaceIngredients(IEnumerable<RecipeIngredient> ingredients)
    {
        ArgumentNullException.ThrowIfNull(ingredients);

        var list = ingredients.ToList();

        if (list.Count == 0)
        {
            throw new DomainValidationException("A recipe must contain at least one ingredient.");
        }

        if (list.GroupBy(ingredient => ingredient.IngredientId).Any(group => group.Count() > 1))
        {
            throw new DomainValidationException("A recipe cannot contain the same ingredient more than once.");
        }

        _ingredients.Clear();
        _ingredients.AddRange(list);
    }

    public RecipeIngredient? FindIngredient(int ingredientId)
    {
        return _ingredients.FirstOrDefault(ingredient => ingredient.IngredientId == ingredientId);
    }

    public void AddIngredient(RecipeIngredient ingredient)
    {
        ArgumentNullException.ThrowIfNull(ingredient);

        if (_ingredients.Any(existing => existing.IngredientId == ingredient.IngredientId))
        {
            throw new DomainValidationException("A recipe cannot contain the same ingredient more than once.");
        }

        _ingredients.Add(ingredient);
    }

    public void RemoveIngredient(int ingredientId)
    {
        _ingredients.RemoveAll(ingredient => ingredient.IngredientId == ingredientId);
    }
}
