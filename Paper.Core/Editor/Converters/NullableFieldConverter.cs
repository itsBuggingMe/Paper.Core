using Frent;
using Frent.Core;
using ImGuiNET;
using System;
using System.Reflection;

namespace Paper.Core.Editor.Converters;

internal class NullableFieldConverter<T> : ConverterAttribute<T?> where T : struct
{
    private readonly ConverterAttribute _innerConverter;
    private readonly EditorMember<T> _innerMember;
    private readonly ValueHolder _holder = new();

    public NullableFieldConverter(ConverterAttribute innerConverter)
    {
        _innerConverter = innerConverter;
        FieldInfo valueField = typeof(ValueHolder).GetField(nameof(ValueHolder.Value))!;
        _innerMember = new EditorMember<T>(new EditorMemberInfo(valueField), innerConverter);
    }

    protected override void Display(Entity entity, ComponentID component, EditorMember<T?> member)
    {
        T? current = member.Value;
        bool hasValue = current.HasValue;
        if (ImGui.Checkbox($"{member.Name} (Has Value)", ref hasValue) && !member.IsReadOnly)
        {
            current = hasValue ? default(T) : null;
            member.Value = current;
        }

        if (!current.HasValue)
            return;

        _holder.Value = current.Value;
        _innerMember.Initialize(_holder);
        _innerConverter.CallDisplay(entity, component, _innerMember);
        if (!member.IsReadOnly)
            member.Value = _holder.Value;
    }

    private sealed class ValueHolder
    {
        public T Value;
    }
}
