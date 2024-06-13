using System.ComponentModel;

namespace Microsoft.FluentUI.AspNetCore.Components.Enums;

/// <summary>
/// Specifies the comparison operator of a <see cref="ColumnBase{TGridItem}.CurrentFilterValue"/>.
/// </summary>
public enum FilterOperator
{
    /// <summary>
    /// Satisfied if the current value equals the specified value.
    /// </summary>
    [Description("{0} == {1}"), Compatible(CompatibilityClass.All)]
    Equals,

    /// <summary>
    /// Satisfied if the current value does not equal the specified value.
    /// </summary>
    [Description("{0} != {1}"), Compatible(CompatibilityClass.All)]
    NotEquals,

    /// <summary>
    /// Satisfied if the current value is less than the specified value.
    /// </summary>
    [Description("{0} < {1}"), Compatible(CompatibilityClass.Numeric | CompatibilityClass.Date)]
    LessThan,

    /// <summary>
    /// Satisfied if the current value is less than or equal to the specified value.
    /// </summary>
    [Description("{0} <= {1}"), Compatible(CompatibilityClass.Numeric | CompatibilityClass.Date)]
    LessThanOrEquals,

    /// <summary>
    /// Satisfied if the current value is greater than the specified value.
    /// </summary>
    [Description("{0} > {1}"), Compatible(CompatibilityClass.Numeric | CompatibilityClass.Date)]
    GreaterThan,

    /// <summary>
    /// Satisfied if the current value is greater than or equal to the specified value.
    /// </summary>
    [Description("{0} >= {1}"), Compatible(CompatibilityClass.Numeric | CompatibilityClass.Date)]
    GreaterThanOrEquals,

    /// <summary>
    /// Satisfied if the current value contains the specified value.
    /// </summary>
    [Description("{0}.Contains({1})"), Compatible(CompatibilityClass.String)]
    Contains,

    /// <summary>
    /// Satisfied if the current value starts with the specified value.
    /// </summary>
    [Description("{0}.StartsWith({1})"), Compatible(CompatibilityClass.String)]
    StartsWith,

    /// <summary>
    /// Satisfied if the current value ends with the specified value.
    /// </summary>
    [Description("{0}.EndsWith({1})"), Compatible(CompatibilityClass.String)]
    EndsWith,

    /// <summary>
    /// Satisfied if the current value does not contain the specified value.
    /// </summary>
    [Description("!({0}.Contains({1}))"), Compatible(CompatibilityClass.String)]
    DoesNotContain,

    /// <summary>
    /// Satisfied if the current value is null.
    /// </summary>
    [Description("{0} is null"), Compatible(CompatibilityClass.All)]
    IsNull,

    /// <summary>
    /// Satisfied if the current value is not null.
    /// </summary>
    [Description("{0} is not null"), Compatible(CompatibilityClass.All)]
    IsNotNull,

    /// <summary>
    /// Satisfied if the current value is in the specified value.
    /// </summary>
    //In,

    /// <summary>
    /// Satisfied if the current value is not in the specified value.
    /// </summary>
    //NotIn,

    /// <summary>
    /// Satisfied if the current value is in the specified range of values, extremes excluded.
    /// </summary>
    [Description("{0} > {1} && {0} < {2}"), Compatible(CompatibilityClass.DateRange)]
    Between,

    /// <summary>
    /// Satisfied if the current value is in the specified range of values, extremes included.
    /// </summary>
    [Description("{0} >= {1} && {0} <= {2}"), Compatible(CompatibilityClass.DateRange)]
    BetweenIncluding,
}

/// <summary>
/// Specifies a set of <see cref="CompatibilityClass"/>es that a <see cref="FilterOperator"/> can be used with.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class CompatibleAttribute(CompatibilityClass types) : Attribute
{
    public CompatibilityClass Types { get; private init; } = types;
}

/// <summary>
/// Represents a class of types that a filter operator can be compatible with.
/// </summary>
public enum CompatibilityClass
{
    String = 1,
    Numeric = 2,
    Boolean = 4,
    Date = 8,
    DateRange = 16,
    All = 255
}
