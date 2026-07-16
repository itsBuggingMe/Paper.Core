using Frent;
using Frent.Core;
using ImGuiNET;
using Microsoft.Xna.Framework;
using SysVec3 = System.Numerics.Vector3;

namespace Paper.Core.Editor.Converters;

[BuiltInConverter]
internal class Vector3FieldConverter : ConverterAttribute<Vector3>
{
    protected override void Display(Entity entity, ComponentID component, EditorMember<Vector3> member)
    {
        Vector3 current = member.Value;
        SysVec3 value = new(current.X, current.Y, current.Z);
        if (ImGui.InputFloat3(member.Name, ref value) && !member.IsReadOnly)
            member.Value = new Vector3(value.X, value.Y, value.Z);
    }
}
