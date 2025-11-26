// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using Microsoft.UI.Xaml.Controls;
using System.Data;

namespace CommunityToolkit.WinUI.Controls;

/// <summary>
/// A panel that arranges its children in equal columns.
/// </summary>
public partial class EqualPanel : Panel
{
    private double _maxOffAxis = 0;
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
            typeof(double),
            typeof(EqualPanel),
            new PropertyMetadata(1d));

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
    public static double GetFactor(DependencyObject obj) => (double)obj.GetValue(FactorProperty);

    /// <summary>
    /// Sets the proportional size of item in the <see cref="EqualPanel"/>.
    /// </summary>
    public static void SetFactor(DependencyObject obj, double value) => obj.SetValue(FactorProperty, value);

    /// <inheritdoc/>
    protected override Size MeasureOverride(Size availableSize)
    {
        _maxOffAxis = 0;
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

            // Adjust proportions according to U axis
            var factor = (double)child.GetValue(FactorProperty);
            portionSize = Math.Max(portionSize, desiredU / factor);
            _totalPortions += factor;

            // Track V axis max
            _maxOffAxis = Math.Max(_maxOffAxis, desiredV);
        }

        // Do nothing if the panel is empty
        if (_visibleItemsCount <= 0)
            return new Size(0, 0);

        bool stretch = Orientation switch
        {
            Orientation.Horizontal => HorizontalAlignment is HorizontalAlignment.Stretch && !double.IsInfinity(availableSize.Width),
            Orientation.Vertical or _ => VerticalAlignment is VerticalAlignment.Stretch && !double.IsInfinity(availableSize.Height),
        };

        // Define XY coords
        double xSize = 0, ySize = 0;

        // Define UV coords for orientation agnostic XY manipulation
        ref double uSize = ref SelectAxis(Orientation, ref xSize, ref ySize, true);
        ref double vSize = ref SelectAxis(Orientation, ref xSize, ref ySize, false);
        double availableU = Orientation is Orientation.Horizontal ? availableSize.Width : availableSize.Height;

        if (stretch)
        {
            // Set uSize/vSize for XY result construction
            uSize = availableU;
            vSize = _maxOffAxis;
        }
        else
        {
            uSize = (portionSize * _totalPortions) + (Spacing * (_visibleItemsCount - 1));
            vSize = _maxOffAxis;
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
        ref double u = ref SelectAxis(Orientation, ref x, ref y, true);
        ref double uSize = ref SelectAxis(Orientation, ref width, ref height, true);
        ref double vSize = ref SelectAxis(Orientation, ref width, ref height, false);
        double finalSizeU = Orientation is Orientation.Horizontal ? finalSize.Width : finalSize.Height;

        // Determine the size of a portion within the final size
        var spacingTotalSize = Spacing * (_visibleItemsCount - 1);
        var portionSize = (finalSizeU - spacingTotalSize) / _totalPortions;
        vSize = _maxOffAxis;
        
        var elements = Children.Where(static e => e.Visibility == Visibility.Visible);
        foreach (var child in elements)
        {
            var factor = (double)child.GetValue(FactorProperty);
            uSize = factor * portionSize;

            // NOTE: The arrange method is still in X/Y coordinate system
            child.Arrange(new Rect(x, y, width, height));
            u += uSize + Spacing;
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

    private static ref double SelectAxis(Orientation orientation, ref double x, ref double y, bool u)
    {
        if ((orientation is Orientation.Horizontal && u) || (orientation is Orientation.Vertical && !u))
            return ref x;
        else
            return ref y;
    }
}
