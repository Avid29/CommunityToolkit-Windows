// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
// See the LICENSE file in the project root for more information.

namespace CommunityToolkit.WinUI.Controls;

/// <summary>
/// WrapPanel is a panel that position child control vertically or horizontally based on the orientation and when max width/ max height is received a new row(in case of horizontal) or column (in case of vertical) is created to fit new controls.
/// </summary>
public partial class WrapPanel
{
    private struct Row
    {
        public Row(List<UVRect> childrenRects, UVCoord size)
        {
            ChildrenRects = childrenRects;
            Size = size;
        }

        public List<UVRect> ChildrenRects { get; }

        public UVCoord Size { get; set; }

        public UVRect Rect
        {
            get
            {
                if (ChildrenRects.Count is 0)
                {
                    return new UVRect(new Point(0, 0), (Size)Size, Size.Orientation);
                }

                return new UVRect(ChildrenRects[0].Position, (Size)Size, Size.Orientation);            
            }
        }

        public void Add(UVCoord position, UVCoord size)
        {
            ChildrenRects.Add(new UVRect(position, (Size)size, position.Orientation));
            Size = new UVCoord(position.Orientation)
            {
                U = position.U + size.U,
                V = Math.Max(Size.V, size.V),
            };
        }
    }
}
