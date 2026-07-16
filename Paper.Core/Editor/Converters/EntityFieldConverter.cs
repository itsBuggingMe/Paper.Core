using Frent;
using Frent.Core;
using Frent.Marshalling;
using ImGuiNET;

namespace Paper.Core.Editor.Converters;

[BuiltInConverter]
internal class EntityFieldConverter : ConverterAttribute<Entity>
{
    protected override void Display(Entity entity, ComponentID component, EditorMember<Entity> member)
    {
        Entity current = member.Value;
        int entityId = current.IsAlive ? EntityMarshal.EntityID(current) : 0;
        if (!ImGui.InputInt(member.Name, ref entityId, 0, 0) || member.IsReadOnly)
            return;

        foreach (Entity potentialTarget in entity.World.CreateQuery().Build().EnumerateWithEntities())
        {
            if (EntityMarshal.EntityID(potentialTarget) == entityId)
            {
                member.Value = potentialTarget;
                return;
            }
        }
    }
}
