using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Paper.Core.Batcher;

namespace Paper.Core;

public class Graphics
{
    public SpriteBatch SpriteBatch { get; private set; }
    public Texture2D WhitePixel { get; private set; }
    public GraphicsDevice GraphicsDevice { get; private set; }
    public GraphicsDeviceManager GraphicsDeviceManager { get; private set; }
    public AtlasBatcher Batcher { get; private set; }
    public ContentManager Content { get; private set; }
    public Fonts Fonts { get; private set; }
    public Graphics(GraphicsDeviceManager graphicsDeviceManager, ContentManager content)
    {
        Content = content;
        Batcher = new AtlasBatcher(graphicsDeviceManager.GraphicsDevice, content);
        GraphicsDeviceManager = graphicsDeviceManager;
        GraphicsDevice = graphicsDeviceManager.GraphicsDevice;
        SpriteBatch = new SpriteBatch(GraphicsDevice);
        WhitePixel = new Texture2D(GraphicsDevice, 1, 1);
        WhitePixel.SetData([Color.White]);
        Fonts = new Fonts(content.Load<SpriteFont>("MainFont"), content.Load<SpriteFont>("SubFont"));
    }

    public void DrawRectangle(Rectangle rectangle, Color color)
    {
        SpriteBatch.Draw(WhitePixel, rectangle, color);
    }

    public void DrawRectangleOutline(Rectangle rectangle, Color color, float lineThickness)
    {
        Vector2 topLeft = new Vector2(rectangle.Left, rectangle.Top) - Vector2.One;
        Vector2 topRight = new(rectangle.Right, rectangle.Top);
        Vector2 bottomRight = new(rectangle.Right, rectangle.Bottom);
        Vector2 bottomLeft = new(rectangle.Left, rectangle.Bottom);

        DrawLine(topLeft, topRight, color, lineThickness);
        DrawLine(topRight, bottomRight, color, lineThickness);
        DrawLine(bottomRight, bottomLeft, color, lineThickness);
        DrawLine(bottomLeft, topLeft, color, lineThickness);
    }

    public void DrawLine(Vector2 a, Vector2 b, Color color, float thickness)
    {
        Vector2 delta = Helper.PointTo(a, b);
        float angle = Helper.MeasureAngle(delta) - MathHelper.PiOver2;// +y is down, not up
        float length = delta.Length();

        SpriteBatch.Draw(WhitePixel, a, null, color, angle, new Vector2(0.5f, 0), new Vector2(thickness, length), SpriteEffects.None, 0);
    }
}