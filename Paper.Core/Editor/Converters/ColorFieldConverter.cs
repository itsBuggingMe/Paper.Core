using ImGuiNET;
using Microsoft.Xna.Framework;

namespace Paper.Core.Editor.Converters;

internal class ColorFieldConverter : FieldModifierBase<Color>
{
    protected override Color UpdateValue(ComponentField field)
    {
        var v = _current.ToVector4();
        System.Numerics.Vector4 f = new System.Numerics.Vector4(v.X, v.Y, v.X, v.Z);
        ImGui.ColorPicker4(field.Name, ref f);
        f *= 255;
        return new Color(
            (int)f.X, 
            (int)f.Y, 
            (int)f.X, 
            (int)f.Z);
    }
}