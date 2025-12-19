// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.WinUI.ConvertersRns;
using Windows.UI;

namespace CommunityToolkit.WinUI.Converters;

/// <summary>
/// Gets the approximated display name for the color.
/// </summary>
public partial class ColorToDisplayNameConverter : IValueConverter, IStaticConverter<Color, string>
{
    /// <inheritdoc/>
    public static string Convert(Color value)
    {
#if WINDOWS_UWP && NET8_0_OR_GREATER
        // Windows.UI.ColorHelper not yet supported on modern uwp.
        // Following advice from Sergio0694
        return value.ToString();
#elif WINUI2
        return Windows.UI.ColorHelper.ToDisplayName(value);
#elif WINUI3
        return Microsoft.UI.ColorHelper.ToDisplayName(value);
#endif
    }

    /// <inheritdoc/>
    public object Convert(
        object value,
        Type targetType,
        object parameter,
        string language)
    {
        Color color;

        if (value is Color valueColor)
        {
            color = valueColor;
        }
        else if (value is SolidColorBrush valueBrush)
        {
            color = valueBrush.Color;
        }
        else
        {
            // Invalid color value provided
            return DependencyProperty.UnsetValue;
        }

        return Convert(color);
    }

    /// <inheritdoc/>
    public object ConvertBack(
        object value,
        Type targetType,
        object parameter,
        string language)
    {
        return DependencyProperty.UnsetValue;
    }
}
