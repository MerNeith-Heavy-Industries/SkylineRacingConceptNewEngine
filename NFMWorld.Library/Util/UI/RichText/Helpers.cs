using Microsoft.Xna.Framework;
using NFMWorld.DriverInterface;
using NFMWorld.DriverInterface.DriverInterface;

namespace NFMWorld.Reactor;

public static partial class Nodes
{
    public static Run Run(
        string? text,
        Color? background = null,
        FontFamily? fontFamily = null,
        float? fontSize = null,
        FontStyle? fontStyle = null,
        Color? foreground = null,
        Color? stroke = null)
    {
        return new Run(text);
    }
    
    public static Span Span(params ReadOnlySpan<IRichTextElement> children)
    {
        var span = new Span();
        foreach (var child in children)
        {
            span.Add(child);
        }
        return span;
    }
    
    public static Span Span(params ReadOnlySpan<string> children)
    {
        var span = new Span();
        foreach (var child in children)
        {
            span.Add(child);
        }
        return span;
    }
}