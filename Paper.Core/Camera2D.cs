using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using System.Xml.Linq;

namespace Paper.Core;

public class Camera2D
{
    public Matrix View
    {
        get => Matrix.CreateTranslation(Position.X, Position.Y, 0) *
            Matrix.CreateScale(Scale.X, Scale.X, 1) *
            Matrix.CreateTranslation(_graphicsDevice.Viewport.Width * NormalizedOrigin.X, _graphicsDevice.Viewport.Height * NormalizedOrigin.Y, 0)
        ;
    }

    public Matrix Projection => Matrix.CreateOrthographicOffCenter(
        _graphicsDevice.Viewport.X,
        _graphicsDevice.Viewport.Width,
        _graphicsDevice.Viewport.Height,
        _graphicsDevice.Viewport.Y, 0, 1);

    private readonly GraphicsDevice _graphicsDevice;

    public Vector2 Position { get; set; }
    public Vector2 Scale { get; set; }
    public Vector2 NormalizedOrigin { get; set; }

    public Camera2D(GraphicsDevice graphicsDevice, Vector2? scale = null, Vector2? originNorm = null)
    {
        _graphicsDevice = graphicsDevice;
        Scale = scale ?? Vector2.One;
        NormalizedOrigin = originNorm ?? new(0.5f);
    }

    public Vector2 ScreenToWorld(Vector2 screenPosition)
    {
        Vector3 worldPos3 = Vector3.Transform(new Vector3(screenPosition, 0), Matrix.Invert(View));

        return new Vector2(worldPos3.X, worldPos3.Y);
    }

}