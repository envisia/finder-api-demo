namespace FinderDemo.Models.Additionals;

/// <summary>Known values of <see cref="CategoryAdditional.ValueType"/>.</summary>
public static class AdditionalValueTypes
{
    public const string String = "string";
    public const string Decimal = "decimal";
    public const string Date = "date";
    public const string Select = "select";
    public const string Checkbox = "checkbox";

    /// <summary>Date format expected by the API for additionals of type <c>date</c>.</summary>
    public const string DateFormat = "dd.MM.yyyy";
}
