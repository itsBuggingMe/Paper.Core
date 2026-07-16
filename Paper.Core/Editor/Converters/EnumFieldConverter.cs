using Frent;
using Frent.Core;
using ImGuiNET;
using System;

namespace Paper.Core.Editor.Converters;

internal class EnumFieldConverter<T> : ConverterAttribute<T> where T : struct, Enum
{
    private static readonly string[] Options = Enum.GetNames<T>();
    private static readonly T[] Values = Enum.GetValues<T>();
    private static readonly bool IsFlags = typeof(T).IsDefined(typeof(FlagsAttribute), false);

    protected override void Display(Entity entity, ComponentID component, EditorMember<T> member)
    {
        if (IsFlags)
        {
            DisplayFlags(member);
            return;
        }

        int current = Array.IndexOf(Values, member.Value);
        if (ImGui.Combo(member.Name, ref current, Options, Options.Length) && !member.IsReadOnly)
            member.Value = Values[current];
    }

    private static void DisplayFlags(EditorMember<T> member)
    {
        T current = member.Value;
        if (!ImGui.BeginCombo(member.Name, current.ToString()))
            return;

        ulong bits = (ulong)(object)current;
        for (int i = 0; i < Values.Length; i++)
        {
            ulong flag = (ulong)(object)Values[i];
            bool selected = flag == 0 ? bits == 0 : (bits & flag) == flag;
            if (!ImGui.Selectable(Options[i], selected) || member.IsReadOnly)
                continue;

            bits = flag == 0
                ? 0
                : selected
                    ? bits & ~flag
                    : bits | flag;
        }

        ImGui.EndCombo();

        member.Value = (T)(object)bits;
    }
}
