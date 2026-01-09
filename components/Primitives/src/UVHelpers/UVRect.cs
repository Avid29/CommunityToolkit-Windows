// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.WinUI.Controls;

/// <summary>
/// A struct representing a rectangle in a UV adjuted coordinate space.
/// </summary>
public struct UVRect
{
    /// <summary>
    /// Initializes a new instance of the <see cref="UVRect"/> struct.
    /// </summary>
    public UVRect(double x, double y, double width, double height, Orientation orientation)
    {
        X = x;
        Y = y;
        Width = width;
        Height = height;
        Orientation = orientation;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UVRect"/> struct.
    /// </summary>
    public UVRect(Point point1, Point point2, Orientation orientation)
    {
        (double lowX, double highX) = (point1.X, point2.X);
        (double lowY, double highY) = (point1.Y, point2.Y);
        if (lowX > highX)
        {
            (lowX, highX) = (highX, lowX);
        }
        if (lowY > highY)
        {
            (lowY, highY) = (highY, lowY); 
        }

        X = lowX;
        Y = lowY;
        Width = highX - lowX;
        Height = highY - lowY;
        Orientation = orientation;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UVRect"/> struct.
    /// </summary>
    public UVRect(Point location, Size size, Orientation orientation)
    {
        X = location.X;
        Y = location.Y;
        Width = size.Width;
        Height = size.Height;
        Orientation = orientation;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="UVRect"/> struct.
    /// </summary>
    public UVRect(Rect rect, Orientation orientation) : this(rect.X, rect.Y, rect.Width, rect.Height, orientation)
    {
    }

    /// <summary>
    /// Gets or sets the X position coordinate of the <see cref="UVRect"/>.
    /// </summary>
    public double X { get; set; }

    /// <summary>
    /// Gets or sets the Y position coordinate of the <see cref="UVRect"/>.
    /// </summary>
    public double Y { get; set; }

    /// <summary>
    /// Gets or sets the width of the <see cref="UVRect"/>.
    /// </summary>
    public double Width { get; set; }

    /// <summary>
    /// Gets or sets the height of the <see cref="UVRect"/>.
    /// </summary>
    public double Height { get; set; }

    /// <summary>
    /// Gets or sets the orientation for translation between the XY and UV coordinate systems.
    /// </summary>
    public Orientation Orientation { get; set; }

    /// <summary>
    /// Gets or sets the U position coordinate of the <see cref="UVRect"/>.
    /// </summary>
    public double U
    {
        readonly get => Orientation is Orientation.Horizontal ? X : Y;
        set
        {
            if (Orientation is Orientation.Horizontal)
            {
                X = value;
            }
            else
            {
                Y = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets the V position coordinate of the <see cref="UVRect"/>.
    /// </summary>
    public double V
    {
        readonly get => Orientation is Orientation.Vertical ? X : Y;
        set
        {
            if (Orientation is Orientation.Vertical)
            {
                X = value;
            }
            else
            {
                Y = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets the size of the <see cref="UVRect"/> over the U axis.
    /// </summary>
    public double USize
    {
        readonly get => Orientation is Orientation.Horizontal ? Width : Height;
        set
        {
            if (Orientation is Orientation.Horizontal)
            {
                Width = value;
            }
            else
            {
                Height = value;
            }
        }
    }

    /// <summary>
    /// Gets or sets the size of the <see cref="UVRect"/> over the V axis.
    /// </summary>
    public double VSize
    {
        readonly get => Orientation is Orientation.Vertical ? Width : Height;
        set
        {
            if (Orientation is Orientation.Vertical)
            {
                Width = value;
            }
            else
            {
                Height = value;
            }
        }
    }

    /// <summary>
    /// Gets the position of the <see cref="UVRect"/>.
    /// </summary>
    public readonly UVCoord Position => new(X, Y, Orientation);

    /// <summary>
    /// Gets the size of the <see cref="UVRect"/>.
    /// </summary>
    public readonly UVCoord Size => new(Width, Height, Orientation);

    /// <summary>
    /// Gets the high coordinates position of the <see cref="UVRect"/>.
    /// </summary>
    public readonly UVCoord HighPosition => Position + Size;

    /// <summary>
    /// Implicitly casts a <see cref="UVRect"/> to a <see cref="Rect"/>.
    /// </summary>
    public static implicit operator Rect(UVRect uv) => new(uv.X, uv.Y, uv.Width, uv.Height);
}
