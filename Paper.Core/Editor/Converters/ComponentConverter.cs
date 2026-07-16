using Frent;
using Frent.Core;
using ImGuiNET;

namespace Paper.Core.Editor.Converters;

public class ComponentConverter<T>(EditorMember[] componentMembers) : ConverterAttribute<T>
{
    protected override void Display(Entity entity, ComponentID component, EditorMember<T> member)
    {
        T? containingComponent = member.Value;
        ImGui.Indent();
        foreach (EditorMember componentMember in componentMembers)
        {
            componentMember.Initialize(containingComponent);
            ImGui.PushID(componentMember.PositionalHash);
            componentMember.Converter.CallDisplay(entity, component, componentMember);
            ImGui.PopID();
            containingComponent = componentMember.GetContainingValue<T>();
        }
        ImGui.Unindent();

        if (!member.IsReadOnly)
            member.Value = containingComponent;
    }
}
