// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using System.Data;

namespace CommunityToolkit.WinUI.Controls;

/// <summary>
/// A panel that arranges its children in equal columns.
/// </summary>
public partial class EqualPanel : Panel
{
    private double _maxOffAxis = 0;
    private double _reservedSize = 0;
    private double _totalPortions = 0;
    private int _visibleItemsCount = 0;

    /// <summary>
    /// Identifies the <see cref="Spacing"/> dependency property.
    /// </summary>
    /// <returns>The identifier for the <see cref="Spacing"/> dependency property.</returns>
    public static readonly DependencyProperty SpacingProperty = DependencyProperty.Register(
        nameof(Spacing),
        typeof(double),
        typeof(EqualPanel),
        new PropertyMetadata(default(double), OnPropertyChanged));

    /// <summary>
    /// Backing <see cref="DependencyProperty"/> for the <see cref="Orientation"/> property.
    /// </summary>
    public static readonly DependencyProperty OrientationProperty = DependencyProperty.Register(
        nameof(Orientation),
        typeof(Orientation),
        typeof(EqualPanel),
        new PropertyMetadata(default(Orientation), OnPropertyChanged));

    /// <summary>
    /// An attached property for identifying the proportional factor of the panel for a child to consume.
    /// </summary>
    public static readonly DependencyProperty FactorProperty =
        DependencyProperty.RegisterAttached(
            "Factor",
            typeof(GridLength),
            typeof(EqualPanel),
            new PropertyMetadata(new GridLength(1, GridUnitType.Star)));

    /// <summary>
    /// Creates a new instance of the <see cref="EqualPanel"/> class.
    /// </summary>
    public EqualPanel()
    {
        RegisterPropertyChangedCallback(HorizontalAlignmentProperty, OnAlignmentChanged);
    }

    /// <summary>
    /// Gets or sets the spacing between items.
    /// </summary>
    public double Spacing
    {
        get => (double)GetValue(SpacingProperty);
        set => SetValue(SpacingProperty, value);
    }

    /// <summary>
    /// Gets or sets the panel orientation.
    /// </summary>
    public Orientation Orientation
    {
        get => (Orientation)GetValue(OrientationProperty);
        set => SetValue(OrientationProperty, value);
    }

    /// <summary>
    /// Gets the proportional size of item in the <see cref="EqualPanel"/>.
    /// </summary>
    public static GridLength GetFactor(DependencyObject obj) => (GridLength)obj.GetValue(FactorProperty);

    /// <summary>
    /// Sets the proportional size of item in the <see cref="EqualPanel"/>.
    /// </summary>
    public static void SetFactor(DependencyObject obj, GridLength value) => obj.SetValue(FactorProperty, value);

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        _maxOffAxis = 0;
        _reservedSize = 0;
        _totalPortions = 0;

        var elements = Children.Where(static e => e.Visibility == Visibility.Visible);
        _visibleItemsCount = elements.Count();

        double portionSize = 0;
        foreach (var child in elements)
        {
            child.Measure(availableSize);

            // Get desired sizes in UV coordinates
            double desiredU, desiredV;
            (desiredU, desiredV) = Orientation switch
            {
                Orientation.Horizontal => (child.DesiredSize.Width, child.DesiredSize.Height),
                Orientation.Vertical or _ => (child.DesiredSize.Height, child.DesiredSize.Width),
            };

            // Track V axis max
            _maxOffAxis = Math.Max(_maxOffAxis, desiredV);

            // Adjust proportions according to U axis
            var factor = (GridLength)child.GetValue(FactorProperty);

            switch (factor.GridUnitType)
            {
                case GridUnitType.Auto:
                    _reservedSize += desiredU;
                    break;
                case GridUnitType.Pixel:
                    _reservedSize += factor.Value;
                    break;
                case GridUnitType.Star:
                    var itemPortions = factor.Value;
                    portionSize = Math.Max(portionSize, desiredU / itemPortions);
                    _totalPortions += itemPortions;
                    break;
            }
        }

        // Do nothing if the panel is empty
        if (_visibleItemsCount <= 0)
            return new Size(0, 0);

        // Determine if the desired alignment is stretched.
        // Don't stretch if infinite space is available though. Attempting to divide infinite space will result in a crash.
        bool stretch = Orientation switch
        {
            Orientation.Horizontal => HorizontalAlignment is HorizontalAlignment.Stretch && !double.IsInfinity(availableSize.Width),
            Orientation.Vertical or _ => VerticalAlignment is VerticalAlignment.Stretch && !double.IsInfinity(availableSize.Height),
        };

        // Define XY coords
        double xSize = 0, ySize = 0;

        // Define UV coords for orientation agnostic XY manipulation
        var size = new UVCoord(ref xSize, ref ySize, Orientation);
        double availableU = Orientation is Orientation.Horizontal ? availableSize.Width : availableSize.Height;

        if (stretch)
        {
            // Set uSize/vSize for XY result construction
            size.U = availableU;
            size.V = _maxOffAxis;
        }
        else
        {
            size.U = (portionSize * _totalPortions) + _reservedSize + (Spacing * (_visibleItemsCount - 1));
            size.V = _maxOffAxis;
        }

        return new Size(xSize, ySize);
    }

    /// <inheritdoc/>
    protected override Size ArrangeOverride(Size finalSize)
    {
        // Define X/Y coordinate variables
        double x = 0;
        double y = 0;
        double width = 0;
        double height = 0;

        // Define UV axis
        var pos = new UVCoord(ref x, ref y, Orientation);
        var size = new UVCoord(ref width, ref height, Orientation);
        double finalSizeU = Orientation is Orientation.Horizontal ? finalSize.Width : finalSize.Height;

        // Determine the size of a portion within the final size
        var spacingTotalSize = Spacing * (_visibleItemsCount - 1);
        var portionSize = (finalSizeU - spacingTotalSize - _reservedSize) / _totalPortions;
        size.V = _maxOffAxis;

        var elements = Children.Where(static e => e.Visibility == Visibility.Visible);
        foreach (var child in elements)
        {
            var factor = (GridLength)child.GetValue(FactorProperty);

            switch (factor.GridUnitType)
            {
                case GridUnitType.Auto:
                    double desiredWidth = child.DesiredSize.Width, desiredHeight = child.DesiredSize.Height;
                    var desiredSize = new UVCoord(ref desiredWidth, ref desiredHeight, Orientation);
                    size.U = desiredSize.U;
                    break;
                case GridUnitType.Pixel:
                    size.U = factor.Value;
                    break;
                case GridUnitType.Star:
                    size.U = factor.Value * portionSize;
                    break;
            }

            // NOTE: The arrange method is still in X/Y coordinate system
            child.Arrange(new Rect(x, y, width, height));
            pos.U += size.U + Spacing;
        }
        return finalSize;
    }

    private void OnAlignmentChanged(DependencyObject sender, DependencyProperty dp)
    {
        InvalidateMeasure();
    }

    private static void OnPropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        var panel = (EqualPanel)d;
        panel.InvalidateMeasure();
    }

    /// <summary>
    /// A struct for mapping X/Y coordinates to an orientation adjusted U/V coordinate system.
    /// </summary>
    private ref struct UVCoord
    {
        private readonly bool _vertical;

        private ref double _x;
        private ref double _y;

        public UVCoord(ref double x, ref double y, Orientation orientation)
        {
            _x = ref x;
            _y = ref y;
            _vertical = orientation is Orientation.Vertical;
        }

        public ref double X => ref _x;

        public ref double Y => ref _y;

        public ref double U
        {
            get
            {
                if (_vertical)
                {
                    return ref Y;
                }
                else
                {
                    return ref X;
                }
            }
        }

        public ref double V
        {
            get
            {
                if (_vertical)
                {
                    return ref X;
                }
                else
                {
                    return ref Y;
                }
            }
        }
    }
}
