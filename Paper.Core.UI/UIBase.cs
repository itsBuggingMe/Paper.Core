using Microsoft.Xna.Framework;

namespace Paper.Core.UI;

public abstract class UIBase<TGraphics>
{
    public bool IsRootElement => _parent is null;
    private UIBase<TGraphics>? _parent;

    protected UIBase<TGraphics>? Parent => _parent;
    private readonly List<UIBase<TGraphics>> _children = [];
    private readonly UIVector2 _position;
    private UIVector2 _size;

    private readonly Vector2 _elementAlignment;
    protected TGraphics Graphics => _graphics ?? throw new InvalidOperationException("No Graphics Object - Does this UI have a parent?");
    internal TGraphics? _graphics;

    public Vector2 Position
    {
        get
        {
            Vector2 basedPosition = _parent is null ? default : _parent.Position;
            Vector2 vec = _position.Scale(ScaleMultipler);
            return basedPosition + vec - _elementAlignment * Size;
        }
    }

    public Vector2 Size
    {
        get
        {
            Vector2 vec = _size.Scale(ScaleMultipler);
            return vec;
        }
    }

    internal Vector2 _scaleMultiplerAsRoot;
    public Vector2 ScaleMultipler
    {
        get
        {
            if (Parent is null)
                return _scaleMultiplerAsRoot;
            return new UIVector2(1, 1, _size.DynamicX, _size.DynamicY).Scale(Parent.ScaleMultipler);
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
