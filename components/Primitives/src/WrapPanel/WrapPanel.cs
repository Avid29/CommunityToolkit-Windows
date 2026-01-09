// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.WinUI.Controls;

/// <summary>
/// WrapPanel is a panel that position child control vertically or horizontally based on the orientation and when max width / max height is reached a new row (in case of horizontal) or column (in case of vertical) is created to fit new controls.
/// </summary>
public partial class WrapPanel : Panel
{
    /// <summary>
    /// Gets or sets a uniform Horizontal distance (in pixels) between items when <see cref="Orientation"/> is set to Horizontal,
    /// or between columns of items when <see cref="Orientation"/> is set to Vertical.
    /// </summary>
    public double HorizontalSpacing
    {
        get { return (double)GetValue(HorizontalSpacingProperty); }
        set { SetValue(HorizontalSpacingProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="HorizontalSpacing"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty HorizontalSpacingProperty =
        DependencyProperty.Register(
            nameof(HorizontalSpacing),
            typeof(double),
            typeof(WrapPanel),
            new PropertyMetadata(0d, LayoutPropertyChanged));

    /// <summary>
    /// Gets or sets a uniform Vertical distance (in pixels) between items when <see cref="Orientation"/> is set to Vertical,
    /// or between rows of items when <see cref="Orientation"/> is set to Horizontal.
    /// </summary>
    public double VerticalSpacing
    {
        get { return (double)GetValue(VerticalSpacingProperty); }
        set { SetValue(VerticalSpacingProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="VerticalSpacing"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty VerticalSpacingProperty =
        DependencyProperty.Register(
            nameof(VerticalSpacing),
            typeof(double),
            typeof(WrapPanel),
            new PropertyMetadata(0d, LayoutPropertyChanged));

    /// <summary>
    /// Gets or sets the orientation of the WrapPanel.
    /// Horizontal means that child controls will be added horizontally until the width of the panel is reached, then a new row is added to add new child controls.
    /// Vertical means that children will be added vertically until the height of the panel is reached, then a new column is added.
    /// </summary>
    public Orientation Orientation
    {
        get { return (Orientation)GetValue(OrientationProperty); }
        set { SetValue(OrientationProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="Orientation"/> dependency property.
    /// </summary>
    public static readonly DependencyProperty OrientationProperty =
        DependencyProperty.Register(
            nameof(Orientation),
            typeof(Orientation),
            typeof(WrapPanel),
            new PropertyMetadata(Orientation.Horizontal, LayoutPropertyChanged));

    /// <summary>
    /// Gets or sets the distance between the border and its child object.
    /// </summary>
    /// <returns>
    /// The dimensions of the space between the border and its child as a Thickness value.
    /// Thickness is a structure that stores dimension values using pixel measures.
    /// </returns>
    public Thickness Padding
    {
        get { return (Thickness)GetValue(PaddingProperty); }
        set { SetValue(PaddingProperty, value); }
    }

    /// <summary>
    /// Identifies the Padding dependency property.
    /// </summary>
    /// <returns>The identifier for the <see cref="Padding"/> dependency property.</returns>
    public static readonly DependencyProperty PaddingProperty =
        DependencyProperty.Register(
            nameof(Padding),
            typeof(Thickness),
            typeof(WrapPanel),
            new PropertyMetadata(default(Thickness), LayoutPropertyChanged));

    /// <summary>
    /// Gets or sets a value indicating how to arrange child items
    /// </summary>
    /// <remarks>
    /// When the available size provided to the panel is infinite (for example,
    /// when placed in a container with Auto sizing), the last child will not be
    /// stretched. Attempting to stretch in this scenario would cause the element
    /// to expand to an infinite size and result in a runtime exception.
    /// </remarks>
    public StretchChild StretchChild
    {
        get { return (StretchChild)GetValue(StretchChildProperty); }
        set { SetValue(StretchChildProperty, value); }
    }

    /// <summary>
    /// Identifies the <see cref="StretchChild"/> dependency property.
    /// </summary>
    /// <returns>The identifier for the <see cref="StretchChild"/> dependency property.</returns>
    public static readonly DependencyProperty StretchChildProperty =
        DependencyProperty.Register(
            nameof(StretchChild),
            typeof(StretchChild),
            typeof(WrapPanel),
            new PropertyMetadata(StretchChild.None, LayoutPropertyChanged));

    private static void LayoutPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is WrapPanel wp)
        {
            wp.InvalidateMeasure();
            wp.InvalidateArrange();
        }
    }

    private readonly List<Row> _rows = [];

    /// <inheritdoc />
    protected override Size MeasureOverride(Size availableSize)
    {
        var childAvailableSize = new Size(
            availableSize.Width - Padding.Left - Padding.Right,
            availableSize.Height - Padding.Top - Padding.Bottom);
        foreach (var child in Children)
        {
            child.Measure(childAvailableSize);
        }

        var requiredSize = UpdateRows(availableSize);
        return requiredSize;
    }

    /// <inheritdoc />
    protected override Size ArrangeOverride(Size finalSize)
    {
        if ((Orientation == Orientation.Horizontal && finalSize.Width < DesiredSize.Width) ||
            (Orientation == Orientation.Vertical && finalSize.Height < DesiredSize.Height))
        {
            // We haven't received our desired size. We need to refresh the rows.
            UpdateRows(finalSize);
        }

        if (_rows.Count > 0)
        {
            // Now that we have all the data, we do the actual arrange pass
            var childIndex = 0;
            foreach (var row in _rows)
            {
                foreach (var rect in row.ChildrenRects)
                {
                    var child = Children[childIndex++];
                    while (child.Visibility == Visibility.Collapsed)
                    {
                        // Collapsed children are not added into the rows,
                        // we skip them.
                        child = Children[childIndex++];
                    }

                    UVRect finalRect = rect;
                    finalRect.VSize = row.Size.V;
                    child.Arrange(finalRect);
                }
            }
        }

        return finalSize;
    }

    private Size UpdateRows(Size availableSize)
    {
        _rows.Clear();

        var paddingStart = new UVCoord(Padding.Left, Padding.Top, Orientation);
        var paddingEnd = new UVCoord(Padding.Right, Padding.Bottom, Orientation);

        if (Children.Count == 0)
        {
            return paddingStart + paddingEnd;
        }

        var availableUVSize = new UVCoord(availableSize, Orientation);
        var uvSpacing = new UVCoord(HorizontalSpacing, VerticalSpacing, Orientation);
        var uvPosition = new UVCoord(Padding.Left, Padding.Top, Orientation);

        var currentRow = new Row([], default);
        var finalMeasure = new UVCoord(0, 0, Orientation);
        void Arrange(UIElement child, bool isLast = false)
        {
            if (child.Visibility == Visibility.Collapsed)
            {
                return; // if an item is collapsed, avoid adding the spacing
            }

            var desiredSize = new UVCoord(child.DesiredSize, Orientation);
            if ((desiredSize.U + uvPosition.U + paddingEnd.U) > availableUVSize.U || uvPosition.U >= availableUVSize.U)
            {
                // next row!
                uvPosition.U = paddingStart.U;
                uvPosition.V += currentRow.Size.V + uvSpacing.V;

                _rows.Add(currentRow);
                currentRow = new Row([], default);
            }

            // Stretch the last item to fill the available space
            // if the parent measure is not infinite
            if (isLast && !double.IsInfinity(availableUVSize.U))
            {
                desiredSize.U = availableUVSize.U - uvPosition.U;
            }

            currentRow.Add(uvPosition, desiredSize);

            // adjust the location for the next items
            uvPosition.U += desiredSize.U + uvSpacing.U;
            finalMeasure.U = Math.Max(finalMeasure.U, uvPosition.U);
        }

        var lastIndex = Children.Count - 1;
        for (var i = 0; i < lastIndex; i++)
        {
            Arrange(Children[i]);
        }

        Arrange(Children[lastIndex], StretchChild == StretchChild.Last);
        if (currentRow.ChildrenRects.Count > 0)
        {
            _rows.Add(currentRow);
        }

        if (_rows.Count == 0)
        {
            return paddingStart + paddingEnd;
        }

        // Get max V here before computing final rect
        var lastRowRect = _rows.Last().Rect;
        finalMeasure.V = lastRowRect.Position.V + lastRowRect.Size.V;
        var finalRect = finalMeasure + paddingEnd;
        return finalRect;
    }
}
