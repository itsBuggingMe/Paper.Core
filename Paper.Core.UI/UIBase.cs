using Microsoft.Xna.Framework;
using System.Security.Cryptography;

namespace Paper.Core.UI;

public abstract class UIBase<TGraphics>
{
    public bool IsRootElement => _parent is null;
    private UIBase<TGraphics>? _parent;

    public void Remove() => _parent?.RemoveChild(this);

    protected UIBase<TGraphics>? Parent => _parent;
    private readonly List<UIBase<TGraphics>> _children = [];

    public UIVector2 RawPosition => _position;
    public UIVector2 RawSize => _size;

    private UIVector2 _position;
    private UIVector2 _size;

    public bool Visible { get; set; } = true;

    public Vector2 ElementAlign { get; set; }

    public TGraphics Graphics
    {
        get
        {
            if(_graphics is not null)
                return _graphics;

            if (_parent is null)
                throw new InvalidOperationException("No Graphics Object - Does this UI have a parent?");

            return _graphics = _parent.Graphics;
        }
    }
    internal TGraphics? _graphics;

    public void SetPosition(Vector2 pos)
    {
        _position = _position with
        {
            X = pos.X,
            Y = pos.Y,
        };
    }

    public void SetSize(Vector2 size)
    {
        _size = _size with
        {
            X = size.X,
            Y = size.Y,
        };
    }

    public Vector2 Position
    {
        get
        {
            Vector2 basedPosition = _parent is null ? default : _parent.Position;
            Vector2 vec = _position.Scale(ScaleMultiplerPos);
            return basedPosition + vec;
        }
    }

    public Vector2 Size
    {
        get
        {
            Vector2 vec = _size.Scale(ScaleMultiplerSize);
            return vec;
        }
    }

    internal Vector2 _scaleMultiplerAsRoot;
    public Vector2 ScaleMultiplerSize
    {
        get
        {
            if (Parent is null)
                return _scaleMultiplerAsRoot;
            return new UIVector2(1, 1, _size.DynamicX, _size.DynamicY).Scale(Parent.ScaleMultiplerSize);
        }
    }

    public Vector2 ScaleMultiplerPos
    {
        get
        {
            if (Parent is null)
                return _scaleMultiplerAsRoot;
            return new UIVector2(1, 1, _position.DynamicX, _position.DynamicY).Scale(Parent.ScaleMultiplerPos);
        }
    }

    public Rectangle Bounds => new Rectangle((Position - ElementAlign * Size).ToPoint(), Size.ToPoint());

    public UIBase(UIVector2 xy) : this(xy, default) { }

    public UIBase(UIVector2 xy, UIVector2 size)
    {
        _position = xy;
        _size = size;
        _graphics = default;
    }

    public IReadOnlyList<UIBase<TGraphics>> Children
    {
        get => _children;
        set
        {
            for(int i = _children.Count - 1; i >= 0; i--)
            {
                var item = _children[i];
                if (!value.Contains(item))
                    RemoveChild(item);
            }
            foreach(var item in value)
            {
                if(!_children.Contains(item))
                    AddChild(item);
            }
        }
    }

    public bool DoUpdate()
    {
        if (!Visible)
            return false;
        return Update();
    }

    public virtual bool Update()
    {
        bool interacted = false;
        for(int i = Children.Count - 1; i >= 0; i--)
            interacted |= Children[i].DoUpdate();
        return interacted;
    }

    public void DoDraw()
    {
        if (!Visible)
            return;
        Draw();
    }

    public virtual void Draw()
    {
        for (int i = Children.Count - 1; i >= 0; i--)
            Children[i].DoDraw();
    }

    public void AddChild(UIBase<TGraphics> child)
    {
        _children.Add(child);
        child._parent = this;
        child._graphics = _graphics;
    }

    public bool RemoveChild(UIBase<TGraphics> child)
    {
        if(_children.Remove(child))
        {
            child._parent = null;
            child._graphics = default;
            return true;
        }
        return false;
    }
}
