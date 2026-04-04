using ImGuiNET;

namespace Paper.Core.Editor.Converters;

internal class StringFieldConverter : FieldModifierBase<string>
{
    protected override string UpdateValue(ComponentField field)
    {
        string value = _current ?? string.Empty;
        byte[] buffer = System.Text.Encoding.UTF8.GetBytes(value + new string('\0', 256 - value.Length));
        if (ImGui.InputText(field.Name, buffer, (uint)buffer.Length))
        {
            value = System.Text.Encoding.UTF8.GetString(buffer).TrimEnd('\0');
        }
        return value;
    }
}
