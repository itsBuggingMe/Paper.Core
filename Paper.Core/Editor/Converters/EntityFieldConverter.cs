using Frent;
using Frent.Marshalling;
using ImGuiNET;
using Microsoft.Xna.Framework;

namespace Paper.Core.Editor.Converters;

internal class EntityFieldConverter : FieldModifierBase<Entity>
{
    protected override Entity UpdateValue(ComponentField field)
    {
        ImGui.Text(EntityMarshal.EntityID(_current).ToString());
        return _current;
    }
}