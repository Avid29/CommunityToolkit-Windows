// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.WinUI;

/// <summary>
/// Provides attached dependency properties for the <see cref="ListViewBase"/>
/// </summary>
public static partial class ListViewExtensions
{
    /// <summary>
    /// Attached <see cref="DependencyProperty"/> for setting the container content stretch direction on the <see cref="ListViewBase"/>
    /// </summary>
    public static readonly DependencyProperty ItemContainerStretchDirectionProperty =
        DependencyProperty.RegisterAttached("ItemContainerStretchDirection", typeof(ItemContainerStretchDirection), typeof(ListViewExtensions),
            new PropertyMetadata(null, OnItemContainerStretchDirectionPropertyChanged));

    /// <summary>
    /// Gets the stretch <see cref="ItemContainerStretchDirection"/> associated with the specified <see cref="ListViewBase"/>
    /// </summary>
    /// <param name="obj">The <see cref="ListViewBase"/> to get the associated <see cref="ItemContainerStretchDirection"/> from</param>
    /// <returns>The <see cref="ItemContainerStretchDirection"/> associated with the <see cref="ListViewBase"/></returns>
    public static ItemContainerStretchDirection GetItemContainerStretchDirection(ListViewBase obj) => (ItemContainerStretchDirection)obj.GetValue(ItemContainerStretchDirectionProperty);

    /// <summary>
    /// Sets the stretch <see cref="ItemContainerStretchDirection"/> associated with the specified <see cref="ListViewBase"/>
    /// </summary>
    /// <param name="obj">The <see cref="ListViewBase"/> to associate the <see cref="ItemContainerStretchDirection"/> with</param>
    /// <param name="value">The <see cref="ItemContainerStretchDirection"/> for binding to the <see cref="ListViewBase"/></param>
    public static void SetItemContainerStretchDirection(ListViewBase obj, ItemContainerStretchDirection value) => obj.SetValue(ItemContainerStretchDirectionProperty, value);

    private static void OnItemContainerStretchDirectionPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        // NOTE: Investigate... does ContainerContentChanging actually need a subscription?
        // We subscribe to the container content changing, but then update the container
        // The container would not need to be updated when its contents changes, would it?

        if (sender is not ListViewBase listViewBase)
            return;

        // Cleanup 
        listViewBase.ContainerContentChanging -= ContainerContentChanging_StretchDirection;
        listViewBase.Unloaded -= OnListViewBaseUnloaded_StretchDirection;

        // Only resubscribe if the stretch event
        if (ItemContainerStretchDirectionProperty is null)
            return;

        // Subscribe to events
        listViewBase.ContainerContentChanging += ContainerContentChanging_StretchDirection;
        listViewBase.Unloaded += OnListViewBaseUnloaded_StretchDirection;
    }

    private static void ContainerContentChanging_StretchDirection(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        // Get directions to apply stretch override
        var stretchDirection = GetItemContainerStretchDirection(sender);
        bool stretchHorizontally = stretchDirection is ItemContainerStretchDirection.Horizontal or ItemContainerStretchDirection.Both;
        bool stretchVertically = stretchDirection is ItemContainerStretchDirection.Vertical or ItemContainerStretchDirection.Both;

        // Override horizontal content stretching if applicable
        if (stretchHorizontally)
        {
            args.ItemContainer.HorizontalContentAlignment = HorizontalAlignment.Stretch;
        }

        // Override vertical content stretching if applicable
        if (stretchVertically)
        {
            args.ItemContainer.VerticalContentAlignment = VerticalAlignment.Stretch;
        }
    }

    private static void OnListViewBaseUnloaded_StretchDirection(object sender, RoutedEventArgs e)
    {
        if (sender is not ListViewBase listViewBase)
            return;

        listViewBase.ContainerContentChanging -= ContainerContentChanging_StretchDirection;
        listViewBase.Unloaded -= OnListViewBaseUnloaded_StretchDirection;
    }
}
