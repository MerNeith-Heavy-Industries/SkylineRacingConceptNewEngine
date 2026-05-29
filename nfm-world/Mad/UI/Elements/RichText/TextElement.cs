using NFMWorld.Util;
using WorldXaml.UI.Base;

namespace NFMWorld.UI;

/// <summary>
/// TextElement is an  base class for content in text based controls.
/// TextElements span other content, applying property values or providing structural information.
/// </summary>
public abstract partial class TextElement : BindableObject, IRichTextElement
{
    /// <summary>
    /// Gets or sets a brush used to paint the control's background.
    /// </summary>
    [Property]
    public partial Color? Background { get; set; }

    /// <summary>
    /// Gets or sets the font family.
    /// </summary>
    [Property]
    public partial FontFamily? FontFamily { get; set; }

    /// <summary>
    /// Gets or sets the font size.
    /// </summary>
    [Property]
    public partial float? FontSize { get; set; }

    /// <summary>
    /// Gets or sets the font style.
    /// </summary>
    [Property]
    public partial FontStyle? FontStyle { get; set; }
    
    /// <summary>
    /// Gets or sets a brush used to paint the text.
    /// </summary>
    [Property]
    public partial Color? Foreground { get; set; }
    
    [Property]
    public partial Color? Stroke { get; set; }
}