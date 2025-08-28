using ImGuiNET;
using Microsoft.Xna.Framework;

namespace Paper.Core.Editor.Converters;

internal class ColorFieldConverter : FieldModifierBase<Color>
{
    protected override Color UpdateValue(ComponentField field)
    {
        System.Numerics.Vector4 f = _current.ToVector4().ToNumerics();
        ImGui.ColorPicker4(field.Name, ref f);
        return new Color(f);
    }
}