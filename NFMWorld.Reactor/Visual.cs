using System.Numerics;
using NFMWorld.Reactor.Events;

namespace NFMWorld.Reactor;

public abstract partial class Visual : INamed
{
    [Property]
    public string? Name { get; set; }

    private StyleSheetState? _oldStyleState;

    /// <summary>
    /// List of classes applied to this element.
    /// </summary>
    [Property]
    public StyleSheet? Style
    {
        get;
        set
        {
            var oldStyle = field;
            field = value;
            UpdateStyleSheet(oldStyle, Style);
        }
    }

    private StyleSheetState GetSheetState()
    {
        var state = StyleSheetState.Normal;
        if (IsHovered)
            state |= StyleSheetState.Hover;
        if (IsActive)
            state |= StyleSheetState.Active;
        if (IsFocused)
            state |= StyleSheetState.Focus;
        return state;
    }

    private void UpdateStyleSheet(StyleSheet? oldStyleSheet, StyleSheet? newStyleSheet)
    {
        var newState = GetSheetState();
        if (newState != _oldStyleState || !Equals(oldStyleSheet, newStyleSheet))
        {
            UpdateStyles(_oldStyleState is {} oldStyleState ? oldStyleSheet?.GetStylesForState(oldStyleState) : null, newStyleSheet?.GetStylesForState(newState));
            _oldStyleState = newState;
        }
    }

    protected virtual void UpdateStyles(StyleSheetStyles? oldStyleSheet, StyleSheetStyles? newStyleSheet)
    {
    }

    #region Parent/child tree

    private IVisualRoot? _root;

    /// <summary>
    /// Raised when the control is attached to a rooted logical tree.
    /// </summary>
    [Property]
    public Action<VisualTreeAttachmentEventArgs>? AttachedToVisualTree { get; set; }
    
    /// <summary>
    /// Raised when the control is detached from a rooted logical tree.
    /// </summary>
    [Property]
    public Action<VisualTreeAttachmentEventArgs>? DetachedFromVisualTree { get; set; }

    /// <summary>
    /// Gets a value indicating whether the element is attached to a rooted logical tree.
    /// </summary>
    public bool IsAttachedToVisualTree => _root != null;

    public Visual? VisualParent
    {
        get;
        set
        {
            if (!Equals(field, value))
            {
                field = value;
                OnParentChanged();
            }
        }
    }

    public Visual()
    {
        _root = this as IVisualRoot;
        if (_root != null)
        {
            AttachedToVisualTree?.Invoke(new VisualTreeAttachmentEventArgs(_root, this, VisualParent));
        }
    }

    private void OnDetachedFromVisualTreeCore(VisualTreeAttachmentEventArgs args)
    {
        if (_root != null)
        {
            DetachedFromVisualTree?.Invoke(args);

            var logicalChildren = VisualChildren;
            var logicalChildrenCount = logicalChildren.Count;

            for (var i = 0; i < logicalChildrenCount; i++)
            {
                if (logicalChildren[i] is { } child && child._root != args.Root) // child may already have been attached within an event handler
                {
                    child.OnDetachedFromVisualTreeCore(args);
                }
            }
        }
        
        _root = null;
    }

    private void OnAttachedToVisualTreeCore(VisualTreeAttachmentEventArgs args)
    {
        if (_root == null)
        {
            AttachedToVisualTree?.Invoke( args);

            var logicalChildren = VisualChildren;
            var logicalChildrenCount = logicalChildren.Count;

            for (var i = 0; i < logicalChildrenCount; i++)
            {
                if (logicalChildren[i] is { } child && child._root != args.Root) // child may already have been attached within an event handler
                {
                    child.OnAttachedToVisualTreeCore(args);
                }
            }

            _root = args.Root;
        }
    }

    private void OnParentChanged()
    {
        // Update logical tree attachment and raise events as needed.
        
        var newRoot = FindVisualRoot(this);

        if (_root != newRoot)
        {
            if (_root != null)
            {
                var e = new VisualTreeAttachmentEventArgs(_root, this, VisualParent);
                OnDetachedFromVisualTreeCore(e);
            }

            if (newRoot is not null)
            {
                var e = new VisualTreeAttachmentEventArgs(newRoot, this, VisualParent);
                OnAttachedToVisualTreeCore(e);
            }
        }
    }

    private static IVisualRoot? FindVisualRoot(Visual? e)
    {
        while (e != null)
        {
            if (e is IVisualRoot root)
            {
                return root;
            }

            e = e.VisualParent;
        }

        return null;
    }
    
    #endregion
    
    /// <summary>
    /// <para>
    /// Gets the Yoga node associated with this visual element representing its contents.
    /// </para>
    ///
    /// <para>
    /// For a visual element which is itself a node, this is the backing Yoga node.
    /// </para>
    /// 
    /// <para>
    /// For a visual element which is a collection of nodes, this should be a parent Yoga node that contains all the
    /// child nodes as its children. This allows the visual element to manage a group of nodes as a single unit for
    /// layout and rendering purposes. The node's lifetime should last as long as the parent visual element.
    /// </para>
    ///
    /// <para>
    /// For a visual element which is a template, this should be a Yoga node that contains the template's layout tree
    /// (its chrome). The node's lifetime should last as long as the parent visual element.
    /// </para>
    ///
    /// <para>
    /// The node behind this property should not change during the lifetime of a visual element, because changes to it
    /// will not automatically be reflected in the parent Yoga node. Thus if the visual element needs to change the Yoga
    /// node it uses for its contents, it is desirable to provide a wrapper Yoga node with <see cref="Display"/> set
    /// to <see cref="Display.Contents"/> instead.
    /// </para>
    /// </summary>
    internal abstract YGNodePtr Contents { get; }

    /// <summary>
    /// Gets the visual children of this visual element. Visual elements are ones that participate in the layout tree,
    /// receive hit testing, game tick updates, and draw calls.
    /// </summary>
    public abstract IReadOnlyList<Visual> VisualChildren { get; }

    /// <summary>
    /// Whether this visual can accept child nodes. <see cref="Node"/> returns false;
    /// <see cref="FlexPanel"/> returns true.
    /// </summary>
    public abstract bool CanHaveChildren { get; }

    /// <summary>Add a child to the end of the children list.</summary>
    public abstract void AddChild(Visual child);

    /// <summary>Insert a child at the given index.</summary>
    public abstract void InsertAt(int index, Visual child);

    /// <summary>Remove the child at the given index.</summary>
    public abstract void RemoveAt(int index);

    [Property]
    public bool IsFocusable { get; set; }

    public abstract Vector2 FocusOrigin { get; }
    
    public abstract Vector2 FocusSize { get; }

    [Property]
    public bool IsHovered
    {
        get;
        set
        {
            field = value;
            UpdateStyleSheet(Style, Style);
        }
    }

    [Property]
    public bool IsActive
    {
        get;
        set
        {
            field = value;
            UpdateStyleSheet(Style, Style);
        }
    }

    [Property]
    public bool IsFocused
    {
        get;
        set
        {
            field = value;
            UpdateStyleSheet(Style, Style);
        }
    }

    [Property]
    public int TabOrder { get; set; }

    /// <summary>
    /// Opaque key for list reconciliation in the Reactor reconciler.
    /// Keys survive across renders to preserve element identity.
    /// </summary>
    [Property]
    public object? Key { get; set; }

    // Reusable snapshot buffer so dispatch methods don't allocate a new list
    // every time VisualChildren is iterated. Allocated once per Visual, cleared
    // and repopulated on each use.
    private List<Visual>? _childSnapshot;

    private protected List<Visual> GetChildSnapshot()
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

    public virtual void Update(FocusManager focusManager)
    {
        foreach (var child in GetChildSnapshot())
        {
            child.Update(focusManager);
        }
    }

    public virtual void Render(RenderContext context)
    {
        foreach (var child in GetChildSnapshot())
        {
            child.Render(context);
        }
    }

    public virtual void DispatchMouseMoved(FocusManager focusManager, BaseMouseMoveEvent @event)
    {
        foreach (var child in GetChildSnapshot())
        {
            child.DispatchMouseMoved(focusManager, @event);
        }
    }

    public virtual void DispatchMouseEntered(FocusManager focusManager, BaseMouseMoveEvent @event)
    {
        foreach (var child in GetChildSnapshot())
        {
            child.DispatchMouseEntered(focusManager, @event);
        }
    }

    public virtual void DispatchMouseLeft(FocusManager focusManager, BaseMouseMoveEvent @event)
    {
        foreach (var child in GetChildSnapshot())
        {
            child.DispatchMouseLeft(focusManager, @event);
        }
    }

    public virtual void DispatchMousePressed(FocusManager focusManager, BaseMouseEvent @event)
    {
        foreach (var child in GetChildSnapshot())
        {
            child.DispatchMousePressed(focusManager, @event);
        }
    }

    public virtual void DispatchMouseReleased(FocusManager focusManager, BaseMouseEvent @event)
    {
        foreach (var child in GetChildSnapshot())
        {
            child.DispatchMouseReleased(focusManager, @event);
        }
    }

    public virtual void DispatchMouseDragged(FocusManager focusManager, BaseMouseDragEvent @event)
    {
        foreach (var child in GetChildSnapshot())
        {
            child.DispatchMouseDragged(focusManager, @event);
        }
    }

    public virtual void DispatchMouseScrolled(FocusManager focusManager, BaseMouseWheelEvent @event)
    {
        foreach (var child in GetChildSnapshot())
        {
            child.DispatchMouseScrolled(focusManager, @event);
        }
    }

    public virtual void DispatchKeyPressed(FocusManager focusManager, KeyboardEvent @event)
    {
        foreach (var child in GetChildSnapshot())
        {
            child.DispatchKeyPressed(focusManager, @event);
        }
    }

    public virtual void DispatchKeyReleased(FocusManager focusManager, KeyboardEvent @event)
    {
        foreach (var child in GetChildSnapshot())
        {
            child.DispatchKeyReleased(focusManager, @event);
        }
    }

    public virtual void DispatchKeyTyped(FocusManager focusManager, KeyboardTypingEvent @event)
    {
        foreach (var child in GetChildSnapshot())
        {
            child.DispatchKeyTyped(focusManager, @event);
        }
    }
}