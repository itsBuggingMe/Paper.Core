using Frent;
using Frent.Core;
using System;

namespace Paper.Core.Editor;

public abstract class ConverterAttribute : Attribute
{
    public abstract Type TargetType { get; }
    public abstract void CallDisplay(Entity entity, ComponentID component, EditorMember member);
}


[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public abstract class ConverterAttribute<T> : ConverterAttribute
{
    public override Type TargetType => typeof(T);
    public override void CallDisplay(Entity entity, ComponentID component, EditorMember member)
        => Display(entity, component, (EditorMember<T>)member);
    protected abstract void Display(Entity entity, ComponentID component, EditorMember<T> member);
}


[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class EditorIgnoreAttribute : Attribute;

[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class ExpandAttribute : Attribute;
[AttributeUsage(AttributeTargets.Class)]
public class BuiltInConverterAttribute : Attribute;