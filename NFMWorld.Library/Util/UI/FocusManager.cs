using NFMWorld.ClayDom.Events;
using NFMWorldLibrary;
using NFMWorldLibrary.Util;

namespace NFMWorld.Reactor;

public static class FocusManager
{
    public static Node? FocusedNode { get; set; }
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
    /// Hit-tests and returns the full ancestor chain (root→leaf) of focusable
    /// elements at <paramref name="screenPos"/>. Empty when nothing hit.
    /// </summary>
    public static List<Component> HitTestChain(Component root, LuaVector2 screenPos)
    {
        var chain = new List<Component>();
        HitTestChainRecursive(root, screenPos, chain);
        return chain;
    }

    private static bool HitTestChainRecursive(Component node, LuaVector2 pos, List<Component> chain)
    {
        var selfHit = false;

        if (node.IsFocusable)
        {
            var bounds = new RectangleF(
                node.FocusOrigin.X, node.FocusOrigin.Y,
                node.FocusSize.X, node.FocusSize.Y);

            if (bounds.Contains(pos.X, pos.Y))
            {
                chain.Add(node);       // ancestor added before children
                selfHit = true;
            }
        }

        // If self isn't hit, children can't be hit either (they're contained within self).
        // Exception: non-focusable containers — they pass through even though selfHit=false.
        if (!selfHit && node.IsFocusable)
            return false;

        var children = node.VisualChildren
            .OfType<Component>()
            .OrderBy(c => c.TabOrder)
            .Reverse();

        foreach (var visual in children)
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
    /// Hit-tests under the cursor, diffs against the previous ancestor chain,
    /// and dispatches MouseEntered / MouseLeft / MouseMoved as appropriate.
    /// Call once per frame from your MouseMoved handler.
    /// </summary>
    public static void DispatchMouseMove(Component root, BaseMouseMoveEvent @event)
    {
        var newChain = HitTestChain(root, @event.Position);

        // Find divergence index — first position where chains differ
        int diverge = 0;
        while (diverge < _hoveredChain.Count && diverge < newChain.Count
               && _hoveredChain[diverge] == newChain[diverge])
            diverge++;

        // MouseLeft — fire on old chain from leaf up to (not including) divergence
        for (int i = _hoveredChain.Count - 1; i >= diverge; i--)
            _hoveredChain[i].DispatchMouseLeft(@event);

        // MouseEntered — fire on new chain from divergence down to leaf
        for (int i = diverge; i < newChain.Count; i++)
        {
            Logging.Info(
                $"[FocusManager] MouseEntered chain[{i}]={newChain[i].GetType().Name} " +
                $"IsFocusable={newChain[i].IsFocusable} " +
                $"FocusOrigin={newChain[i].FocusOrigin} FocusSize={newChain[i].FocusSize}");
            newChain[i].DispatchMouseEntered(@event);
        }

        var oldLeaf = _hoveredChain.Count > 0 ? _hoveredChain[^1] : null;
        var newLeaf = newChain.Count > 0 ? newChain[^1] : null;

        _hoveredChain = newChain;

        // if (oldLeaf != newLeaf)
        //     HoveredChanged?.Invoke(newLeaf);

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
    /// Clear the hover chain, resetting <see cref="Component.IsHovered"/>
    /// on all currently-hovered elements.
    /// </summary>
    public static void ClearHover()
    {
        if (_hoveredChain.Count == 0) return;

        // Walk leaf→root so MouseLeft propagates naturally
        for (int i = _hoveredChain.Count - 1; i >= 0; i--)
            _hoveredChain[i].IsHovered = false;

        _hoveredChain.Clear();
        // HoveredChanged?.Invoke(null);
    }
}