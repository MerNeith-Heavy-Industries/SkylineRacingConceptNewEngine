using System;
using System.Collections.Generic;
using System.Text;
using ClaySharp;
using nfm_world_library.Lua;

namespace NFMWorld.ClayDom;

[LuaVisible]
public partial class ClayTextElement : ClayElementBase
{
    public override NodeType NodeType => NodeType.TextElement;

    public object? UserData; // A pointer that will be transparently passed through to the resulting render command.
    public Clay.Color TextColor; // The RGBA color of the font to render, conventionally specified as 0-255.
    public ushort FontId; // An integer transparently passed to the measure text function to identify the font to use.
    public ushort FontSize; // Controls the size of the font.
    public ushort LetterSpacing; // Controls extra horizontal spacing between characters.
    public ushort LineHeight; // Controls additional vertical space between wrapped lines of text.
    public Clay.TextElementConfigWrapMode WrapMode; // How text wraps.
    public Clay.TextAlignment TextAlignment; // How wrapped lines are horizontally aligned.

    private string _text = "";

    // TODO find a good way to cache text from children
    protected override void OnChildrenChanged()
    {
    }

    internal override void LayoutSelfAndChildren()
    {
        var sb = new StringBuilder();
        if (Children is not null)
        {
            foreach (var child in Children)
            {
                if (child is ClayTextNode textNode)
                {
                    sb.Append(textNode.Text);
                }
            }
        }
        _text = sb.ToString();
        
        Clay.Text(_text, new Clay.TextElementConfig()
        {
            UserData = UserData,
            TextColor = TextColor,
            FontId = FontId,
            FontSize = FontSize,
            LetterSpacing = LetterSpacing,
            LineHeight = LineHeight,
            WrapMode = WrapMode,
            TextAlignment = TextAlignment
        });
    }

    [LuaName]
    public override void SetProperty(string key, object value)
    {
        switch (key.Trim().ToLowerInvariant())
        {
            case "font-family":
            {
                if (UserData is CustomFontInfo fontInfo)
                {
                    fontInfo.fontFamily = ToCss(value);
                }
                else
                {
                    UserData = new CustomFontInfo()
                    {
                        fontFamily = ToCss(value)
                    };
                }
                break;
            }
            case "font-style":
            {
                if (UserData is CustomFontInfo fontInfo)
                {
                    fontInfo.fontStyle = ParseFontStyle(value);
                }
                else
                {
                    UserData = new CustomFontInfo()
                    {
                        fontStyle = ParseFontStyle(value)
                    };
                }
                break;
            }
            case "color":
                TextColor = ParseColor(value);
                break;
            case "font-size":
                FontSize = ToUshort(ParsePixels(value, "font-size"), "font-size");
                break;
            case "letter-spacing":
                LetterSpacing = ToUshort(ParsePixels(value, "letter-spacing"), "letter-spacing");
                break;
            case "line-height":
                LineHeight = ToUshort(ParsePixels(value, "line-height"), "line-height");
                break;
            case "font-id":
                FontId = ToUshort(ParseInt(value, "font-id"), "font-id");
                break;
            case "text-align":
                TextAlignment = ParseTextAlignment(value);
                break;
            case "white-space":
            case "text-wrap":
                WrapMode = ParseWrapMode(value);
                break;
            case "data":
                UserData = value;
                break;
            default:
                throw new ArgumentException($"Unsupported CSS property '{key}'.");
        }
    }
}