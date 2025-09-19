using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using System;
using System.Diagnostics;
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
    private IScreen? _current;
    private ServiceContainer _services;
    private Time _shared;

    private ScreenManager(ServiceContainer services, Func<ServiceContainer, IScreen> firstFactory, Game game)
    {
        _firstFactory = firstFactory;
        _services = services;
        _game = game;
        _shared = services.GetService<Time>();
    }

    public bool Enabled => true;
    public int UpdateOrder => default;
    public int DrawOrder => default;
    public bool Visible => true;

#pragma warning disable CS0067 // Event never used
    public event EventHandler<EventArgs>? EnabledChanged;
    public event EventHandler<EventArgs>? UpdateOrderChanged;
    public event EventHandler<EventArgs>? DrawOrderChanged;
    public event EventHandler<EventArgs>? VisibleChanged;
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
#if BLAZORGL
        try
        {
#endif
            InputHelper.TickUpdate(_game.IsActive);
            ThrowIfNotInitalized(out IScreen current);

            if (InputHelper.RisingEdge(Keys.R) && InputHelper.Down(Keys.LeftControl))
            {
                var arg = current.OnExit();
                var next = _current = _firstFactory(_services);
                next.OnEnter(current, arg);
            }

            _shared.SetValues(gameTime);
            current.Update(_shared);
#if BLAZORGL
        }
        catch (Exception e)
        {
            Debugger.Break();
        }
#endif
    }

    public void Draw(GameTime gameTime)
    {
#if BLAZORGL
        try
        {
#endif
            ThrowIfNotInitalized(out IScreen current);
            _shared.SetValues(gameTime);
            current.Draw(_shared);
#if BLAZORGL
        }
        catch (Exception e)
        {
            Debugger.Break();
        }
#endif
    }

    public void SwitchScreen<T>()
        where T : IScreen
    {
        ThrowIfNotInitalized(out var current);

        var next = CreateScreen<T>(_services);
        object? arg = current.OnExit();
        _current = next;
        next.OnEnter(current, arg);
    }

    public void SwitchScreen(IScreen next)
    {
        ThrowIfNotInitalized(out var current);

        object? arg = current.OnExit();
        _current = next;
        next.OnEnter(current, arg);
    }

    private void ThrowIfNotInitalized(out IScreen current)
    {
        if (_current is null)
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

        public void Update(Time gameTime) { }
        public void Draw(Time gameTime) { }

        public void OnEnter(IScreen previous, object? args) { }
        public object? OnExit() => null;
    }
}