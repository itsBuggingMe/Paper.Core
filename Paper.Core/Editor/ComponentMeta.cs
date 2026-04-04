using Frent;
using Frent.Components;
using Frent.Core;
using Microsoft.Xna.Framework;
using Paper.Core.Editor.Converters;
using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Reflection;

namespace Paper.Core.Editor;
public class ComponentMeta(ComponentID id)
{
    public string Name { get; private set; } = id.Type.Name;
    public string Description { get; private set; } = id.Type.GetCustomAttribute<DescriptionAttribute>()?.Description ?? "<no description>";
    public ComponentID ID { get; private set; } = id;

    public ImmutableArray<ComponentID> Arguments { get; private set; } =
        id.Type.GetMethods(BindingFlags.Instance | BindingFlags.Public)
               .Where(m => m.Name == "Update")
               .SelectMany(m => m.GetParameters())
               .Where(t => t.ParameterType.IsByRef)
               .Select(p => Component.GetComponentID(p.ParameterType.GetElementType()!))
               .ToImmutableArray();

    public ImmutableArray<ComponentField> ComponentFields { get; init; } = GetComponentFields(id);

    public static readonly Dictionary<Type, IFieldModifer> FieldModifierTable = typeof(ComponentMeta)
        .Assembly
        .GetTypes()
        .Where(t => t.IsAssignableTo(typeof(IFieldModifer)) && !t.IsAbstract && !t.IsInterface && !t.IsGenericTypeDefinition)
        .Select(t => (IFieldModifer)Activator.CreateInstance(t)!)
        .ToDictionary(k => k.FieldType);

    private static ImmutableArray<ComponentField> GetComponentFields(ComponentID id)
    {
        var members = id.Type.GetMembers(BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
        var fieldsWithInclude = members
                .Where(t => Attribute.IsDefined(t, typeof(EditorInclude))).ToArray();

        if(fieldsWithInclude.Length == 0)
        {
            return members
                .Where(t => 
                t is FieldInfo f && f.IsPublic || 
                t is PropertyInfo p && p.CanWrite && p.CanRead && p.GetAccessors().All(p => p.IsPublic))
                .Select(t => t is FieldInfo f ? new ComponentField(id, f) : new ComponentField(id, (PropertyInfo)t))
                .ToImmutableArray();
        }

        return fieldsWithInclude
            .Select(t => t is FieldInfo f ? new ComponentField(id, f) : new ComponentField(id, (PropertyInfo)t))
            .ToImmutableArray();
    }

    private static readonly Dictionary<ComponentID, ComponentMeta> s_componentMetaTableCache = [];

    public static IFieldModifer? GetFieldModifer(Type fieldType)
    {
        if (FieldModifierTable.TryGetValue(fieldType, out var cachedMetadata))
            return cachedMetadata;

        if (fieldType.IsEnum)
        {
            FieldModifierTable.Add(fieldType, (IFieldModifer?)Activator.CreateInstance(typeof(EnumFieldConverter<>).MakeGenericType(fieldType)) ?? throw new Exception("Unable to create enum converter."));
        }

        if (Nullable.GetUnderlyingType(fieldType) is Type underlyingType)
        {
            FieldModifierTable.Add(fieldType, (IFieldModifer?)Activator.CreateInstance(typeof(NullableFieldConverter<>).MakeGenericType(underlyingType)) ?? throw new Exception("Unable to create enum converter."));
        }

        return null;
    }

    public static ComponentMeta GetComponentMeta(ComponentID componentType)
    {
        if (s_componentMetaTableCache.TryGetValue(componentType, out var cachedMetadata))
            return cachedMetadata;

        ComponentMeta componentMeta = new(componentType);
        s_componentMetaTableCache.Add(componentType, componentMeta);
        return componentMeta;
    }
}

public class ComponentField
{
    public ComponentField(ComponentID id, FieldInfo info)
    {
        Type = info.FieldType;
        ComponentID = id;
        _fieldInfo = info;
    }

    public ComponentField(ComponentID id, PropertyInfo info)
    {
        Type = info.PropertyType;
        ComponentID = id;
        _propertyInfo = info;
    }

    public ReadOnlySpan<char> Name
    {
        get
        {
            string name = _fieldInfo?.Name ?? _propertyInfo?.Name ?? throw new UnreachableException();


            if (name.StartsWith("E_"))
            {
                return name.AsSpan(2);
            }
            return name;
        }
    }

    public ComponentID ComponentID { get; init; }
    public Type Type { get; init; }
    private FieldInfo _fieldInfo;
    private PropertyInfo _propertyInfo;

    public object GetValue(object component)
    {
        if (_fieldInfo is not null)
        {
            return _fieldInfo.GetValue(component)!;
        }

        return _propertyInfo!.GetValue(component)!;
    }

    public void SetValue(Entity entity, object value)
    {
        object component = entity.Get(ComponentID);

        if (_fieldInfo is not null)
        {
            _fieldInfo.SetValue(component, value);
        }
        else
        {
            _propertyInfo!.SetValue(component, value);
        }

        entity.Set(ComponentID, component);
    }
}