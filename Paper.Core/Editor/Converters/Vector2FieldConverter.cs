using Frent;
using Frent.Core;
using ImGuiNET;
using Microsoft.Xna.Framework;
using SysVec2 = System.Numerics.Vector2;

namespace Paper.Core.Editor.Converters;

[BuiltInConverter]
internal class Vector2FieldConverter : ConverterAttribute<Vector2>
{
    protected override void Display(Entity entity, ComponentID component, EditorMember<Vector2> member)
    {
        Vector2 current = member.Value;
        SysVec2 value = new(current.X, current.Y);
        if (ImGui.InputFloat2(member.Name, ref value) && !member.IsReadOnly)
            member.Value = new Vector2(value.X, value.Y);
    }
}
