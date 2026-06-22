using System.ComponentModel;
using System.Reactive;
using WorldXaml.UI;
using WorldXaml.UI.Controls;
using WorldXaml.UI.LogicalTree;

namespace WorldXaml.UI.Base;

public abstract class BindableObject : PropertyObject, ILogical, IStyleNode, INamed
{
    #region Resources
    
    /// <summary>
    /// Local resource dictionary for this element.
    /// Resource lookup walks the logical tree through IResourceNode parents.
    /// </summary>
    public StyleSheet? Styles { get; set; }

    /// <summary>
    /// Finds a resource by key, walking up the logical tree.
    /// Returns null if not found.
    /// </summary>
    public object? FindResource(object key)
    {
        var node = this as IStyleNode;
        while (node is not null)
        {
            if (node.Styles is not null && node.Styles.TryGetValue(key, out var value))
                return value;

            node = node is ILogical logical && logical.LogicalParent is IStyleNode parent
                ? parent
                : null;
        }
        return null;
    }
    
    #endregion
    
    public static Property<string?> NameProperty { get; } = Property.Register<BindableObject, string?>("Name");

    public string? Name
    {
        get => GetValue(NameProperty);
        set => SetValue(NameProperty, value);
    }

    /// <summary>
    /// CSS-like class names applied to this element.
    /// Styles can match on these via Selector="Type.classname".
    /// </summary>
    public Classes Classes => field ??= new Classes(this);

    #region Parent/child tree

    private ILogicalRoot? _root;

    public event EventHandler<LogicalTreeAttachmentEventArgs>? AttachedToLogicalTree;
    public event EventHandler<LogicalTreeAttachmentEventArgs>? DetachedFromLogicalTree;

    bool ILogical.IsAttachedToLogicalTree => _root != null;

    public ILogical? LogicalParent
    {
        get;
        set
        {
            if (field == value) return;
            field = value;
            OnParentChanged();
        }
    }
    
    public abstract IReadOnlyList<ILogical> LogicalChildren { get; }

    /// <summary>
    /// Triggered when the object is mounted onto the logical tree.
    /// </summary>
    public AnimationTrigger Mounted { get; } = new();
    
    /// <summary>
    /// Triggered when the object is unmounted from the logical tree.
    /// </summary>
    public AnimationTrigger Unmounted { get; } = new();
    
    public BindableObject()
    {
        _root = this as ILogicalRoot;
        if (_root != null)
        {
            Mounted.Trigger();
        }
    }

    private void OnDetachedFromLogicalTreeCore(LogicalTreeAttachmentEventArgs args)
    {
        if (_root != null)
        {
            Mounted.Reset();
            DetachedFromLogicalTree?.Invoke(this, args);
            Unmounted.Trigger();

            var logicalChildren = LogicalChildren;
            var logicalChildrenCount = logicalChildren.Count;

            for (var i = 0; i < logicalChildrenCount; i++)
            {
                if (logicalChildren[i] is BindableObject child && child._root != args.Root) // child may already have been attached within an event handler
                {
                    child.OnDetachedFromLogicalTreeCore(args);
                }
            }
        }
        
        _root = null;
    }

    private void OnAttachedToLogicalTreeCore(LogicalTreeAttachmentEventArgs args)
    {
        if (_root == null)
        {
            Unmounted.Reset();
            AttachedToLogicalTree?.Invoke(this, args);
            Mounted.Trigger();

            var logicalChildren = LogicalChildren;
            var logicalChildrenCount = logicalChildren.Count;

            for (var i = 0; i < logicalChildrenCount; i++)
            {
                if (logicalChildren[i] is BindableObject child && child._root != args.Root) // child may already have been attached within an event handler
                {
                    child.OnAttachedToLogicalTreeCore(args);
                }
            }

            _root = args.Root;
        }
    }

    private void OnParentChanged()
    {
        // Update logical tree attachment and raise events as needed.
        
        var newRoot = FindLogicalRoot(this);

        if (_root != newRoot)
        {
            if (_root != null)
            {
                var e = new LogicalTreeAttachmentEventArgs(_root, this, LogicalParent);
                OnDetachedFromLogicalTreeCore(e);
            }

            if (newRoot is not null)
            {
                var e = new LogicalTreeAttachmentEventArgs(newRoot, this, LogicalParent);
                OnAttachedToLogicalTreeCore(e);
            }
        }
    }

    private static ILogicalRoot? FindLogicalRoot(ILogical? e)
    {
        while (e != null)
        {
            if (e is ILogicalRoot root)
            {
                return root;
            }

            e = e.LogicalParent;
        }

        return null;
    }
    
    #endregion
}