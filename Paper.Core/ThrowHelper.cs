using System;
using System.Diagnostics.CodeAnalysis;

namespace Paper.Core;

public class ThrowHelper
{
    [DoesNotReturn]
    public static void InvalidOperationException(string message)
    {
        throw new InvalidOperationException(message);
    }
}
