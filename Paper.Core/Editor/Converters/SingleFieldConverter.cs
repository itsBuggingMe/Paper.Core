using ImGuiNET;

namespace Paper.Core.Editor.Converters;

internal class SingleFieldConverter : FieldModifierBase<float>
{
    protected override float UpdateValue(ComponentField field)
    {
        float f = _current;
        ImGui.InputFloat(field.Name, ref f);
        return f;
    }
}