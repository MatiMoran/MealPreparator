namespace MealPreparator.Domain.Entities;

public sealed class RecipeTag
{
    public int RecipeId { get; private set; }

    public int TagId { get; private set; }

    public Tag Tag { get; private set; } = null!;

    private RecipeTag()
    {
    }

    public RecipeTag(Tag tag)
    {
        ArgumentNullException.ThrowIfNull(tag);
        Tag = tag;
    }
}
