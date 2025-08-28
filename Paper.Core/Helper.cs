using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;

namespace Paper.Core;

public static class Helper
{
    public static ref T Ref<T>(this List<T> list, int index) => ref CollectionsMarshal.AsSpan(list)[index];
    public static ref T Ref<T>(this List<T> list, Index index) => ref CollectionsMarshal.AsSpan(list)[index];
    public static Vector2 RandomVector(Vector2 scale)
    {
        return new Vector2(Random.Shared.NextSingle(), Random.Shared.NextSingle()) * scale;
    }

    public static float MeasureAngle(Vector2 value)
    {
        return MathF.Atan2(value.Y, value.X);
    }

    public static Vector2 PointTo(Vector2 fromA, Vector2 toB)
    {
        return toB - fromA;
    }

    public static Vector2 NormalizeSafeCopy(this Vector2 vector2)
    {
        if(vector2 == default)
        {
            return Vector2.Zero;
        }

        return Vector2.Normalize(vector2);
    }

    public static Rectangle RectangleFromCenterSize(Vector2 center, Vector2 size)
    {
        Vector2 tl = center - size * 0.5f;
        return new Rectangle(tl.ToPoint(), size.ToPoint());
    }

    public static int TaxicabDistance(Point A, Point B)
    {
        return Math.Abs(A.X - B.X) + Math.Abs(A.Y - B.Y);
    }

    public static float TaxicabDistance(Vector2 A, Vector2 B)
    {
        return Math.Abs(A.X - B.X) + Math.Abs(A.Y - B.Y);
    }
}
