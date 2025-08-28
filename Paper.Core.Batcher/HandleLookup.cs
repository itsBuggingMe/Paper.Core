using Microsoft.Xna.Framework;

namespace Paper.Core.Batcher;

internal struct HandleLookup(Rectangle bounds, Vector2 tL, Vector2 tR, Vector2 bL, Vector2 bR)
{
    public Rectangle Bounds = bounds;
    public Vector2 TL = tL;
    public Vector2 TR = tR;
    public Vector2 BL = bL;
    public Vector2 BR = bR;
    // All textures supported have width, so TR.X must be non zero for it to not be default.
    public bool IsDefault => TR.X == default;
}