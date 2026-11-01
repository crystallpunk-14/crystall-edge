using System.Numerics;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;

namespace Content.Client._CE.UserInterface.Controls;

/// <summary>
/// What happens to column tracks left without children, mirroring CSS Grid <c>repeat(auto-fill | auto-fit, ...)</c>.
/// </summary>
public enum CEAutoGridMode : byte
{
    /// <summary>
    /// <c>auto-fill</c>: every column that fits is kept, empty ones still take space.
    /// </summary>
    Fill,

    /// <summary>
    /// <c>auto-fit</c>: empty columns collapse, so a short row of children can stretch over the whole width.
    /// </summary>
    Fit,
}

/// <summary>
/// A grid that picks its column count from the width it is given, like CSS Grid
/// <c>grid-template-columns: repeat(auto-fill | auto-fit, minmax(MinColumnWidth, 1fr))</c>:
/// as many columns as fit without overflowing (at least one), wrapping the rest of the children into new rows.
/// </summary>
/// <remarks>
/// The column minimum is never below the widest child, so children are not squeezed under their own size.
/// When the width is unbounded (e.g. inside a horizontal scroll) the grid falls back to <see cref="MaxColumns"/>
/// columns, or a single column when unset - as CSS repeats the track list once without a definite size.
/// </remarks>
public sealed class CEAutoGridContainer : Container
{
    private float _minColumnWidth;
    private int? _maxColumns;
    private CEAutoGridMode _mode = CEAutoGridMode.Fill;
    private bool _stretchColumns;
    private BoxContainer.AlignMode _justify = BoxContainer.AlignMode.Begin;
    private float _hSeparation = 4;
    private float _vSeparation = 4;

    /// <summary>
    /// The lower bound of a column width, the <c>min</c> of <c>minmax(min, 1fr)</c>.
    /// </summary>
    public float MinColumnWidth
    {
        get => _minColumnWidth;
        set
        {
            _minColumnWidth = Math.Max(0, value);
            InvalidateMeasure();
        }
    }

    /// <summary>
    /// Caps the column count however wide the grid gets; null for no cap.
    /// </summary>
    public int? MaxColumns
    {
        get => _maxColumns;
        set
        {
            _maxColumns = value is { } max ? Math.Max(1, max) : null;
            InvalidateMeasure();
        }
    }

    public CEAutoGridMode Mode
    {
        get => _mode;
        set
        {
            _mode = value;
            InvalidateMeasure();
        }
    }

    /// <summary>
    /// Whether columns share all the width equally (the <c>1fr</c> max) instead of staying at their minimum.
    /// </summary>
    public bool StretchColumns
    {
        get => _stretchColumns;
        set
        {
            _stretchColumns = value;
            InvalidateMeasure();
        }
    }

    /// <summary>
    /// Where non-stretched columns sit when they leave free width, like CSS <c>justify-content</c>.
    /// </summary>
    public BoxContainer.AlignMode Justify
    {
        get => _justify;
        set
        {
            _justify = value;
            InvalidateArrange();
        }
    }

    /// <summary>
    /// The gap between columns, like CSS <c>column-gap</c>.
    /// </summary>
    public float HSeparation
    {
        get => _hSeparation;
        set
        {
            _hSeparation = Math.Max(0, value);
            InvalidateMeasure();
        }
    }

    /// <summary>
    /// The gap between rows, like CSS <c>row-gap</c>.
    /// </summary>
    public float VSeparation
    {
        get => _vSeparation;
        set
        {
            _vSeparation = Math.Max(0, value);
            InvalidateMeasure();
        }
    }

    protected override Vector2 MeasureOverride(Vector2 availableSize)
    {
        // First pass finds the widest child: the column can't be narrower than its content.
        var visible = 0;
        var widestChild = 0f;
        foreach (var child in Children)
        {
            if (!child.Visible)
                continue;

            child.Measure(availableSize);
            widestChild = Math.Max(widestChild, child.DesiredSize.X);
            visible++;
        }

        if (visible == 0)
            return Vector2.Zero;

        var minWidth = Math.Max(_minColumnWidth, widestChild);
        var columns = GetColumns(availableSize.X, minWidth, visible);
        var columnWidth = GetColumnWidth(availableSize.X, minWidth, columns);

        // Second pass lets children lay out against the real column width, e.g. for wrapping text.
        var height = 0f;
        var rowHeight = 0f;
        var index = 0;
        foreach (var child in Children)
        {
            if (!child.Visible)
                continue;

            child.Measure(new Vector2(columnWidth, float.PositiveInfinity));
            rowHeight = Math.Max(rowHeight, child.DesiredSize.Y);

            if (++index % columns != 0 && index != visible)
                continue;

            height += rowHeight + (height > 0 ? _vSeparation : 0);
            rowHeight = 0;
        }

        var width = minWidth * columns + _hSeparation * (columns - 1);
        return new Vector2(width, height);
    }

    protected override Vector2 ArrangeOverride(Vector2 finalSize)
    {
        var visible = 0;
        var widestChild = 0f;
        foreach (var child in Children)
        {
            if (!child.Visible)
                continue;

            widestChild = Math.Max(widestChild, child.DesiredSize.X);
            visible++;
        }

        if (visible == 0)
            return finalSize;

        var minWidth = Math.Max(_minColumnWidth, widestChild);
        var columns = GetColumns(finalSize.X, minWidth, visible);
        var columnWidth = GetColumnWidth(finalSize.X, minWidth, columns);

        var usedWidth = columnWidth * columns + _hSeparation * (columns - 1);
        var free = Math.Max(0, finalSize.X - usedWidth);
        var startX = _justify switch
        {
            BoxContainer.AlignMode.Center => free / 2,
            BoxContainer.AlignMode.End => free,
            _ => 0f,
        };

        var y = 0f;
        var rowStart = 0;
        var children = new List<Control>(visible);
        foreach (var child in Children)
        {
            if (child.Visible)
                children.Add(child);
        }

        while (rowStart < children.Count)
        {
            var rowEnd = Math.Min(rowStart + columns, children.Count);

            var rowHeight = 0f;
            for (var i = rowStart; i < rowEnd; i++)
            {
                children[i].Measure(new Vector2(columnWidth, float.PositiveInfinity));
                rowHeight = Math.Max(rowHeight, children[i].DesiredSize.Y);
            }

            for (var i = rowStart; i < rowEnd; i++)
            {
                var x = startX + (i - rowStart) * (columnWidth + _hSeparation);
                children[i].Arrange(UIBox2.FromDimensions(new Vector2(x, y), new Vector2(columnWidth, rowHeight)));
            }

            y += rowHeight + _vSeparation;
            rowStart = rowEnd;
        }

        return finalSize;
    }

    /// <summary>
    /// The CSS auto-repeat count: the most columns of <paramref name="minWidth"/> plus gaps that fit, at least one.
    /// </summary>
    private int GetColumns(float width, float minWidth, int visible)
    {
        int columns;
        if (float.IsFinite(width))
        {
            // Floored like browsers do, so zero-width children don't divide by zero.
            var track = Math.Max(1f, minWidth) + _hSeparation;
            columns = Math.Max(1, (int) ((width + _hSeparation) / track));
        }
        else
        {
            columns = _maxColumns ?? 1;
        }

        if (_maxColumns is { } max)
            columns = Math.Min(columns, max);

        if (_mode == CEAutoGridMode.Fit)
            columns = Math.Min(columns, visible);

        return columns;
    }

    private float GetColumnWidth(float width, float minWidth, int columns)
    {
        if (!_stretchColumns || !float.IsFinite(width))
            return minWidth;

        var shared = (width - _hSeparation * (columns - 1)) / columns;
        return Math.Max(minWidth, shared);
    }
}
