using Frent;
using Frent.Core;
using ImGuiNET;
using Microsoft.Xna.Framework;

namespace Paper.Core.Editor.Converters;

[BuiltInConverter]
internal class ColorFieldConverter : ConverterAttribute<Color>
{
    protected override void Display(Entity entity, ComponentID component, EditorMember<Color> member)
    {
        Vector4 current = member.Value.ToVector4();
        System.Numerics.Vector4 value = new(current.X, current.Y, current.Z, current.W);
        if (ImGui.ColorEdit4(member.Name, ref value) && !member.IsReadOnly)
            member.Value = new Color(value);
    }
}
