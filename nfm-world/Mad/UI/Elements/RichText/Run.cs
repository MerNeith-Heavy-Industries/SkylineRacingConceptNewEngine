using Avalonia.Data;
using Avalonia.Metadata;
using WorldXaml.UI.Base;

namespace NFMWorld.UI;

/// <summary>
/// A terminal element in text flow hierarchy - contains a uniformatted run of unicode characters
/// </summary>
public partial class Run : Inline, IRichTextLeaf
{
    public override IReadOnlyList<IInline> LogicalChildren => [];

    /// <summary>
    /// Initializes an instance of Run class.
    /// </summary>
    public Run()
    {
        Text = string.Empty;
    }

    /// <summary>
    /// Initializes an instance of Run class specifying its text content.
    /// </summary>
    /// <param name="text">
    /// Text content assigned to the Run.
    /// </param>
    public Run(string? text)
    {
        Text = text ?? string.Empty;
    }

    /// <summary>
    /// The content spanned by this TextElement.
    /// </summary>
    [Content]
    [Property(DefaultMode = BindingMode.TwoWay, OnChangedMethod = nameof(OnTextChanged))]
    public partial string Text { get; set; }
    
    private partial void OnTextChanged(string newText)
    {
        Host?.Invalidate();
    }
}