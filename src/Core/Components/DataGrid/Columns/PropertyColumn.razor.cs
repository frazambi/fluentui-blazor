using System.Globalization;
using System.Linq.Expressions;
using System.Reflection;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Rendering;
using Microsoft.FluentUI.AspNetCore.Components.DataGrid.Infrastructure;
using Microsoft.FluentUI.AspNetCore.Components.Enums;
using WallesCore.Helpers;
using WallesCore.Extensions;
using Microsoft.FluentUI.AspNetCore.Components.Extensions;

namespace Microsoft.FluentUI.AspNetCore.Components;

/// <summary>
/// Represents a <see cref="FluentDataGrid{TGridItem}"/> column whose cells display a single value.
/// </summary>
/// <typeparam name="TGridItem">The type of data represented by each row in the grid.</typeparam>
/// <typeparam name="TProp">The type of the value being displayed in the column's cells.</typeparam>
public partial class PropertyColumn<TGridItem, TProp> : ColumnBase<TGridItem>
{
    private Expression<Func<TGridItem, TProp>>? _lastAssignedProperty;
    private Func<TGridItem, object?>? _cellTextFunc;
    private Func<TGridItem, string?>? _cellTooltipTextFunc;
    private GridSort<TGridItem>? _sortBuilder;
    private GridSort<TGridItem>? _customSortBy;
    private bool? _isCaseSensitive;

    // current contiene l'ultimo valore che aveva il filtro quando è stato inviato. pending quello che c'è al momento della modifica dall'utente, prima dell'invio.
    // queste due variabili servono per permettere che se si chiude il filtro senza inviare, quando lo si riapre si rivedranno gli ultimi valori submittati.
    private object? _currentFilterValue;
    private object? _pendingFilterValue;

    public PropertyInfo? PropertyInfo { get; private set; }

    /// <summary>
    /// Defines the value to be displayed in this column's cells.
    /// </summary>
    [Parameter, EditorRequired] public Expression<Func<TGridItem, TProp>> Property { get; set; } = default!;

    /// <summary>
    /// Optionally specifies a format string for the value.
    ///
    /// Using this requires the <typeparamref name="TProp"/> type to implement <see cref="IFormattable" />.
    /// </summary>
    [Parameter] public string? Format { get; set; }

    /// <summary>
    /// Optionally specifies how to compare values in this column when sorting.
    ///
    /// Using this requires the <typeparamref name="TProp"/> type to implement <see cref="IComparable{T}"/>.
    /// </summary>
    [Parameter] public IComparer<TProp>? Comparer { get; set; } = null;

    /// <summary>
    /// Gets or sets a value indicating whether the column can be used to filter the data.
    ///
    /// The default value is false.
    /// </summary>
    [Parameter] public bool Filterable { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the column's value is treated as case sensitive during filtering.
    ///
    /// Defaults to <see cref="Grid.CaseSensitive"/> if set, otherwise false.
    /// </summary>
    [Parameter]
    public bool CaseSensitive
    {
        get => _isCaseSensitive ?? Grid?.CaseSensitive ?? false;
        set => _isCaseSensitive = value;
    }


    [Parameter]
    public override GridSort<TGridItem>? SortBy
    {
        get => _customSortBy ?? _sortBuilder;
        set => _customSortBy = value;
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        // We have to do a bit of pre-processing on the lambda expression. Only do that if it's new or changed.
        if (_lastAssignedProperty != Property)
        {
            _lastAssignedProperty = Property;
            var compiledPropertyExpression = Property.Compile();

            if (!string.IsNullOrEmpty(Format))
            {
                // TODO: Consider using reflection to avoid having to box every value just to call IFormattable.ToString
                // For example, define a method "string Type<U>(Func<TGridItem, U> property) where U: IFormattable", and
                // then construct the closed type here with U=TProp when we know TProp implements IFormattable

                // If the type is nullable, we're interested in formatting the underlying type
                var nullableUnderlyingTypeOrNull = Nullable.GetUnderlyingType(typeof(TProp));
                if (!typeof(IFormattable).IsAssignableFrom(nullableUnderlyingTypeOrNull ?? typeof(TProp)))
                {
                    throw new InvalidOperationException($"A '{nameof(Format)}' parameter was supplied, but the type '{typeof(TProp)}' does not implement '{typeof(IFormattable)}'.");
                }

                _cellTextFunc = item => ((IFormattable?)compiledPropertyExpression!(item))?.ToString(Format, null);
                _cellTooltipTextFunc = item => TooltipText?.Invoke(item) ?? _cellTextFunc(item)!.ToString();
            }
            else
            {
                // boolean columns render an icon instead of "true" / "false"
                if (typeof(TProp) == typeof(bool))
                {
                    _cellTextFunc = item => (RenderFragment)(builder => RenderBooleanIcon(builder, compiledPropertyExpression!(item).ChangeType<bool>()));
                }
                else
                {
                    _cellTextFunc = item => compiledPropertyExpression!(item)?.ToString();
                }
                _cellTooltipTextFunc = item => TooltipText?.Invoke(item) ?? compiledPropertyExpression!(item)?.ToString();
            }

            _sortBuilder = Comparer is not null ? GridSort<TGridItem>.ByAscending(Property, Comparer) : GridSort<TGridItem>.ByAscending(Property);
        }

        if (Property.Body is MemberExpression memberExpression)
        {
            if (Title is null)
            {
                PropertyInfo = memberExpression.Member as PropertyInfo;
                var daText = memberExpression.Member.DeclaringType?.GetDisplayAttributeString(memberExpression.Member.Name);
                if (!string.IsNullOrEmpty(daText))
                {
                    Title = daText;
                }
                else
                {
                    Title = memberExpression.Member.Name;
                }
            }
        }

        if (Filterable && ColumnOptions == null)
        {
            if (PropertyAccess.IsNumeric(typeof(TProp)))
            {
                var method = GetType().GetMethod(nameof(RenderNumericFilter), BindingFlags.NonPublic | BindingFlags.Instance)!.MakeGenericMethod(typeof(TProp));
                ColumnOptions = builder => method.Invoke(this, [builder]);
            }
            else if (typeof(TProp) == typeof(bool))
            {
                ColumnOptions = RenderBooleanFilter;
            }
            else if (PropertyAccess.IsDate(typeof(TProp)))
            {
                ColumnOptions = RenderDateFilter;
            }
            else
            {
                ColumnOptions = RenderStringFilter;
            }
        }
    }

    /// <inheritdoc />
    protected internal override void CellContent(RenderTreeBuilder builder, TGridItem item)
    {
        var renderedValue = _cellTextFunc?.Invoke(item);
        if (renderedValue is RenderFragment frag)
        {
            builder.AddContent(0, frag);
        }
        else
        {
            builder.AddContent(0, renderedValue);
        }
    }

    protected internal override string? RawCellContent(TGridItem item)
        => _cellTooltipTextFunc?.Invoke(item);

    protected override bool IsSortableByDefault()
    => _customSortBy is not null;

    public override void RemoveFilter()
    {
        _currentFilterValue = null;
        _pendingFilterValue = null;
        base.RemoveFilter();
    }

    /// <summary>
    /// Sets value to <see cref="CurrentFilter"/> based on <see cref="Property"/> and the value and operator passed.
    /// </summary>
    /// <param name="value">The value used to filter this column.</param>
    /// <param name="filterOperator">The operator used to compare each row's value for this column and <see cref="_pendingFilterValue"/>. Must be compatible with <see cref="TProp"/>, otherwise throws <see cref="ArgumentException"/>.</param>
    public Task SetFilterValueAsync<TFilter>(TFilter? value, FilterOperator filterOperator)
    {
        if (value is null)
        {
            return Task.CompletedTask;
        }

        string filterFormat = filterOperator.ToAttributeValue(false)!;
        string filterProperty = Property.Body.ToString().Substring(Property.Body.ToString().IndexOf('.') + 1);
        string[] filterValues = [];

        if (typeof(TProp) != typeof(TFilter))
        {
            // tipo filtro è DateRange, colonna non può esserlo mai
            if (value is DateRange _d)
            {
                CheckCompatibility(CompatibilityClass.DateRange);

                filterValues = [$"DateTime({_d.Start ?? DateTime.MinValue:yyyy,MM,dd})", $"DateTime({_d.End ?? DateTime.MaxValue:yyyy,MM,dd})"];
            }
            // tipo filtro è string, tipo colonna no
            else if (value is string _s)
            {
                CheckCompatibility(CompatibilityClass.String);

                filterFormat = filterFormat.Replace("{0}", CaseSensitive ? "{0}.ToString()" : "{0}.ToString().ToLower()");
                filterValues = ['"' + (CaseSensitive ? _s : _s.ToLower()) + '"'];
            }
            else
            {
                throw new InvalidOperationException("Unsupported type conversion.");
            }
        }
        else
        {
            if (typeof(TProp) == typeof(string))
            {
                CheckCompatibility(CompatibilityClass.String);

                if (CaseSensitive)
                {
                    filterValues = ['"' + value.ToString() + '"'];
                }
                else
                {
                    filterFormat = filterFormat.Replace("{0}", "{0}.ToLower()");
                    filterValues = ['"' + value.ToString()!.ToLower() + '"'];
                }
            }
            else if (PropertyAccess.IsNumeric(typeof(TProp)))
            {
                CheckCompatibility(CompatibilityClass.Numeric);

                filterValues = [Convert.ToString(value, CultureInfo.InvariantCulture)!];
            }
            else if (typeof(TProp) == typeof(bool))
            {
                CheckCompatibility(CompatibilityClass.Boolean);

                filterValues = [value.ToString()!];
            }
            else if (PropertyAccess.IsDate(typeof(TProp)))
            {
                CheckCompatibility(CompatibilityClass.Date);

                filterValues = [$"{(value as DateTime?)?.ToString("yyyy,MM,dd")}"];
            }
        }

        CurrentFilterValue = string.Format(filterFormat, [filterProperty, .. filterValues]);
        // in alcuni casi _xFilterValue può essere un tipo di riferimento, in tali casi deve implementare ICloneable per permettere una copiatura corretta
        if (_pendingFilterValue is ICloneable c)
        {
            _currentFilterValue = c.Clone();
        }
        else
        {
            _currentFilterValue = _pendingFilterValue;
        }

        // reset pagination
        if (Grid.Pagination is not null)
        {
            Grid.Pagination.CurrentPageIndex = 0;
        }

        Grid.CloseColumnOptions();

        return InternalGridContext.Grid.RefreshDataAsync();

        void CheckCompatibility(CompatibilityClass comp)
        {
            var filterOperatorCompatibilities = typeof(FilterOperator).GetField(filterOperator.ToString())!.GetCustomAttribute<CompatibleAttribute>()!.Types;
            if ((filterOperatorCompatibilities & comp) == 0)
            {
                throw new ArgumentException($"Parameter {nameof(filterOperator)} is not compatible with this column's type.");
            }
        }
    }

    /// <summary>
    /// Sets the displayed filter value to the last submitted value.
    /// </summary>
    public void ResetFilterToLastValue()
    {
        if (Filterable)
        {
            if (_currentFilterValue is ICloneable c)
            {
                _pendingFilterValue = c.Clone();
            }
            else
            {
                _pendingFilterValue = _currentFilterValue;
            }
        }
    }
}
