using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace Paper.Core;

public class GraphicsBase
{
    public SpriteBatch SpriteBatch { get; private set; }
    public Texture2D WhitePixel { get; private set; }
    public GraphicsDevice GraphicsDevice { get; private set; }
    public GraphicsDeviceManager GraphicsDeviceManager { get; private set; }
    public ContentManager Content { get; private set; }
    public Camera2D Camera { get; private set; }
    public Game Game { get; private set; }
    public Fonts Fonts { get; private set; }
    public GraphicsBase(GraphicsDeviceManager graphicsDeviceManager, ContentManager content, Game game)
    {
        Content = content;
        Game = game;
        GraphicsDeviceManager = graphicsDeviceManager;
        GraphicsDevice = graphicsDeviceManager.GraphicsDevice;
        SpriteBatch = new SpriteBatch(GraphicsDevice);
        WhitePixel = new Texture2D(GraphicsDevice, 1, 1);
        Camera = new Camera2D(GraphicsDevice);
        WhitePixel.SetData([Color.White]);
        SpriteFont? second = null;
        try { second = content.Load<SpriteFont>("SubFont"); } catch { }
        SpriteFont main = content.Load<SpriteFont>("MainFont");
        Fonts = new Fonts(main, second ?? main);
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