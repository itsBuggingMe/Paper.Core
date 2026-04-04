using ImGuiNET;
using System;

namespace Paper.Core.Editor.Converters;

internal class NullableFieldConverter<T> : FieldModifierBase<T?> where T : struct
{
    private readonly IFieldModifer _inner = ComponentMeta.GetFieldModifer(typeof(T))
        ?? throw new Exception("Missing field modifier for nullable type");

    protected override T? UpdateValue(ComponentField field)
    {
        bool hasValue = _current.HasValue;
        if (ImGui.Checkbox($"{field.Name} (Has Value)", ref hasValue))
        {
            if (!hasValue)
                return null;
            if (!_current.HasValue)
                return new Nullable<T>(default);
        }
        if (_current.HasValue)
        {
            _inner.Entity = Entity;
            _inner.FieldToModify = FieldToModify;
            _inner.UpdateUI();
            return (T?)FieldToModify.GetValue(Entity.Get(field.ComponentID));
        }
        else
        {
            return null;
        }
    }
}
