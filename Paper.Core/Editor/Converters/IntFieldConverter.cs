using ImGuiNET;

namespace Paper.Core.Editor.Converters;

internal class IntFieldConverter : FieldModifierBase<int>
{
    protected override int UpdateValue(ComponentField field)
    {
        int f = _current;
        ImGui.InputInt(field.Name, ref f);
        return f;
    }
}