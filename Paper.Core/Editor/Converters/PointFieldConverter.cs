using ImGuiNET;
using Microsoft.Xna.Framework;

namespace Paper.Core.Editor.Converters;

internal class PointFieldConverter : FieldModifierBase<Point>
{
    protected override Point UpdateValue(ComponentField field)
    {
        ImGui.InputInt2(field.Name, ref _current.X);
        return _current;
    }
}