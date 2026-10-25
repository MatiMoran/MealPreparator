using MealPreparator.Domain.Enums;
using MealPreparator.Domain.Exceptions;

namespace MealPreparator.Domain.Entities;

public sealed class Ingredient
{
    private readonly List<IngredientUnitConversion> _unitConversions = new();

    public int Id { get; internal set; }

    public string Name { get; private set; } = string.Empty;

    public UnitOfMeasure? Unit { get; private set; }

    public bool IsBinary { get; private set; }

    public string? ImageUrl { get; private set; }

    public IReadOnlyCollection<IngredientUnitConversion> UnitConversions => _unitConversions;

    private Ingredient()
    {
    }

    public Ingredient(
        string name,
        UnitOfMeasure? unit,
        string? imageUrl = null,
        bool isBinary = false,
        IEnumerable<IngredientUnitConversion>? unitConversions = null)
    {
        Update(name, unit, isBinary);
        SetImageUrl(imageUrl);
        SetUnitConversions(unitConversions ?? []);
    }

    public void Update(string name, UnitOfMeasure? unit, bool isBinary = false)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new DomainValidationException("Ingredient name is required.");
        }

        var trimmedName = name.Trim();

        if (trimmedName.Length > 200)
        {
            throw new DomainValidationException("Ingredient name cannot exceed 200 characters.");
        }

        if (!isBinary && (!unit.HasValue || unit == UnitOfMeasure.Unknown || !Enum.IsDefined(unit.Value)))
        {
            throw new DomainValidationException("Ingredient unit is not valid.");
        }

        Name = trimmedName;
        IsBinary = isBinary;
        Unit = isBinary ? null : unit;

        if (isBinary)
        {
            _unitConversions.Clear();
        }
    }

    public void SetUnitConversions(IEnumerable<IngredientUnitConversion> conversions)
    {
        ArgumentNullException.ThrowIfNull(conversions);
        var list = conversions.ToList();

        if (IsBinary && list.Count > 0)
        {
            throw new DomainValidationException("Binary ingredients cannot have unit conversions.");
        }

        if (list.Any(conversion => conversion is null))
        {
            throw new DomainValidationException("Unit conversions cannot contain null entries.");
        }

        if (list.Any(conversion => conversion.Unit == Unit))
        {
            throw new DomainValidationException("The base unit cannot also be a conversion unit.");
        }

        if (list.Select(conversion => conversion.Unit).Distinct().Count() != list.Count)
        {
            throw new DomainValidationException("Conversion units must be unique for an ingredient.");
        }

        _unitConversions.Clear();
        _unitConversions.AddRange(list);
    }

    public bool SupportsUnit(UnitOfMeasure? unit)
    {
        return !IsBinary && unit.HasValue &&
            (unit == Unit || _unitConversions.Any(conversion => conversion.Unit == unit));
    }

    public decimal ConvertToBaseQuantity(decimal quantity, UnitOfMeasure unit)
    {
        if (!SupportsUnit(unit))
        {
            throw new DomainValidationException("The selected unit has no conversion for this ingredient.");
        }

        if (unit == Unit)
        {
            return quantity;
        }

        return quantity * _unitConversions.Single(conversion => conversion.Unit == unit).Factor;
    }

    public void SetImageUrl(string? imageUrl)
    {
        if (string.IsNullOrWhiteSpace(imageUrl))
        {
            ImageUrl = null;
            return;
        }

        var trimmed = imageUrl.Trim();

        if (trimmed.Length > 500)
        {
            throw new DomainValidationException("Ingredient image URL cannot exceed 500 characters.");
        }

        ImageUrl = trimmed;
    }
}
