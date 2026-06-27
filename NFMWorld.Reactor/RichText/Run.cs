namespace NFMWorld.Reactor;

/// <summary>
/// A terminal element in text flow hierarchy - contains a uniformatted run of unicode characters
/// </summary>
public partial class Run : TextElement, IRichTextLeaf
{
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
    public string Text { get; set; }
}