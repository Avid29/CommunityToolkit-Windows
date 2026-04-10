// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

using CommunityToolkit.WinUI.ConvertersRns;

namespace CommunityToolkit.WinUI.Converters;

/// <summary>
/// Converts a file size in bytes to a more human-readable friendly format using <see cref="CommunityToolkit.Common.Converters.ToFileSizeString(long)"/>
/// </summary>
public partial class FileSizeToFriendlyStringConverter : IValueConverter, IStaticConverter<long, string>
{
    /// <inheritdoc/>
    public static string Convert(long value) => CommunityToolkit.Common.Converters.ToFileSizeString(value);

    /// <inheritdoc/>
    public object Convert(object value, Type targetType, object parameter, string language)
    {
        if (value is long size)
        {
            return Convert(size);
        }

        return string.Empty;
    }

    /// <inheritdoc/>
    public object ConvertBack(object value, Type targetType, object parameter, string language)
    {
        throw new NotImplementedException();
    }
}
