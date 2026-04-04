using Frent;
using Frent.Marshalling;
using ImGuiNET;

namespace Paper.Core.Editor.Converters;

internal class EntityFieldConverter : FieldModifierBase<Entity>
{
    protected override Entity UpdateValue(ComponentField field)
    {
        int entity = EntityMarshal.EntityID(_current);
        if(ImGui.InputInt(field.Name, ref entity, 0, 0, ImGuiInputTextFlags.None))
        {
            foreach(var potentialTarget in _current.World.CreateQuery().Build().EnumerateWithEntities())
            {
                if(EntityMarshal.EntityID(potentialTarget) == entity)
                {
                    return potentialTarget;
                }
            }
        }
        return _current;
    }
}