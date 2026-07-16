using Frent;
using Frent.Core;
using ImGuiNET;
using Microsoft.Xna.Framework;
using System;

namespace Paper.Core.Editor.Converters;

[BuiltInConverter]
internal class RectangleFieldConverter : ConverterAttribute<Rectangle>
{
    protected override void Display(Entity entity, ComponentID component, EditorMember<Rectangle> member)
    {
        Rectangle current = member.Value;
        Span<int> values = stackalloc int[4] { current.X, current.Y, current.Width, current.Height };
        if (ImGui.InputInt4(member.Name, ref values[0]) && !member.IsReadOnly)
            member.Value = new Rectangle(values[0], values[1], values[2], values[3]);
    }
}
