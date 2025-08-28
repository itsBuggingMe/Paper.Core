using System;

namespace Paper.Core.Editor;

/* * * * * * * * * * * * *
 *  Components by default include all public properties, except when excluded. When EditorInclude is used, only ones with editor include are used.
 * * * * * * * * * * * * */
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property | AttributeTargets.Struct | AttributeTargets.Class)]
public class DescriptionAttribute(string description) : Attribute
{
    public string Description { get; set; } = description;
}
[AttributeUsage(AttributeTargets.Field | AttributeTargets.Property)]
public class EditorInclude : Attribute;