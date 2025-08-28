using System.Collections.Immutable;
using Frent.Core;
using Frent.Components;
using System.Linq;
using System;
using Microsoft.Xna.Framework;
using System.Reflection;
using System.Diagnostics;
using System.Collections.Frozen;
using Frent;

namespace Paper.Core.Editor;
internal class ComponentMeta(ComponentID id)
{
    public string Name { get; private set; } = id.Type.Name;
    public string Description { get; private set; } = id.Type.GetCustomAttribute<DescriptionAttribute>()?.Description;
    public ComponentID ID { get; private set; } = id;

    public static readonly FrozenDictionary<Type, IFieldModifer> FieldModifierTable = typeof(ComponentMeta)
        .Assembly
        .GetTypes()
        .Where(t => t.IsAssignableTo(typeof(IFieldModifer)) && !t.IsAbstract && !t.IsInterface)
        .Select(t => (IFieldModifer)Activator.CreateInstance(t)!)
        .ToFrozenDictionary(k => k.FieldType);

    public ImmutableArray<ComponentID> Arguments { get; private set; } =
        id.Type.GetMethod("Update")?
               .GetParameters()
               .Where(t => t.ParameterType.IsByRef)
               .Select(p => Component.GetComponentID(p.ParameterType.GetElementType()!))
               .ToImmutableArray()
            ?? [];

    public ImmutableArray<ComponentField> ComponentFields { get; init; } = GetComponentFields(id);

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

    public static readonly ImmutableArray<ComponentMeta> Components = Assembly.GetEntryAssembly()!
        .GetTypes()
        .Append(typeof(EditorName))
        .Where(t => t.IsAssignableTo(typeof(IComponentBase)))
        .Select(t => new ComponentMeta(Component.GetComponentID(t)))
        .ToImmutableArray();

    public static readonly FrozenDictionary<ComponentID, ComponentMeta> ComponentMetaTable = Components.ToFrozenDictionary(k => k.ID);
}

internal class ComponentField
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