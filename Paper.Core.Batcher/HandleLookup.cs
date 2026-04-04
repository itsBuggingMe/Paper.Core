using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Paper.Core.Batcher;

internal struct HandleLookup(Texture2D texture, Rectangle bounds, Vector2 tL, Vector2 tR, Vector2 bL, Vector2 bR)
{
    public Texture2D OriginalTexture = texture;
    public Rectangle Bounds = bounds;
    public Vector2 TL = tL;
    public Vector2 TR = tR;
    public Vector2 BL = bL;
    public Vector2 BR = bR;
    // All textures supported have width, so TR.X must be non zero for it to not be default.
    public bool IsDefault => TR.X == default;
}