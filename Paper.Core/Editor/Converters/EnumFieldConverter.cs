using ImGuiNET;
using Paper.Core.Editor;
using System;

namespace Paper.Core.Editor.Converters;

public class EnumFieldConverter<T> : FieldModifierBase<T>
    where T : struct, Enum
{
    private static readonly string[] _options = Enum.GetNames<T>();
    private static readonly T[] _values = Enum.GetValues<T>();

    protected override T UpdateValue(ComponentField field)
    {
        int current = Array.IndexOf(_values, _current);
        int initalCurrent = current;

        if (ImGui.Combo(field.Name, ref current, _options, _options.Length) && current != initalCurrent)
        {
            return _values[current];
        }

        return _current;
    }
}
