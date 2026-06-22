// ReSharper disable once CheckNamespace
namespace WorldXaml.UI;

/// <summary>
/// Holds the event arguments for the <see cref="ILogical.AttachedToLogicalTree"/> and 
/// <see cref="ILogical.DetachedFromLogicalTree"/> events.
/// </summary>
/// <param name="root">The root of the logical tree.</param>
/// <param name="source">The control being attached/detached.</param>
/// <param name="parent">The <see cref="Parent"/>.</param>
public class VisualTreeAttachmentEventArgs(
    IVisualRoot root,
    Visual source,
    Visual? parent)
    : EventArgs
 {
    /// <summary>
    /// Gets the root of the logical tree that the control is being attached to or detached from.
    /// </summary>
    public IVisualRoot Root { get; } = root ?? throw new ArgumentNullException(nameof(root));

    /// <summary>
    /// Gets the control that was attached or detached from the logical tree.
    /// </summary>
    /// <remarks>
    /// Logical tree attachment events travel down the attached logical tree from the point of
    /// attachment/detachment, so this control may be different from the control that the
    /// event is being raised on.
    /// </remarks>
    public Visual Source { get; } = source ?? throw new ArgumentNullException(nameof(source));

    /// <summary>
    /// Gets the control that <see cref="Source"/> is being attached to or detached from.
    /// </summary>
    /// <remarks>
    /// For logical tree attachment, holds the new logical parent of <see cref="Source"/>. For
    /// detachment, holds the old logical parent of <see cref="Source"/>. If the detachment event
    /// was caused by a top-level control being closed, then this property will be null.
    /// </remarks>
    public Visual? Parent { get; } = parent;
}
