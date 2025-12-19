// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.WinUI.ConvertersRns;

/// <summary>
/// A base class for a converter class with static methods.
/// </summary>
/// <typeparam name="TFrom">The type of value to convert from.</typeparam>
/// <typeparam name="TTo">The type of value to convert to.</typeparam>
public interface IStaticConverter<TFrom, TTo>
{
    /// <summary>
    /// Apply the convert operation.
    /// </summary>
    /// <param name="value">The value to convert from</param>
    /// <returns>The converted value.</returns>
    static abstract TTo Convert(TFrom value);
}
