using NFMWorld.DriverInterface.DriverInterface;

namespace NFMWorld.Reactor;

public struct TextStyles()
{
    /// <summary>
    /// Sets the fill color of the text. The default value is white.
    /// </summary>
    public Color ForegroundColor = new Color(255, 255, 255);
    
    /// <summary>
    /// Sets the stroke color of the text. Or set to null to disable the stroke.
    /// </summary>
    public Color? StrokeColor;
    
    /// <summary>
    /// Gets or sets the font family.
    /// </summary>
    public FontFamily FontFamily = FontFamily.DroidSans;
    
    /// <summary>
    /// Gets or sets the font size.
    /// </summary>
    public float FontSize = 12f;
    
    /// <summary>
    /// Gets or sets the font style.
    /// </summary>
    public FontStyle FontStyle = FontStyle.Plain;
    public BreakType BreakType = BreakType.Word;
    public OverflowBehavior OverflowBehavior = OverflowBehavior.Stretch;
    
    /// <summary>
    /// Sets the horizontal alignment of the text. The default value is <see cref="TextHorizontalAlignment.Left"/>.
    /// </summary>
    public TextHorizontalAlignment HorizontalAlignment = TextHorizontalAlignment.Left;
    
    /// <summary>
    /// Sets the vertical alignment of the text. The default value is <see cref="TextVerticalAlignment.Top"/>.
    /// </summary>
    public TextVerticalAlignment VerticalAlignment = TextVerticalAlignment.Top;

}