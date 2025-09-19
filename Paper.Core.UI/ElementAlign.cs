using Microsoft.Xna.Framework;

namespace Paper.Core.UI;
public static class ElementAlign
{
    public static readonly Vector2 TopLeft = Vector2.Zero;
    public static readonly Vector2 TopRight = Vector2.UnitX;

    public static readonly Vector2 BottomRight = Vector2.One;
    public static readonly Vector2 BottomLeft = Vector2.UnitY;

    public static readonly Vector2 Center = new Vector2(0.5f);

    public static readonly Vector2 TopMiddle = new Vector2(0.5f, 0);
    public static readonly Vector2 BottomMiddle = new Vector2(0.5f, 1);

    public static readonly Vector2 LeftMiddle = new Vector2(0, 0.5f);
    public static readonly Vector2 RightMiddle = new Vector2(1, 0.5f);
}