using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace Paper.Core.Editor;

public class EditorMember<T> : EditorMember
{
    public T? Value
    {
        get
        {
            return _getter(_boxContainingType);
        }
        set
        {
            if (_setter is null)
                throw new InvalidOperationException("Property is read only.");
            _setter(_boxContainingType, value);
        }
    }

    public override bool IsReadOnly => _setter is null;

    private readonly GetValue _getter;
    private readonly SetValue? _setter;

    public EditorMember(EditorMemberInfo info, ConverterAttribute converter) : base(info, converter)
    {
        MemberInfo? memberInfo = info.AsMemberInfo();
        ArgumentNullException.ThrowIfNull(memberInfo);
        object initalizedDelegates = s_initalizerUnbound
            .MakeGenericMethod(memberInfo.DeclaringType ?? throw new ArgumentException("Member must not be of unbound generic type!"))
            .Invoke(null, [info])!;
        (_getter, _setter, _boxContainingType) = (ValueTuple<GetValue, SetValue, IStrongBox>)initalizedDelegates;
    }

    private delegate void SetValue(IStrongBox containingType, T? value);
    private delegate T? GetValue(IStrongBox containingType);

    private delegate void SetValueTyped<TContainingType>(ref TContainingType containingType, T? value);
    private delegate T? GetValueTyped<TContainingType>(ref TContainingType containingType);

    private static readonly MethodInfo s_initalizerUnbound =
        (MethodInfo)typeof(EditorMember<T>)
        .GetMember("CreateDelegates", BindingFlags.NonPublic | BindingFlags.Static)[0];


    private static (GetValue, SetValue?, IStrongBox) CreateDelegates<TContainingType>(EditorMemberInfo info)
    {
        FieldInfo[]? innerFields = null;

        GetValue getter = info switch
        {
            PropertyInfo p  when typeof(TContainingType).IsValueType && p.GetGetMethod()?.CreateDelegate<GetValueTyped<TContainingType>>() is { } s
                => (sbox) => s(ref ((StrongBox<TContainingType>)sbox).Value!),
            PropertyInfo p when p.GetGetMethod()?.CreateDelegate<Func<TContainingType?, T>>() is { } c
                => (sbox) => c(((StrongBox<TContainingType>)sbox).Value),
            FieldInfo f => (containing) =>
            {
                if (typeof(TContainingType).IsValueType)
                {
                    innerFields ??= [StrongBoxFieldInfoOf<TContainingType>(), f];
                    return __refvalue(TypedReference.MakeTypedReference(containing, innerFields), T?);
                }
                else if (containing is StrongBox<TContainingType> { Value: not null } typed)
                {
                    innerFields ??= [f];
                    return __refvalue(TypedReference.MakeTypedReference(typed.Value, innerFields), T?);
                }

                return default;
            }
            ,
            _ => throw new ArgumentException("Editor members must be at least readable."),
        };

        SetValue? setter = info switch
        {
            PropertyInfo p when typeof(TContainingType).IsValueType && p.GetSetMethod()?.CreateDelegate<SetValueTyped<TContainingType>>() is { } s
                => (sbox, val) => s(ref ((StrongBox<TContainingType>)sbox).Value!, val),
            PropertyInfo p when p.GetSetMethod()?.CreateDelegate<Action<TContainingType, T?>>() is { } c => (sbox, val) =>
            {
                if (sbox is StrongBox<TContainingType> { Value: not null } typed)
                    c(typed.Value, val);
            }
            ,
            FieldInfo f when !f.IsInitOnly => (containing, val) =>
            {
                if(typeof(TContainingType).IsValueType)
                {
                    innerFields ??= [StrongBoxFieldInfoOf<TContainingType>(), f];
                    __refvalue(TypedReference.MakeTypedReference(containing, innerFields), T?) = val;
                }
                else if(containing is StrongBox<TContainingType> { Value: not null } typed)
                {
                    innerFields ??= [f];
                    __refvalue(TypedReference.MakeTypedReference(typed.Value, innerFields), T?) = val;
                }
            }
            ,
            _ => null,
        };

        return (getter, setter, new StrongBox<TContainingType>());
    }
}

public readonly union EditorMemberInfo(PropertyInfo, FieldInfo)
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
        .GetField("Value", BindingFlags.Public | BindingFlags.Instance)
        ?? throw new Exception("Could not find Value field of StrongBox");

    private static readonly Dictionary<Type, FieldInfo> _strongBoxFieldInfoCache = [];
    internal static FieldInfo StrongBoxFieldInfoOf<T>()
    {
        return CollectionsMarshal.GetValueRefOrAddDefault(_strongBoxFieldInfoCache, typeof(T), out _)
            ??= typeof(StrongBox<T>)
            .GetField("Value", BindingFlags.Public | BindingFlags.Instance)
            ?? throw new Exception("Could not find Value field of StrongBox");
    }
    

    public abstract bool IsReadOnly { get; }
    public EditorMemberInfo Member { get; set; }
    public string Name { get; }
    public int PositionalHash { get; }
    public ConverterAttribute Converter { get; }
    protected IStrongBox _boxContainingType;
    public EditorMember(EditorMemberInfo memberInfo, ConverterAttribute converterAttribute)
    {
        if (memberInfo.Value is not MemberInfo typedAsMemberInfo)
            throw new ArgumentNullException(nameof(memberInfo));

        Member = memberInfo;
        Name = typedAsMemberInfo.Name;
        PositionalHash = Member.GetHashCode();
        Converter = converterAttribute;
        _boxContainingType = null!;
    }

    public void Initialize<TContainingType>(TContainingType? containingType)
    {
        ((StrongBox<TContainingType?>)_boxContainingType).Value = containingType;
    }

    public TContainingType? GetContainingValue<TContainingType>()
    {
        return ((StrongBox<TContainingType?>)_boxContainingType).Value;
    }

}
