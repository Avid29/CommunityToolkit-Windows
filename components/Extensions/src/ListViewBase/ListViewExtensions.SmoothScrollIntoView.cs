// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.WinUI;

/// <summary>
/// Smooth scroll the list to bring specified item into view
/// </summary>
public static partial class ListViewExtensions
{
    /// <summary>
    /// Smooth scrolling the list to bring the specified index into view
    /// </summary>
    /// <param name="listViewBase">List to scroll</param>
    /// <param name="index">The index to bring into view. Negative indicies will be used as an offset from the end.</param>
    /// <param name="itemPlacement">Set the item placement after scrolling</param>
    /// <param name="disableAnimation">Set true to disable animation</param>
    /// <param name="scrollIfVisible">Set false to disable scrolling when the corresponding item is in view</param>
    /// <param name="additionalHorizontalOffset">Adds additional horizontal offset</param>
    /// <param name="additionalVerticalOffset">Adds additional vertical offset</param>
    /// <returns>Returns <see cref="Task"/> that completes after scrolling</returns>
    public static async Task SmoothScrollIntoViewWithIndexAsync(this ListViewBase listViewBase, int index, ScrollItemPlacement itemPlacement = ScrollItemPlacement.Default, bool disableAnimation = false, bool scrollIfVisible = true, int additionalHorizontalOffset = 0, int additionalVerticalOffset = 0)
    {
        // Clamp index to valid range and adjust negative indicies to be used as an offset from the end
        index = Math.Clamp(index, -listViewBase.Items.Count, listViewBase.Items.Count - 1);
        if (index < 0)
        {
            index += listViewBase.Items.Count;
        }

        bool isVirtualizing = false;
        double previousXOffset = 0;
        double previousYOffset = 0;

        var scrollViewer = listViewBase.FindDescendant<ScrollViewer>();
        var selectorItem = listViewBase.ContainerFromIndex(index) as SelectorItem;

        if (scrollViewer is null)
            return;

        // If selectorItem is null then the panel is virtualized.
        // So in order to get the container of the item we need to scroll to that item first and then use ContainerFromIndex
        if (selectorItem is null)
        {
            isVirtualizing = true;

            previousXOffset = scrollViewer.HorizontalOffset;
            previousYOffset = scrollViewer.VerticalOffset;

            var tcs = new TaskCompletionSource<object?>();

            void ViewChanged(object? _, ScrollViewerViewChangedEventArgs __) => tcs.TrySetResult(result: default);

            try
            {
                scrollViewer.ViewChanged += ViewChanged;
                listViewBase.ScrollIntoView(listViewBase.Items[index], ScrollIntoViewAlignment.Leading);
                await tcs.Task;
            }
            finally
            {
                scrollViewer.ViewChanged -= ViewChanged;
            }

            selectorItem = (SelectorItem)listViewBase.ContainerFromIndex(index);
        }

        var transform = selectorItem.TransformToVisual((UIElement)scrollViewer.Content);
        var position = transform.TransformPoint(new Point(0, 0));

        // Scrolling back to previous position
        if (isVirtualizing)
        {
            await scrollViewer.ChangeViewAsync(previousXOffset, previousYOffset, zoomFactor: null, disableAnimation: true);
        }

        var listViewBaseWidth = listViewBase.ActualWidth;
        var selectorItemWidth = selectorItem.ActualWidth;
        var listViewBaseHeight = listViewBase.ActualHeight;
        var selectorItemHeight = selectorItem.ActualHeight;

        // Store the previous absolute offsets of the scroll viewer
        previousXOffset = scrollViewer.HorizontalOffset;
        previousYOffset = scrollViewer.VerticalOffset;

        // Calculate min and max positions to bring the item fully into view
        var minXPosition = position.X - listViewBaseWidth + selectorItemWidth;
        var minYPosition = position.Y - listViewBaseHeight + selectorItemHeight;
        var maxXPosition = position.X;
        var maxYPosition = position.Y;

        // Declare final positions with a default of the previous offsets
        double finalXPosition = previousXOffset;
        double finalYPosition = previousYOffset;

        // If scrollIfVisible is true or the item is not fully visible, calculate new offsets
        if (scrollIfVisible || previousXOffset > maxXPosition || previousXOffset < minXPosition || previousYOffset > maxYPosition || previousYOffset < minYPosition)
        {
            switch (itemPlacement)
            {
                case ScrollItemPlacement.Default:
                    if (previousXOffset <= maxXPosition && previousXOffset >= minXPosition)
                    {
                        finalXPosition = previousXOffset + additionalHorizontalOffset;
                    }
                    else if (Math.Abs(previousXOffset - minXPosition) < Math.Abs(previousXOffset - maxXPosition))
                    {
                        finalXPosition = minXPosition + additionalHorizontalOffset;
                    }
                    else
                    {
                        finalXPosition = maxXPosition + additionalHorizontalOffset;
                    }

                    if (previousYOffset <= maxYPosition && previousYOffset >= minYPosition)
                    {
                        finalYPosition = previousYOffset + additionalVerticalOffset;
                    }
                    else if (Math.Abs(previousYOffset - minYPosition) < Math.Abs(previousYOffset - maxYPosition))
                    {
                        finalYPosition = minYPosition + additionalVerticalOffset;
                    }
                    else
                    {
                        finalYPosition = maxYPosition + additionalVerticalOffset;
                    }

                    break;

                case ScrollItemPlacement.Left:
                    finalXPosition = maxXPosition + additionalHorizontalOffset;
                    finalYPosition = previousYOffset + additionalVerticalOffset;
                    break;

                case ScrollItemPlacement.Top:
                    finalXPosition = previousXOffset + additionalHorizontalOffset;
                    finalYPosition = maxYPosition + additionalVerticalOffset;
                    break;

                case ScrollItemPlacement.Center:
                    var centerX = (listViewBaseWidth - selectorItemWidth) / 2.0;
                    var centerY = (listViewBaseHeight - selectorItemHeight) / 2.0;
                    finalXPosition = maxXPosition - centerX + additionalHorizontalOffset;
                    finalYPosition = maxYPosition - centerY + additionalVerticalOffset;
                    break;

                case ScrollItemPlacement.Right:
                    finalXPosition = minXPosition + additionalHorizontalOffset;
                    finalYPosition = previousYOffset + additionalVerticalOffset;
                    break;

                case ScrollItemPlacement.Bottom:
                    finalXPosition = previousXOffset + additionalHorizontalOffset;
                    finalYPosition = minYPosition + additionalVerticalOffset;
                    break;

                default:
                    finalXPosition = previousXOffset + additionalHorizontalOffset;
                    finalYPosition = previousYOffset + additionalVerticalOffset;
                    break;
            }
        }

        await scrollViewer.ChangeViewAsync(finalXPosition, finalYPosition, zoomFactor: null, disableAnimation);
    }

    /// <summary>
    /// Smooth scrolling the list to bring the specified data item into view
    /// </summary>
    /// <param name="listViewBase">List to scroll</param>
    /// <param name="item">The data item to bring into view</param>
    /// <param name="itemPlacement">Set the item placement after scrolling</param>
    /// <param name="disableAnimation">Set true to disable animation</param>
    /// <param name="scrollIfVisible">Set true to disable scrolling when the corresponding item is in view</param>
    /// <param name="additionalHorizontalOffset">Adds additional horizontal offset</param>
    /// <param name="additionalVerticalOffset">Adds additional vertical offset</param>
    /// <returns>Returns <see cref="Task"/> that completes after scrolling</returns>
    public static async Task SmoothScrollIntoViewWithItemAsync(this ListViewBase listViewBase, object item, ScrollItemPlacement itemPlacement = ScrollItemPlacement.Default, bool disableAnimation = false, bool scrollIfVisible = true, int additionalHorizontalOffset = 0, int additionalVerticalOffset = 0)
    {
        await SmoothScrollIntoViewWithIndexAsync(listViewBase, listViewBase.Items.IndexOf(item), itemPlacement, disableAnimation, scrollIfVisible, additionalHorizontalOffset, additionalVerticalOffset);
    }

    /// <summary>
    /// Changes the view of <see cref="ScrollViewer"/> asynchronous.
    /// </summary>
    /// <param name="scrollViewer">The scroll viewer.</param>
    /// <param name="horizontalOffset">The horizontal offset.</param>
    /// <param name="verticalOffset">The vertical offset.</param>
    /// <param name="zoomFactor">The zoom factor.</param>
    /// <param name="disableAnimation">if set to <c>true</c> disable animation.</param>
    private static async Task ChangeViewAsync(this ScrollViewer scrollViewer, double horizontalOffset, double verticalOffset, float? zoomFactor, bool disableAnimation)
    {
        // Clamp offsets to valid range
        horizontalOffset = Math.Clamp(horizontalOffset, 0, scrollViewer.ScrollableWidth);
        verticalOffset = Math.Clamp(verticalOffset, 0, scrollViewer.ScrollableHeight);

        // Ensure the offsets are not already at the requested position
        // This MUST be done to prevent deadlock. The ViewChanged event will not fire if the offsets do not change.
        if (horizontalOffset == scrollViewer.HorizontalOffset &&
            verticalOffset == scrollViewer.VerticalOffset)
            return;

        var tcs = new TaskCompletionSource<object?>();

        void ViewChanged(object? _, ScrollViewerViewChangedEventArgs e)
        {
            if (e.IsIntermediate)
            {
                return;
            }

            tcs.TrySetResult(result: default);
        }

        try
        {
            scrollViewer.ViewChanged += ViewChanged;
            scrollViewer.ChangeView(horizontalOffset, verticalOffset, zoomFactor, disableAnimation);
            await tcs.Task;
        }
        finally
        {
            scrollViewer.ViewChanged -= ViewChanged;
        }
    }
}
