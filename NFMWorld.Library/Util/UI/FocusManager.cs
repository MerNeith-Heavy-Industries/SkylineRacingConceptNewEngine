using NFMWorld.ClayDom.Events;
using NFMWorldLibrary.Util;

namespace NFMWorld.Reactor;

public static class FocusManager
{
    public static Node? FocusedNode
    {
        get;
        set
        {
            if (field is Component cmp)
                cmp.Unfocused?.Invoke();
            field = value;
            if (value is Component cmp1)
                cmp1.Focused?.Invoke();
        }
    }
    public static Node? ActiveNode { get; set; }

    /// <summary>Move focus to the next focusable element in Tab order.</summary>
    public static bool FocusNext(Node root)
    {
        var all = GetFocusableDescendants(root).ToArray();
        if (all.Length == 0) return false;

        var idx = FocusedNode is not null
            ? all.IndexOf(FocusedNode)
            : -1;

        var next = (idx + 1) % all.Length;
        FocusedNode = all[next];
        return true;
    }

    /// <summary>Move focus to the previous focusable element.</summary>
    public static bool FocusPrev(Node root)
    {
        var all = GetFocusableDescendants(root).ToArray();
        if (all.Length == 0) return false;

        var idx = FocusedNode is not null
            ? all.IndexOf(FocusedNode)
            : all.Length;

        var prev = (idx - 1 + all.Length) % all.Length;
        FocusedNode = all[prev];
        return true;
    }

    /// <summary>
    /// Depth-first enumeration of all focusable Nodes under a root.
    /// Respects Visibility (skips Hidden/Collapsed subtrees).
    /// </summary>
    private static IEnumerable<Node> GetFocusableDescendants(Node root)
    {
        foreach (var child in root.VisualChildren)
        {
            if (child is not Component { IsDisplayed: true } element)
                continue;

            if (element.IsFocusable)
                yield return element;

            foreach (var descendant in GetFocusableDescendants(element))
                yield return descendant;
        }
    }

    /// <summary>
    /// Hit-test: find the topmost focusable Node at a screen position.
    /// Walks children in reverse (topmost rendered last).
    /// </summary>
    public static Node? HitTest(Component root, Vector2 screenPos)
    {
        return HitTestRecursive(root, screenPos);
    }

    private static Component? HitTestRecursive(Component node, Vector2 pos)
    {
        // Skip nodes (and their descendants) clipped out by an overflow ancestor.
        if (node.ClipRect is { } clip && !clip.Contains(pos.X, pos.Y))
            return null;

        // Walk children back-to-front for correct z-order
        foreach (var visual in GetTopmostFirst(node))
        {
            if (!visual.IsDisplayed)
                continue;

            var result = HitTestRecursive(visual, pos);
            if (result is not null) return result;
        }

        // Check self
        var bounds = new RectangleF(
            node.FocusOrigin.X, node.FocusOrigin.Y,
            node.FocusSize.X, node.FocusSize.Y);

        if (bounds.Contains(pos.X, pos.Y))
            return node;

        return null;
    }

    /// <summary>
    /// Hit-tests and returns the full ancestor chain (root→leaf) of nodes at
    /// <paramref name="screenPos"/>. Empty when nothing hit.
    /// </summary>
    public static List<Component> HitTestChain(Component root, LuaVector2 screenPos)
    {
        var chain = new List<Component>();
        HitTestChainRecursive(root, screenPos, chain);
        return chain;
    }

    /// <summary>
    /// Enumerates a node's visual children back-to-front (topmost rendered
    /// last), which is the standard z-order. Equivalent to the previous
    /// <c>OrderBy(TabOrder).Reverse()</c> when TabOrder is uniform (the common
    /// case for the Lua UI, which never sets it), but without per-node LINQ
    /// allocation on every mouse move.
    /// </summary>
    private static IEnumerable<Component> GetTopmostFirst(Component node)
    {
        var children = node.VisualChildren;
        for (int i = children.Count - 1; i >= 0; i--)
        {
            if (children[i] is Component c)
                yield return c;
        }
    }

    private static bool HitTestChainRecursive(Component node, LuaVector2 pos, List<Component> chain)
    {
        // Skip nodes (and their descendants) clipped out by an overflow ancestor.
        if (node.ClipRect is { } clip && !clip.Contains(pos.X, pos.Y))
            return false;

        var selfHit = false;

        var bounds = new RectangleF(
            node.FocusOrigin.X, node.FocusOrigin.Y,
            node.FocusSize.X, node.FocusSize.Y);

        if (bounds.Contains(pos.X, pos.Y))
        {
            chain.Add(node);       // ancestor added before children
            selfHit = true;
        }

        // If self isn't hit, children can't be hit either (they're contained within self).
        // Exception: non-focusable containers — they pass through even though selfHit=false.
        if (!selfHit && node.IsFocusable)
            return false;

        foreach (var visual in GetTopmostFirst(node))
        {
            if (!visual.IsDisplayed)
                continue;

            if (HitTestChainRecursive(visual, pos, chain))
                return true;           // deepest child hit — stop
        }

        return selfHit;
    }

    private static List<Component> _hoveredChain = [];

    /// <summary>
    /// Hit-tests under the cursor, diffs against the previous hover chain,
    /// and dispatches MouseEntered / MouseLeft / MouseMoved as appropriate.
    /// Call once per frame from your MouseMoved handler.
    ///
    /// The hit-test chain already contains exactly the nodes under the cursor
    /// (every ancestor of the topmost hit node), so hover changes are
    /// dispatched only for nodes that actually entered or left the cursor.
    /// Each node's <see cref="Component.DispatchMouseEntered"/> /
    /// <see cref="Component.DispatchMouseLeft"/> are self-only (they do not
    /// recurse into children), so siblings and un-hit descendants are never
    /// spuriously hovered.
    /// </summary>
    public static void DispatchMouseMove(Component root, BaseMouseMoveEvent @event)
    {
        var newChain = HitTestChain(root, @event.Position);

        // MouseLeft — nodes that were hovered but no longer are (leaf→root).
        for (int i = _hoveredChain.Count - 1; i >= 0; i--)
        {
            var node = _hoveredChain[i];
            if (!newChain.Contains(node))
                node.DispatchMouseLeft(@event);
        }

        // MouseEntered — nodes under the cursor that weren't before (root→leaf).
        for (int i = 0; i < newChain.Count; i++)
        {
            var node = newChain[i];
            if (!_hoveredChain.Contains(node))
                node.DispatchMouseEntered(@event);
        }

        _hoveredChain = newChain;

        // MouseMoved — always fire from root so it propagates to all children
        root.DispatchMouseMoved(@event);
    }

    /// <summary>Clear focus entirely.</summary>
    public static void ClearFocus()
    {
        FocusedNode = null;
        ActiveNode = null;
    }

    /// <summary>
    /// Clear the hover chain, resetting <see cref="Component.IsHovered"/> on
    /// all currently-hovered elements. Does not fire MouseLeft events — host
    /// side state (e.g. React styled components) is reset by React's own
    /// unmount effect when the tree is torn down. Use when deactivating the
    /// UI (phase change) or when the tree is structurally mutated.
    /// </summary>
    public static void ClearHover()
    {
        ResetHover();
    }

    /// <summary>
    /// Silently drop the hover chain without firing events. Use during
    /// structural DOM mutations (React unmount/remount) where hovered nodes
    /// are being torn down and the chain would otherwise hold stale
    /// references to disposed components.
    /// </summary>
    public static void ResetHover()
    {
        if (_hoveredChain.Count == 0) return;

        for (int i = _hoveredChain.Count - 1; i >= 0; i--)
            _hoveredChain[i].IsHovered = false;

        _hoveredChain.Clear();
    }
}