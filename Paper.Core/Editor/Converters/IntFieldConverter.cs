using Frent;
using Frent.Core;
using ImGuiNET;

namespace Paper.Core.Editor.Converters;

[BuiltInConverter]
public class IntConverter : ConverterAttribute<int>
{
    protected override void Display(Entity entity, ComponentID component, EditorMember<int> member)
    {
        int num = member.Value;
        if (ImGui.InputInt(member.Name, ref num) && !member.IsReadOnly)
            member.Value = num;
    }
}

public class IntSliderAttribute(int min, int max) : ConverterAttribute<int>
{
    protected override void Display(Entity entity, ComponentID component, EditorMember<int> member)
    {
        int value = member.Value;
        if (ImGui.SliderInt(member.Name, ref value, min, max) && !member.IsReadOnly)
            member.Value = value;
    }
}
