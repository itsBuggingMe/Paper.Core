using Frent;
using Frent.Core;
using System;

namespace Paper.Core.Editor;

public abstract class ConverterAttribute : Attribute
{
    public abstract void Display(Entity entity, ComponentID component, EditorMember member);
}


[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public abstract class ConverterAttribute<T> : ConverterAttribute
{
    public void Display(Entity entity, ComponentID component, EditorMember member)
    {
        Display(entity, component, member);
    }

    protected abstract void Display(Entity entity, ComponentID component, EditorMember<T> member);
}


[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class EditorIgnore : Attribute;