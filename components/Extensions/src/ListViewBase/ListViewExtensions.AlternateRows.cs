// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.


namespace CommunityToolkit.WinUI;

/// <summary>
/// Provides attached dependency properties for the <see cref="ListViewBase"/>
/// </summary>
public static partial class ListViewExtensions
{
    private static readonly Dictionary<IObservableVector<object>, ListViewBase> _trackedListViews = [];

    /// <summary>
    /// Attached <see cref="DependencyProperty"/> for binding a <see cref="Brush"/> as an alternate background color to a <see cref="ListViewBase"/>
    /// </summary>
    public static readonly DependencyProperty AlternateColorProperty =
        DependencyProperty.RegisterAttached("AlternateColor", typeof(Brush), typeof(ListViewExtensions),
            new PropertyMetadata(null, OnAlternateColorPropertyChanged));

    /// <summary>
    /// Attached <see cref="DependencyProperty"/> for binding a <see cref="DataTemplate"/> as an alternate template to a <see cref="ListViewBase"/>
    /// </summary>
    public static readonly DependencyProperty AlternateItemTemplateProperty =
        DependencyProperty.RegisterAttached("AlternateItemTemplate", typeof(DataTemplate), typeof(ListViewExtensions),
            new PropertyMetadata(null, OnAlternateItemTemplatePropertyChanged));

    /// <summary>
    /// Gets the alternate <see cref="Brush"/> associated with the specified <see cref="ListViewBase"/>
    /// </summary>
    /// <param name="obj">The <see cref="ListViewBase"/> to get the associated <see cref="Brush"/> from</param>
    /// <returns>The <see cref="Brush"/> associated with the <see cref="ListViewBase"/></returns>
    public static Brush GetAlternateColor(ListViewBase obj) => (Brush)obj.GetValue(AlternateColorProperty);

    /// <summary>
    /// Sets the alternate <see cref="Brush"/> associated with the specified <see cref="DependencyObject"/>
    /// </summary>
    /// <param name="obj">The <see cref="ListViewBase"/> to associate the <see cref="Brush"/> with</param>
    /// <param name="value">The <see cref="Brush"/> for binding to the <see cref="ListViewBase"/></param>
    public static void SetAlternateColor(ListViewBase obj, Brush value) => obj.SetValue(AlternateColorProperty, value);

    /// <summary>
    /// Gets the <see cref="DataTemplate"/> associated with the specified <see cref="ListViewBase"/>
    /// </summary>
    /// <param name="obj">The <see cref="ListViewBase"/> to get the associated <see cref="DataTemplate"/> from</param>
    /// <returns>The <see cref="DataTemplate"/> associated with the <see cref="ListViewBase"/></returns>
    public static DataTemplate GetAlternateItemTemplate(ListViewBase obj) => (DataTemplate)obj.GetValue(AlternateItemTemplateProperty);

    /// <summary>
    /// Sets the <see cref="DataTemplate"/> associated with the specified <see cref="ListViewBase"/>
    /// </summary>
    /// <param name="obj">The <see cref="ListViewBase"/> to associate the <see cref="DataTemplate"/> with</param>
    /// <param name="value">The <see cref="DataTemplate"/> for binding to the <see cref="ListViewBase"/></param>
    public static void SetAlternateItemTemplate(ListViewBase obj, DataTemplate value) => obj.SetValue(AlternateItemTemplateProperty, value);

    private static void OnAlternateColorPropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is not ListViewBase listViewBase)
            return;

        // Unsubscribe from events
        listViewBase.ContainerContentChanging -= ContainerContentChanging_AltColor;
        listViewBase.Items.VectorChanged -= ItemsVectorChanged_AltColor;
        listViewBase.Unloaded -= OnListViewBaseUnloaded_AlternateRows;

        // Track the list view for lookup by items collection
        _trackedListViews[listViewBase.Items] = listViewBase;

        // If the property
        if (AlternateColorProperty is null)
            return;

        // 
        listViewBase.ContainerContentChanging += ContainerContentChanging_AltColor;
        listViewBase.Items.VectorChanged += ItemsVectorChanged_AltColor;
        listViewBase.Unloaded += OnListViewBaseUnloaded_AlternateRows;
    }

    private static void OnAlternateItemTemplatePropertyChanged(DependencyObject sender, DependencyPropertyChangedEventArgs args)
    {
        if (sender is not ListViewBase listViewBase)
            return;

        listViewBase.ContainerContentChanging -= ContainerContentChanging_AltTemplate;
        listViewBase.Unloaded -= OnListViewBaseUnloaded_AlternateRows;

        if (AlternateItemTemplateProperty == null)
            return;

        listViewBase.ContainerContentChanging += ContainerContentChanging_AltTemplate;
        listViewBase.Unloaded += OnListViewBaseUnloaded_AlternateRows;
    }

    private static void ContainerContentChanging_AltColor(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        var itemContainer = args.ItemContainer as Control;
        SetItemContainerBackground(sender, itemContainer, args.ItemIndex);
    }

    private static void ItemsVectorChanged_AltColor(IObservableVector<object> sender, IVectorChangedEventArgs args)
    {
        // If the index is at the end we can ignore
        if (args.Index == (sender.Count - 1))
            return;

        // Only need to handle Inserted and Removed
        // Any other update will not effect other items, and can be handled by a container update
        if (args.CollectionChange is not CollectionChange.ItemInserted and not CollectionChange.ItemRemoved)
            return;

        // Query item
        _trackedListViews.TryGetValue(sender, out ListViewBase? listViewBase);
        if (listViewBase == null)
            return;

        // Update all items below and including the updated item
        int startingIndex = (int)args.Index;
        for (int i = startingIndex; i < sender.Count; i++)
        {
            // Get item container or element at index
            var itemContainer = listViewBase.ContainerFromIndex(i) as Control;
            itemContainer ??= listViewBase.Items[i] as Control;

            if (itemContainer is not null)
            {
                SetItemContainerBackground(listViewBase, itemContainer, i);
            }
        }
    }

    private static void ContainerContentChanging_AltTemplate(ListViewBase sender, ContainerContentChangingEventArgs args)
    {
        var template = args.ItemIndex % 2 == 0 ? GetAlternateItemTemplate(sender) : sender.ItemTemplate;
        args.ItemContainer.ContentTemplate = template;
    }

    private static void SetItemContainerBackground(ListViewBase sender, Control itemContainer, int itemIndex)
    {
        var brush = itemIndex % 2 == 0 ? GetAlternateColor(sender) : null;
        var rootBorder = itemContainer.FindDescendant<Border>();

        itemContainer.Background = brush;
        if (rootBorder is not null)
        {
            rootBorder.Background = brush;
        }
    }

    private static void OnListViewBaseUnloaded_AlternateRows(object sender, RoutedEventArgs e)
    {
        if (sender is not ListViewBase listViewBase)
            return;

        _trackedListViews.Remove(listViewBase.Items);

        listViewBase.ContainerContentChanging -= ContainerContentChanging_AltColor;
        listViewBase.Items.VectorChanged -= ItemsVectorChanged_AltColor;
        listViewBase.ContainerContentChanging -= ContainerContentChanging_AltTemplate;
        listViewBase.Unloaded -= OnListViewBaseUnloaded_AlternateRows;
    }
}
