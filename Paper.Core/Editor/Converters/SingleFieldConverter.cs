using Frent;
using Frent.Core;
using ImGuiNET;

namespace Paper.Core.Editor.Converters;

[BuiltInConverter]
public class SingleFieldConverter : ConverterAttribute<float>
{
    protected override void Display(Entity entity, ComponentID component, EditorMember<float> member)
    {
        float value = member.Value;
        if (ImGui.InputFloat(member.Name, ref value) && !member.IsReadOnly)
            member.Value = value;
    }
}

public class FloatSliderAttribute(float min, float max) : ConverterAttribute<float>
{
    protected override void Display(Entity entity, ComponentID component, EditorMember<float> member)
    {
        float value = member.Value;
        if (ImGui.SliderFloat(member.Name, ref value, min, max) && !member.IsReadOnly)
            member.Value = value;
    }
}
