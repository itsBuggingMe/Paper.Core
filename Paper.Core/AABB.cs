using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Microsoft.Xna.Framework;
using System.Diagnostics.CodeAnalysis;

namespace Paper.Core;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public struct AABB
{
    [UnscopedRef] public ref Vector2 X1Y1 => ref Unsafe.As<AABB, Vector2>(ref this);
    [UnscopedRef] public ref Vector2 X2Y2 => ref Unsafe.Add(ref Unsafe.As<AABB, Vector2>(ref this), 1);
    
    [UnscopedRef] public ref float Top => ref Y1;
    [UnscopedRef] public ref float Left => ref X1;
    [UnscopedRef] public ref float Bottom => ref Y2;
    [UnscopedRef] public ref float Right => ref X2;

    public float X1;
    public float Y1;

    public float X2;
    public float Y2;

    public readonly Vector2 Center => new Vector2(X1 + X2, Y1 + Y2) * 0.5f;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AABB(float x1, float y1, float x2, float y2)
    {
        X1 = x1;
        Y1 = y1;

        X2 = x2;
        Y2 = y2;
    }


    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public AABB(Vector2 A, Vector2 B) : this(A.X, A.Y, B.X, B.Y)
    {
    }

    public static AABB ToAABB(Rectangle rect)
    {
        return new AABB(rect.X, rect.Y, rect.Right, rect.Bottom);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Offset(in float x, in float y)
    {
        X1 += x;
        Y1 += y;

        X2 += x;
        Y2 += y;
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public void Offset(in Vector2 Amount)
        => Offset(in Amount.X, in Amount.Y);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsCollision(ref readonly AABB a, ref readonly AABB b)
    {
        return a.X1 < b.X2 && b.X1 < a.X2 && a.Y1 < b.Y2 && b.Y1 < a.Y2;
    }

    public bool Contains(in Vector2 Point)
    {
        return X1 < Point.X && X2 > Point.X && Y1 < Point.Y && Y2 > Point.Y;
    }

    public Rectangle ToRectangle()
    {
        return new Rectangle((int)X1, (int)Y1, (int)(X2 - X1), (int)(Y2 - Y1));
    }

    public static AABB FromSize(Vector2 center, Vector2 size)
    {
        size *= 0.5f;
        return new AABB(center - size, center + size);
    }

    public static AABB FromSize(Vector2 origin, Vector2 size, Vector2 offset)
    {
        Vector2 topLeft = origin - size * offset;
        return new AABB(topLeft, topLeft + size);
    }

    public static AABB FromSize(Vector2 center, float size)
        => FromSize(center, new Vector2(size));

    public static Vector2 FindMTV(AABB a, AABB b)
    {
        float overlapX1 = b.X2 - a.X1;
        float overlapX2 = a.X2 - b.X1;
        float overlapY1 = b.Y2 - a.Y1;
        float overlapY2 = a.Y2 - b.Y1;

        if (overlapX1 < 0 || overlapX2 < 0 || overlapY1 < 0 || overlapY2 < 0)
        {
            return new Vector2(0, 0);
        }

        float minOverlapX = overlapX1 < overlapX2 ? overlapX1 : -overlapX2;
        float minOverlapY = overlapY1 < overlapY2 ? overlapY1 : -overlapY2;

        if (Math.Abs(minOverlapX) < Math.Abs(minOverlapY))
        {
            return new Vector2(minOverlapX, 0);
        }
        else
        {
            return new Vector2(0, minOverlapY);
        }
    }
}
