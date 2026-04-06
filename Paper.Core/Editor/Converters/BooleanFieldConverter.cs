using ImGuiNET;

namespace Paper.Core.Editor.Converters;

internal class BoolFieldConverter : FieldModifierBase<bool>
{
    protected override bool UpdateValue(ComponentField field)
    {
        bool b = _current;
        ImGui.Checkbox(field.Name, ref b);
        return b;
    }
}