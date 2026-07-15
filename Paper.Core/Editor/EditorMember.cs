using Frent;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;

namespace Paper.Core.Editor;

public class EditorMember<TContainingType, T> : EditorMember
{
    public T? Value
    {
        get
        {
            ValidateContainingType(in _boxContainingType.Value);
            return _getter(ref _boxContainingType.Value);
        }
        set
        {
            if (_setter is null)
                throw new InvalidOperationException("Property is read only.");
            ValidateContainingType(in _boxContainingType.Value);
            _setter(ref _boxContainingType.Value, value);
        }
    }

    private readonly GetValue _getter;
    private readonly SetValue? _setter;
    private readonly StrongBox<TContainingType> _boxContainingType = new StrongBox<TContainingType>();

    public EditorMember(EditorMemberInfo info) : base(info)
    {
        FieldInfo[]? innerFields = null;
        
        _getter = info switch
        {
            PropertyInfo p => p.GetGetMethod()?.CreateDelegate<GetValue>()
                ?? throw new ArgumentException("Editor members must be at least readable."),
            FieldInfo f => (ref containing) =>
            {
                innerFields ??= [StrongBoxField, f];
                _boxContainingType.Value = containing!;
                return __refvalue(TypedReference.MakeTypedReference(_boxContainingType, innerFields), T);
            },
            null => throw new ArgumentNullException(nameof(info)),
        };

        _setter = info switch
        {
            PropertyInfo p => p.GetSetMethod()?.CreateDelegate<SetValue>(),
            FieldInfo f => (ref containing, value) =>
            {
                innerFields ??= [StrongBoxField, f];
                _boxContainingType.Value = containing!;
                __refvalue(TypedReference.MakeTypedReference(_boxContainingType, innerFields), T) = value!;
            }
        };
    }

    public void Initalize(TContainingType containingType)
    {
        _boxContainingType.Value = containingType;
    }

    private void ValidateContainingType([NotNull] in TContainingType? t)
    {
        if (!typeof(TContainingType).IsValueType && t is null)
            throw new InvalidOperationException("Call Initalize first!");
    }

    private delegate void SetValue(ref TContainingType containingType, T? value);
    private delegate T? GetValue(ref TContainingType containingType);
}

public union EditorMemberInfo(PropertyInfo, FieldInfo)
{
    public readonly Type? Type => Value switch
    {
        PropertyInfo p => p.PropertyType,
        FieldInfo f => f.FieldType,
        _ => null,
    };

    public readonly MemberInfo? AsMemberInfo() => Value as MemberInfo;
}
public abstract class EditorMember
{
    internal static readonly FieldInfo StrongBoxField = typeof(StrongBox<>)
        .GetField("Value", BindingFlags.Public | BindingFlags.Instance)!;
    public bool IsReadOnly { get; set; }
    public EditorMemberInfo Member { get; set; }
    public string Name { get; }
    public int PositionalHash { get; }
    public ConverterAttribute Converter { get; }
    public EditorMember(EditorMemberInfo memberInfo)
    {
        if (memberInfo is null)
            throw new ArgumentNullException(nameof(memberInfo));

        IsReadOnly = memberInfo switch
        {
            PropertyInfo p => !p.CanWrite,
            FieldInfo f => f.IsInitOnly,
        };

        Member = memberInfo;
        Name = ((MemberInfo)memberInfo.Value).Name;
        PositionalHash = Member.GetHashCode();
        Converter = memberInfo!.AsMemberInfo()!
                .CustomAttributes!
                .Where(a => a.AttributeType.IsAssignableFrom(typeof(ConverterAttribute)))
                .FirstOrDefault() ??
                ComponentMetadata.DefaultConverterAttributeForType(memberInfo!.Type)!;
    }

    
}