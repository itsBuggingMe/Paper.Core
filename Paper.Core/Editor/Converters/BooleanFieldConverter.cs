using Frent;
using Frent.Core;
using ImGuiNET;

namespace Paper.Core.Editor.Converters;

[BuiltInConverter]
internal class BoolFieldConverter : ConverterAttribute<bool>
{
    protected override void Display(Entity entity, ComponentID component, EditorMember<bool> member)
    {
        bool value = member.Value;
        if (ImGui.Checkbox(member.Name, ref value) && !member.IsReadOnly)
            member.Value = value;
    }
}
