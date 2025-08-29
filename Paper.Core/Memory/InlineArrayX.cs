using System.Runtime.CompilerServices;

namespace Paper.Core.Memory;

[InlineArray(4)]
public struct InlineArray4<T>
{
    private T _t0;
}

[InlineArray(8)]
public struct InlineArray8<T>
{
    private T _t0;
}
