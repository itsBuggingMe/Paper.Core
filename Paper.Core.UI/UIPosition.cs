using Microsoft.Xna.Framework;

namespace Paper.Core.UI;

public record struct UIVector2
{
    /// <summary>
    /// The given X coordinate.
    /// </summary>
    public readonly float X;
    /// <summary>
    /// The given Y coordinate.
    /// </summary>
    public readonly float Y;

    public readonly Vector2 Vector => new(X, Y);

    /// <summary>
    /// Whether or not the X coordinate is exact or scaled.
    /// </summary>
    public readonly bool DynamicX;
    /// <summary>
    /// Whether or not the Y coordinate is exact or scaled.
    /// </summary>
    public readonly bool DynamicY;

    internal readonly Vector2 Scale(Vector2 scaled)
    {
        return new Vector2(X * (DynamicX ? scaled.X : 1), Y * (DynamicY ? scaled.Y : 1));
    }

    public UIVector2(float value, bool dynamic = false)
    {
        X = value;
        Y = value;
        DynamicX = dynamic;
        DynamicY = dynamic;
    }

    public UIVector2(float x, float y, bool dynamicX = true, bool dynamicY = true)
    {
        X = x;
        Y = y;
        DynamicX = dynamicX;
        DynamicY = dynamicY;
    }

    public UIVector2(Vector2 vector, bool dynamic = true)
    {
        X = vector.X;
        Y = vector.Y;
        DynamicX = dynamic;
        DynamicY = dynamic;
    }
}
