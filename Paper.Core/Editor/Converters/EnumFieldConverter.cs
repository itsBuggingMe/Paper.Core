using Frent;
using Frent.Core;
using ImGuiNET;
using System;
using System.Reflection.PortableExecutable;

namespace Paper.Core.Editor.Converters;

internal class EnumFieldConverter<T> : ConverterAttribute<T> where T : struct, Enum
{
    private static readonly string[] Options = Enum.GetNames<T>();
    private static readonly T[] Values = Enum.GetValues<T>();
    private static readonly bool IsFlags = typeof(T).IsDefined(typeof(FlagsAttribute), false);
    private static readonly TypeCode s_enumTypeCode = Type.GetTypeCode(typeof(T));
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

        ulong bits = ValueToUlong(current);
        for (int i = 0; i < Values.Length; i++)
        {
            ulong flag = ValueToUlong(Values[i]);
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

        member.Value = UlongToValue(bits);
    }

    private static ulong ValueToUlong(T @enum) => s_enumTypeCode switch
    {
        TypeCode.Int32 => (ulong)(int)(object)@enum,
        TypeCode.UInt32 => (ulong)(uint)(object)@enum,
        TypeCode.Int64 => (ulong)(long)(object)@enum,
        TypeCode.UInt64 => (ulong)(ulong)(object)@enum,
        TypeCode.Byte => (ulong)(byte)(object)@enum,
        TypeCode.SByte => (ulong)(sbyte)(object)@enum,
        TypeCode.Int16 => (ulong)(short)(object)@enum,
        TypeCode.UInt16 => (ulong)(ushort)(object)@enum,
        _ => throw new NotSupportedException(),
    };

    private static T UlongToValue(ulong num) => s_enumTypeCode switch
    {
        TypeCode.Int32 => (T)(object)(int)num,
        TypeCode.UInt32 => (T)(object)(uint)num,
        TypeCode.Int64 => (T)(object)(long)num,
        TypeCode.UInt64 => (T)(object)(ulong)num,
        TypeCode.Byte => (T)(object)(byte)num,
        TypeCode.SByte => (T)(object)(sbyte)num,
        TypeCode.Int16 => (T)(object)(short)num,
        TypeCode.UInt16 => (T)(object)(ushort)num,
        _ => throw new NotSupportedException(),
    };
}
