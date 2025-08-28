using Microsoft.Xna.Framework;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Linq;

namespace Paper.Core;

public class ScreenManager : IGameComponent, IUpdateable, IDrawable
{
    public IScreen CurrentScreen
    {
        get
        {
            ThrowIfNotInitalized(out var current);
            return current;
        }
    }

    private Func<ServiceContainer, IScreen> _firstFactory;
    private Game _game;
    private IScreen _current;
    private ServiceContainer _services;

    private ScreenManager(ServiceContainer services, Func<ServiceContainer, IScreen> firstFactory, Game game)
    {
        _firstFactory = firstFactory;
        _services = services;
        _game = game;
    }

    public bool Enabled => true;
    public int UpdateOrder => default;
    public int DrawOrder => default;
    public bool Visible => true;

#pragma warning disable CS0067 // Event never used
    public event EventHandler<EventArgs> EnabledChanged;
    public event EventHandler<EventArgs> UpdateOrderChanged;
    public event EventHandler<EventArgs> DrawOrderChanged;
    public event EventHandler<EventArgs> VisibleChanged;
#pragma warning restore CS0067 // Event never used

    public static ScreenManager Create<T>(ServiceContainer services, Game game)
        where T : IScreen
    {
        return new ScreenManager(services, CreateScreen<T>, game);
    }

    public void Initialize()
    {
        if (_firstFactory is null)
            throw new InvalidOperationException("Screen Manager already initalized!");
        _current = _firstFactory(_services);
        _current.OnEnter(NullScreen.Create(_services), null);
    }

    public void Update(GameTime gameTime)
    {
        ThrowIfNotInitalized(out IScreen current);
        InputHelper.TickUpdate(_game.IsActive);
        current.Update(gameTime);
    }

    public void Draw(GameTime gameTime)
    {
        ThrowIfNotInitalized(out IScreen current);
        current.Draw(gameTime);
    }

    public void SwitchScreen<T>()
        where T : IScreen
    {
        ThrowIfNotInitalized(out var current);

        var next = CreateScreen<T>(_services);
        object arg = current.OnExit(next);
        _current = next;
        next.OnEnter(current, arg);
    }

    private void ThrowIfNotInitalized(out IScreen current)
    {
        if(_current is null)
        {
            throw new InvalidOperationException("Screen Manager not initalized!");
        }
        current = _current;
    }

    private static IScreen CreateScreen<T>(ServiceContainer serviceContainer)
        where T : IScreen
    {
        var ctor = typeof(T)
            .GetConstructors()
            .First();
        return (IScreen)ctor.Invoke(ctor.GetParameters().Select(p => serviceContainer.GetService(p.ParameterType)).ToArray());
    }

    private class NullScreen : IScreen
    {
        private static readonly NullScreen s_instance = new NullScreen();
        public static IScreen Create(ServiceContainer services) => s_instance;

        public void Update(GameTime gameTime) { }
        public void Draw(GameTime gameTime) { }

        public void OnEnter(IScreen previous, object args) { }
        public object OnExit(IScreen next) => null;
    }
}