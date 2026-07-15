using Frent.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Threading;

namespace Paper.Core.Editor;

internal static class ComponentMetadata
{
    private static readonly Dictionary<ComponentID, EditorComponentModel> _metadataCache = new();
    private static readonly Dictionary<Type, ConverterAttribute> _builtinConverters = new();

    public static EditorComponentModel GetComponentModel(ComponentID componentType)
    {
        if(_metadataCache.TryGetValue(componentType, out EditorComponentModel? metadata))
            return metadata;

        var allMembers = componentType
                .Type
                .GetMembers()
                .Select(m => m switch
                {
                    PropertyInfo p => new EditorMemberInfo(p),
                    FieldInfo f => new EditorMemberInfo(f),
                    _ => null,
                })
                .Where(m => m.AsMemberInfo()?.CustomAttributes.All(a => a))
                .ToLookup(m => m);

        metadata = new EditorComponentModel()
        {
            Members = allMembers[true].Select(m => Activator.CreateInstance(typeof(EditorMember<>).MakeGenericType())).ToArray(),
            Members = allMembers[true],
        };
        return _metadataCache[componentType] = metadata;
    }

    public static ConverterAttribute DefaultConverterAttributeForType(Type type)
    {

    }
}
