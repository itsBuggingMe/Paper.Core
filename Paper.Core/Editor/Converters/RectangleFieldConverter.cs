using ImGuiNET;
using Microsoft.Xna.Framework;
using System;

namespace Paper.Core.Editor.Converters;

internal class RectangleFieldConverter : FieldModifierBase<Rectangle>
{
    protected override Rectangle UpdateValue(ComponentField field)
    {
        Span<int> vals = stackalloc int[4] { _current.X, _current.Y, _current.Width, _current.Height };
        ImGui.InputInt4(field.Name, ref vals[0]);
        return new Rectangle(vals[0], vals[1], vals[2], vals[3]);
    }
}
