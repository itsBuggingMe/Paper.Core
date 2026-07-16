using Frent;
using Frent.Core;
using ImGuiNET;
using Microsoft.Xna.Framework;

namespace Paper.Core.Editor.Converters;

[BuiltInConverter]
internal class PointFieldConverter : ConverterAttribute<Point>
{
    protected override void Display(Entity entity, ComponentID component, EditorMember<Point> member)
    {
        Point value = member.Value;
        if (ImGui.InputInt2(member.Name, ref value.X) && !member.IsReadOnly)
            member.Value = value;
    }
}
