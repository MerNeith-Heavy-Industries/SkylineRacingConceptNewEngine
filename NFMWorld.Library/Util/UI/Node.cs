using System.Numerics;
using NFMWorld.ClayDom.Events;

namespace NFMWorld.Reactor;

public abstract partial class Node
{
    public string? Name { get; set; }

    #region Parent/child tree

    public Node? VisualParent
    {
        get;
        set;
    }

    public Node()
    {
    }

    #endregion
    
    /// <summary>
    /// Gets the Yoga node associated with this visual element representing its contents.
    /// </summary>
    internal abstract YGNodePtr Contents { get; }

    /// <summary>
    /// Gets the visual children of this visual element. Visual elements are ones that participate in the layout tree,
    /// receive hit testing, game tick updates, and draw calls.
    /// </summary>
    public abstract IReadOnlyList<Node> VisualChildren { get; }

    /// <summary>
    /// Whether this visual can accept child nodes. <see cref="Component"/> returns false;
    /// <see cref="View"/> returns true.
    /// </summary>
    public abstract bool CanHaveChildren { get; }

    /// <summary>Add a child to the end of the children list.</summary>
    public abstract void AddChild(Node child);

    /// <summary>Insert a child at the given index.</summary>
    public abstract void InsertAt(int index, Node child);

    /// <summary>Remove the child at the given index.</summary>
    public abstract void RemoveAt(int index);

    // Reusable snapshot buffer so dispatch methods don't allocate a new list
    // every time VisualChildren is iterated. Allocated once per Visual, cleared
    // and repopulated on each use.
    private List<Node>? _childSnapshot;

    private protected List<Node> GetChildSnapshot()
    {
        var list = _childSnapshot ??= [];
        list.Clear();
        list.AddRange(VisualChildren);
        return list;
    }

    internal virtual void NotifyUiScaleChanged()
    {
        foreach (var child in GetChildSnapshot())
        {
            child.NotifyUiScaleChanged();
        }
    }

    public virtual void Update()
    {
        foreach (var child in GetChildSnapshot())
        {
            child.Update();
        }
    }

    public virtual void Render(RenderContext context)
    {
        foreach (var child in GetChildSnapshot())
        {
            child.Render(context);
        }
    }

    public virtual void DispatchMouseMoved(BaseMouseMoveEvent @event)
    {
        foreach (var child in GetChildSnapshot())
        {
            child.DispatchMouseMoved(@event);
        }
    }

    public virtual void DispatchMouseEntered(BaseMouseMoveEvent @event)
    {
        foreach (var child in GetChildSnapshot())
        {
            child.DispatchMouseEntered(@event);
        }
    }

    public virtual void DispatchMouseLeft(BaseMouseMoveEvent @event)
    {
        foreach (var child in GetChildSnapshot())
        {
            child.DispatchMouseLeft(@event);
        }
    }

    public virtual void DispatchMousePressed(BaseMouseEvent @event)
    {
        foreach (var child in GetChildSnapshot())
        {
            child.DispatchMousePressed(@event);
        }
    }

    public virtual void DispatchMouseReleased(BaseMouseEvent @event)
    {
        foreach (var child in GetChildSnapshot())
        {
            child.DispatchMouseReleased(@event);
        }
    }

    public virtual void DispatchMouseDragged(BaseMouseDragEvent @event)
    {
        foreach (var child in GetChildSnapshot())
        {
            child.DispatchMouseDragged(@event);
        }
    }

    public virtual void DispatchMouseScrolled(BaseMouseWheelEvent @event)
    {
        foreach (var child in GetChildSnapshot())
        {
            child.DispatchMouseScrolled(@event);
        }
    }

    public virtual void DispatchKeyPressed(KeyboardEvent @event)
    {
        foreach (var child in GetChildSnapshot())
        {
            child.DispatchKeyPressed(@event);
        }
    }

    public virtual void DispatchKeyReleased(KeyboardEvent @event)
    {
        foreach (var child in GetChildSnapshot())
        {
            child.DispatchKeyReleased(@event);
        }
    }

    public virtual void DispatchKeyTyped(KeyboardTypingEvent @event)
    {
        foreach (var child in GetChildSnapshot())
        {
            child.DispatchKeyTyped(@event);
        }
    }
}
