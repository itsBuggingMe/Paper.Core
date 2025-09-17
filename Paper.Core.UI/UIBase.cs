using Microsoft.Xna.Framework;

namespace Paper.Core.UI;

public abstract class UIBase
{
    public bool IsRootElement => _parent is null;
    private UIBase? _parent;
    private Vector2 _scaleMultiplier;

    protected UIBase? Parent => _parent;
    protected Vector2 ScaleMultiplier
    {
        get => _scaleMultiplier;
        set => _scaleMultiplier = value;
    }
        
    private readonly List<UIBase> _children = [];
    private readonly UIVector2 _position;
    private readonly UIVector2 _size;
    private readonly Vector2 _elementAlignment;

    public Vector2 Position
    {
        get
        {
            Vector2 basedPosition = _parent is null ? default : _parent.Position;
            Vector2 vec = _position.Scale(_scaleMultiplier);
            return basedPosition + vec - _elementAlignment * Size;
        }
    }

    public Vector2 Size
    {
        get
        {
            Vector2 basedSize = _parent is null ? default : _parent.Size;
            Vector2 vec = _size.Scale(_scaleMultiplier);
            return basedSize + vec;
        }
    }

    public UIBase(UIVector2 xy) : this(xy, default) { }

    public UIBase(UIVector2 xy, UIVector2 size)
    {
        _position = xy;
        _size = size;
        _scaleMultiplier = Vector2.One;
    }

    public IReadOnlyList<UIBase> Children
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

    public virtual void Update() { }
    public virtual void Draw() { }

    public void AddChild(UIBase child)
    {
        _children.Add(child);
        child._parent = this;
    }

    public bool RemoveChild(UIBase child)
    {
        if(_children.Remove(child))
        {
            child._parent = null;
            return true;
        }
        return false;
    }
}
