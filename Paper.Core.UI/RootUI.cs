using Microsoft.Xna.Framework;

namespace Paper.Core.UI;

public class RootUI<TGraphics> : UIBase<TGraphics>, IDisposable
{
    private readonly Game _game;
    private readonly float _baseWidth;
    private readonly float _baseHeight;

    public RootUI(Game game, TGraphics graphics, float baseWidth, float baseHeight) : base(default, new UIVector2(baseWidth, baseHeight, false, false))
    {
        game.Window.ClientSizeChanged += Resize;
        _game = game;
        _baseWidth = baseWidth;
        _baseHeight = baseHeight;
        _graphics = graphics;
        Resize(null, null);
    }

    private void Resize(object? sender, EventArgs? args)
    {
        _scaleMultiplerAsRoot = _game.GraphicsDevice.Viewport.Bounds.Size.ToVector2() / new Vector2(_baseWidth, _baseHeight);
    }

    public void Dispose()
    {
        _game.Window.ClientSizeChanged -= Resize;
    }
}
