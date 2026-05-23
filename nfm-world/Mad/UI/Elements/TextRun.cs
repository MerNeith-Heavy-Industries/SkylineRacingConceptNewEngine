using System.Diagnostics.CodeAnalysis;
using Avalonia;
using Avalonia.Metadata;
using NFMWorld.DriverInterface;
using NFMWorld.Util;
using WorldXaml.UI.Base;
using WorldXaml.UI.Yoga;

namespace NFMWorld.UI;

public partial class TextRun : Node
{
    private IFontMetrics? _fontMetrics;

    public static StyledProperty<Color> ColorProperty { get; } = AvaloniaProperty.Register<TextRun, Color>(
        nameof(Color),
        defaultValue: new Color(255, 255, 255));

    [Property]
    public partial Color Color { get; set; }
    
    [Property]
    public partial Color? StrokeColor { get; set; }
    
    public static StyledProperty<Font> FontProperty { get; } = AvaloniaProperty.Register<TextRun, Font>(
        nameof(Font),
        defaultValue: new Font(FontFamily.DroidSans, FontStyle.Plain, 18),
        onChanged: (run, font) =>
        {
            run.SetFontMetrics();
            run.RelayoutText();
        });

    [Property]
    public partial Font Font { get; set; }
    
    public static StyledProperty<string> TextProperty { get; } = AvaloniaProperty.Register<TextRun, string>(
        nameof(Text),
        defaultValue: string.Empty,
        onChanged: (run, text) => run.RelayoutText());

    [Content]
    [Property]
    public partial string? Text { get; set; }

    [MemberNotNull(nameof(_fontMetrics))]
    private void SetFontMetrics()
    {
        G.SetFont(Font with { Size = Font.Size }); // Does not use scale here
        _fontMetrics = G.GetFontMetrics();
    }

    private void RelayoutText()
    {
        if (_fontMetrics == null)
        {
            SetFontMetrics();
        }
        Width = _fontMetrics!.StringWidth(Text ?? string.Empty);
        Height = _fontMetrics!.Height(Text ?? string.Empty);
    }

    [Property(defaultValue: TextHorizontalAlignment.Left)]
    public partial TextHorizontalAlignment HorizontalAlignment { get; set; }

    [Property(defaultValue: TextVerticalAlignment.Top)]
    public partial TextVerticalAlignment VerticalAlignment { get; set; }

    protected override void RenderContent(System.Numerics.Vector2 position, System.Numerics.Vector2 size)
    {
        base.RenderContent(position, size);

        if (string.IsNullOrEmpty(Text))
        {
            return;
        }
        
        G.SetFont(Font with { Size = Font.Size * G.Scale });
        if (StrokeColor != null)
        {
            G.SetColor((Color)StrokeColor);
            G.DrawStringStrokeAligned(Text, (int)position.X, (int)position.Y, (int)size.X, (int)size.Y, HorizontalAlignment, VerticalAlignment);
        }

        G.SetColor(Color);
        G.DrawStringAligned(Text, (int)position.X, (int)position.Y, (int)size.X, (int)size.Y, HorizontalAlignment, VerticalAlignment);
    }
}