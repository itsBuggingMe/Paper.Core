using Frent;
using Frent.Core;
using ImGuiNET;
using System.Text;

namespace Paper.Core.Editor.Converters;

[BuiltInConverter]
internal class StringFieldConverter : ConverterAttribute<string>
{
    protected override void Display(Entity entity, ComponentID component, EditorMember<string> member)
    {
        string current = member.Value ?? string.Empty;
        int capacity = System.Math.Max(256, Encoding.UTF8.GetByteCount(current) + 1);
        byte[] buffer = new byte[capacity];
        Encoding.UTF8.GetBytes(current, buffer);

        if (ImGui.InputText(member.Name, buffer, (uint)buffer.Length) && !member.IsReadOnly)
            member.Value = Encoding.UTF8.GetString(buffer).TrimEnd('\0');
    }
}
