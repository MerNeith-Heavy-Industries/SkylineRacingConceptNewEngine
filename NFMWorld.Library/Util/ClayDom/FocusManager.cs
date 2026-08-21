namespace NFMWorld.ClayDom;

public static class FocusManager
{
    public static ClayElementBase? FocusedElement { get; set; }
    
    /// <summary>Move focus to the next focusable element in Tab order.</summary>
    public static bool FocusNext(ClayElementBase root)
    {
        var all = GetFocusableDescendants(root).ToArray();
        if (all.Length == 0) return false;

        var idx = FocusedElement is not null
            ? all.IndexOf(FocusedElement)
            : -1;

        var next = (idx + 1) % all.Length;
        FocusedElement = all[next];
        return true;
    }

    /// <summary>Move focus to the previous focusable element.</summary>
    public static bool FocusPrev(ClayElementBase root)
    {
        var all = GetFocusableDescendants(root).ToArray();
        if (all.Length == 0) return false;

        var idx = FocusedElement is not null
            ? all.IndexOf(FocusedElement)
            : all.Length;

        var prev = (idx - 1 + all.Length) % all.Length;
        FocusedElement = all[prev];
        return true;
    }

    /// <summary>
    /// Depth-first enumeration of all focusable Nodes under a root.
    /// Respects Visibility (skips Hidden/Collapsed subtrees).
    /// </summary>
    private static IEnumerable<ClayElementBase> GetFocusableDescendants(ClayElementBase root)
    {
        if (root.Children != null)
        {
            foreach (var child in root.Children)
            {
                if (child is not ClayElementBase element || !element.IsDisplayed)
                    continue;

                if (element.IsFocusable)
                    yield return element;

                foreach (var descendant in GetFocusableDescendants(element))
                    yield return descendant;
            }
        }
    }


}