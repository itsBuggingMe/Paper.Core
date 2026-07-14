using Frent;
using System;
using System.ComponentModel;
using System.Diagnostics.CodeAnalysis;
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
            ValidateContainingType(in _containingType);
            return _getter(_containingType);
        }
        set
        {
            if (_setter is null)
                throw new InvalidOperationException("Property is read only.");
            ValidateContainingType(in _containingType);
            _setter(_containingType, value);
        }
    }

    private TContainingType? _containingType;

    private readonly Func<TContainingType, T?> _getter;
    private readonly Action<TContainingType, T?>? _setter;
    private object? _cachedBoxT = default(T);

    public EditorMember(EditorMemberInfo info) : base(info)
    {
        _getter = info switch
        {
            PropertyInfo p => (p.GetGetMethod() ?? throw new ArgumentException("Editor members must be at least readable.")).CreateDelegate<Func<TContainingType, T>>(),
            FieldInfo f => ((containing) =>
            {
                T tOnStack = default!;
                TypedReference @ref = __makeref(tOnStack);
                if(f.GetValueDirect(@ref) is T t)
                {
                    tOnStack = t;
                }
                return tOnStack;
            }),
        };


        Unbox? unboxer = null;
        _setter = info switch
        {
            PropertyInfo p => p.GetSetMethod()?.CreateDelegate<Action<TContainingType, T?>>(),
            FieldInfo f => (containing, value) =>
            {
                if(typeof(T).IsValueType)
                {
                    unboxer ??= UnsafeUnboxMethodInfo.MakeGenericMethod(typeof(T)).CreateDelegate<Unbox>();
                    unboxer(_cachedBoxT!) = value!;
                }
                else
                {
                    _cachedBoxT = value;
                }
                f.SetValueDirect(__makeref(_containingType), _cachedBoxT!);
            }
        };
    }

    private delegate ref T Unbox(object o);

    public void Initalize(TContainingType containingType)
    {
        _containingType = containingType;
    }

    private void ValidateContainingType([NotNull] in TContainingType? t)
    {
        if (!typeof(TContainingType).IsValueType && t is null)
            throw new InvalidOperationException("Call Initalize first!");
    }
}

public readonly union EditorMemberInfo(PropertyInfo, FieldInfo);
public abstract class EditorMember
{
    internal static readonly MethodInfo UnsafeUnboxMethodInfo = typeof(Unsafe)
        .GetMethod("Unbox")!;

    public bool IsReadOnly { get; set; }
    public EditorMemberInfo Member { get; set; }
    public string Name { get; }
    public int PositionalHash { get; }
    public EditorMember(EditorMemberInfo memberInfo)
    {
        IsReadOnly = memberInfo switch
        {
            PropertyInfo p => !p.CanWrite,
            FieldInfo f => f.IsInitOnly,
        };

        Member = memberInfo;
        Name = ((MemberInfo)memberInfo.Value).Name;
        PositionalHash = Member.GetHashCode();
    }
}