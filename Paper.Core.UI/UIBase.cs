using Microsoft.Xna.Framework;

namespace Paper.Core.UI;

public abstract class UIBase<TGraphics>
{
    public bool IsRootElement => _parent is null;
    private UIBase<TGraphics>? _parent;

    protected UIBase<TGraphics>? Parent => _parent;
    private readonly List<UIBase<TGraphics>> _children = [];
    private UIVector2 _position;
    private UIVector2 _size;

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

    public Vector2 Position
    {
        get
        {
            Vector2 basedPosition = _parent is null ? default : _parent.Position;
            Vector2 vec = _position.Scale(ScaleMultiplerPos);
            return basedPosition + vec - ElementAlign * Size;
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

    public Rectangle Bounds => new Rectangle(Position.ToPoint(), Size.ToPoint());

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
            foreach (var item in _children)
            {
                if(!value.Contains(item))
                    RemoveChild(item);
            }
            foreach(var item in value)
            {
                if(!_children.Contains(item))
                    AddChild(item);
            }
        }
    }

    public virtual void Update()
    {
        foreach (var item in Children)
            item.Update();
    }
    public virtual void Draw()
    {
        foreach(var item in Children)
            item.Draw();
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
