using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Paper.Core.Batcher;

public class QuadRenderer(ContentManager content, GraphicsDevice device)
{
    private readonly VertexPositionColorTexture2D[] _vertBuffer = new VertexPositionColorTexture2D[4];
    private static readonly short[] _indexBuffer = [0, 1, 2, 1, 3, 2];

    private readonly Effect _drawEffect = content.Load<Effect>("sprite_batcher");

    public void Draw(Texture2D texture, Vector2 where)
    {
        device.RasterizerState = RasterizerState.CullNone;

        Viewport viewport = device.Viewport;
        Matrix transform = Matrix.CreateOrthographicOffCenter(viewport.X, viewport.Width, viewport.Height, viewport.Y, 0, 1);

        Vector2 size = texture.Bounds.Size.ToVector2();

        _vertBuffer[0] = new(where, Color.White, new(0, 0));
        _vertBuffer[1] = new(where + size * Vector2.UnitX, Color.White, new(1, 0));
        _vertBuffer[2] = new(where + size * Vector2.UnitY, Color.White, new(0, 1));
        _vertBuffer[3] = new(where + size, Color.White, new(1, 1));
        
        _drawEffect.Parameters["Atlas"]?.SetValue(texture);
        _drawEffect.Parameters["Transform"]?.SetValue(transform);

        _drawEffect.CurrentTechnique.Passes[0].Apply();

        device.DrawUserIndexedPrimitives(PrimitiveType.TriangleList, _vertBuffer, 0, 4, _indexBuffer, 0, 2);
    }
}