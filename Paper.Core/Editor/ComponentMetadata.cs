using Frent.Core;
using Paper.Core.Editor.Converters;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;

namespace Paper.Core.Editor;

internal static class ComponentMetadata
{
    private static readonly Dictionary<ComponentID, EditorMember[]> _metadataCache = new();
    private static readonly Dictionary<Type, ConverterAttribute> _builtinConverters = [];
    private static readonly MethodInfo _registerComponent = typeof(Component).GetMethod("RegisterComponent") ??
        throw new Exception("");
    public static EditorMember[] GetComponentMembers(ComponentID componentType)
    {
        if(_metadataCache.TryGetValue(componentType, out EditorMember[]? metadata))
            return metadata;

        // [EditorIgnore] -> [AnyConverterAttribute] -> [Expand] -> Find existing converter
        var allMembers = componentType
                .Type
                .GetMembers(BindingFlags.Instance | BindingFlags.Public)
                .Select(m => m switch
                {
                    FieldInfo fieldInfo => (m, Type: fieldInfo.FieldType),
                    PropertyInfo propertyInfo when propertyInfo.GetIndexParameters().Length == 0 => (m, Type: propertyInfo.PropertyType),
                    _ => default,
                })
                .Where(t => t != default && !t.m.CustomAttributes.Any(a => a.AttributeType == typeof(EditorIgnoreAttribute)))
                .Select(t =>
                {
                    if(t.m.CustomAttributes.FirstOrDefault(a => a.AttributeType.IsAssignableTo(typeof(ConverterAttribute))) is CustomAttributeData converterAttribute)
                    {
                        return (Converter: CreateConverterAttribute(converterAttribute), t);
                    }

                    if(t.m.CustomAttributes.Any(a => a.AttributeType == typeof(ExpandAttribute)))
                    {
                        Type componentConverterType = typeof(ComponentConverter<>).MakeGenericType(t.Type);
                        _registerComponent.MakeGenericMethod(t.Type).Invoke(null, []);
                        return (Converter: (ConverterAttribute?)Activator.CreateInstance(componentConverterType, (object)GetComponentMembers(Component.GetComponentID(t.Type))), t);
                    }

                    return (Converter: GetConverter(t.Type), t);
                })
                .Where(s => s.Converter is not null)
                .Select(s => (EditorMember)Activator.CreateInstance(typeof(EditorMember<>)
                    .MakeGenericType(s.t.Type), s.t.m is FieldInfo f ? new EditorMemberInfo(f) : new EditorMemberInfo((PropertyInfo)s.t.m), s.Converter)!)
                .ToArray();

        return _metadataCache[componentType] = allMembers;

        static ConverterAttribute? CreateConverterAttribute(CustomAttributeData? data)
        {
            if (data is null)
                return null;

            return (ConverterAttribute?)Activator.CreateInstance(data.AttributeType, data.ConstructorArguments.Select(a => a.Value).ToArray());
        }
    }

    public static void RegisterBuiltinConverters(Assembly assembly)
    {
        var converters = assembly.GetTypes()
            .Where(t => t is { IsClass: true, IsAbstract: false } && t.IsAssignableTo(typeof(ConverterAttribute)) && Attribute.IsDefined(t, typeof(BuiltInConverterAttribute)))
            .Select(t => (ConverterAttribute?)Activator.CreateInstance(t))
            .Where(s => s is not null);

        foreach(var converter in converters)
        {
            _builtinConverters[converter!.TargetType] = converter!;
        }

        _metadataCache.Clear();
    }

    internal static ConverterAttribute? GetConverter(Type type)
    {
        if (_builtinConverters.TryGetValue(type, out ConverterAttribute? converter))
            return converter;

        Type? nullableType = Nullable.GetUnderlyingType(type);
        Type? converterType = type.IsEnum
            ? typeof(EnumFieldConverter<>).MakeGenericType(type)
            : nullableType is not null && GetConverter(nullableType) is not null
                ? typeof(NullableFieldConverter<>).MakeGenericType(nullableType)
                : null;

        if (converterType is null)
            return null;

        converter = nullableType is not null
            ? (ConverterAttribute?)Activator.CreateInstance(converterType, GetConverter(nullableType)!)
            : (ConverterAttribute?)Activator.CreateInstance(converterType);

        return _builtinConverters[type] = converter!;
    }
}
