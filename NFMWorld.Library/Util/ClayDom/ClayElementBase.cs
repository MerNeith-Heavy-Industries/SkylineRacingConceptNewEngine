using System.Diagnostics;
using System.Globalization;
using ClaySharp;
using Lua;
using nfm_world_library.Lua;
using NFMWorld.ClayDom.Events;
using NFMWorld.DriverInterface.DriverInterface;
using NFMWorld.LuaSourceGenerator.Generator;
using NFMWorldLibrary;

namespace NFMWorld.ClayDom;

public abstract partial class ClayElementBase : ClayNode, ILuaUserData
{
    private protected static uint MaxId;

    public uint Id = MaxId++;
    public string DebugName = string.Empty;

    public List<ClayNode>? Children = null;

    // id is the only thing that matter, stringId is shown in DebugView
    public Clay.ElementId ElementId => new Clay.ElementId()
    {
        Id = Id,
        StringId = DebugName
    };
    
    public bool IsHovered { get; set; }
    public bool IsFocusable { get; set; }
    
    public bool IsDisplayed { get; set; }

    public bool IsFocused
    {
        get => ReferenceEquals(FocusManager.FocusedElement, this);
        set
        {
            if (value)
            {
                FocusManager.FocusedElement = this;
            }
            else if (ReferenceEquals(FocusManager.FocusedElement, this))
            {
                FocusManager.FocusedElement = null;
            }
        }
    }
    public bool IsActive { get; set; }

    public int TabOrder { get; set; }

    public Action<MouseEvent>? MousePressed { get; set; }

    public Action<MouseEvent>? MouseReleased { get; set; }
    
    public Action<MouseDragEvent>? MouseDragged { get; set; }
    
    public Action<MouseWheelEvent>? MouseScrolled { get; set; }
    
    public Action<MouseMoveEvent>? MouseMoved { get; set; }
    
    public Action<MouseMoveEvent>? MouseEntered { get; set; }
    
    public Action<MouseMoveEvent>? MouseLeft { get; set; }
    
    public Action<KeyboardTypingEvent>? KeyTyped { get; set; }

    public Action<KeyboardEvent>? KeyPressed { get; set; }

    public Action<KeyboardEvent>? KeyReleased { get; set; }

    // Reusable snapshot buffer so dispatch methods don't allocate a new list
    // every time VisualChildren is iterated. Allocated once per Visual, cleared
    // and repopulated on each use.
    private List<ClayNode>? _childSnapshot;

    private protected IReadOnlyList<ClayNode> GetChildSnapshot()
    {
        if (Children is null) return [];
        var list = _childSnapshot ??= [];
        list.Clear();
        list.AddRange(Children);
        return list;
    }

    #region Child tree
    
    protected virtual void OnChildrenChanged()
    {
    }

    public void AppendChild(ClayNode node)
    {
        (Children ??= []).Add(node);
        OnChildrenChanged();
    }

    public void InsertBefore(ClayNode node, ClayNode beforeNode)
    {
        if (Children?.IndexOf(beforeNode) is { } idx and > -1)
        {
            Children.Insert(idx, node);
        }
        else
        {
            AppendChild(node);
        }
        OnChildrenChanged();
    }

    public void RemoveChild(ClayNode node)
    {
        if (Children?.Remove(node) == true)
        {
            OnChildrenChanged();
        }
    }
    
    #endregion

    #region Props

    public Clay.ElementDeclaration ElementDeclaration;
    public Clay.TextElementConfig TextConfig;

    internal virtual void LayoutSelfAndChildren()
    {
    }

    public void SetLuaProperty(string key, LuaValue value, LuaState state)
    {
        switch (key.ToLowerInvariant())
        {
            // ---- Clay-specific ----
            case "image":
                ElementDeclaration.Image.ImageData = ToCss(value);
                break;
            case "name":
                DebugName = ToCss(value);
                break;
            case "data":
                ElementDeclaration.UserData = value;
                break;
            case "style":
                if (value.TryRead<LuaTable>(out var table))
                {
                    ComputeStyles(table);
                }
                break;
            case "onmouseenter":
            {
                if (value.TryRead<LuaFunction>(out var func))
                {
                    MouseEntered = @event =>
                    {
                        state.Call(func, [@event]);
                    };
                }

                break;
            }
            case "onmouseleave":
            {
                if (value.TryRead<LuaFunction>(out var func))
                {
                    MouseLeft = @event =>
                    {
                        state.Call(func, [@event]);
                    };
                }

                break;
            }
            case "onmousemove":
            {
                if (value.TryRead<LuaFunction>(out var func))
                {
                    MouseMoved = @event =>
                    {
                        state.Call(func, [@event]);
                    };
                }

                break;
            }
            case "onmousedrag":
            {
                if (value.TryRead<LuaFunction>(out var func))
                {
                    MouseDragged = @event =>
                    {
                        state.Call(func, [@event]);
                    };
                }

                break;
            }
            case "onmousedown":
            {
                if (value.TryRead<LuaFunction>(out var func))
                {
                    MousePressed = @event =>
                    {
                        state.Call(func, [@event]);
                    };
                }

                break;
            }
            case "onmouseup":
            {
                if (value.TryRead<LuaFunction>(out var func))
                {
                    MouseReleased = @event =>
                    {
                        state.Call(func, [@event]);
                    };
                }

                break;
            }
            case "onmousescroll":
            {
                if (value.TryRead<LuaFunction>(out var func))
                {
                    MouseScrolled = @event =>
                    {
                        state.Call(func, [@event]);
                    };
                }

                break;
            }
            case "onkeytype":
            {
                if (value.TryRead<LuaFunction>(out var func))
                {
                    KeyTyped = @event =>
                    {
                        state.Call(func, [@event]);
                    };
                }

                break;
            }
            case "onkeydown":
            {
                if (value.TryRead<LuaFunction>(out var func))
                {
                    KeyPressed = @event =>
                    {
                        state.Call(func, [@event]);
                    };
                }

                break;
            }
            case "onkeyup":
            {
                if (value.TryRead<LuaFunction>(out var func))
                {
                    KeyReleased = @event =>
                    {
                        state.Call(func, [@event]);
                    };
                }

                break;
            }
        }
        
        void ComputeStyles(IEnumerable<KeyValuePair<LuaValue, LuaValue>> table)
        {
            foreach (var (keyValue, value) in table)
            {
                var key = keyValue.Read<string>();
                switch (key)
                {
                    // ---- Sizing ----
                    case "width":
                        ElementDeclaration.Layout.Sizing.Width = ParseSizing(value, "width");
                        break;
                    case "height":
                        ElementDeclaration.Layout.Sizing.Height = ParseSizing(value, "height");
                        break;
                    case "min-width":
                        ElementDeclaration.Layout.Sizing.Width.MinMax.Min = ParsePixels(value, "min-width");
                        break;
                    case "max-width":
                        ElementDeclaration.Layout.Sizing.Width.MinMax.Max = ParsePixels(value, "max-width");
                        break;
                    case "min-height":
                        ElementDeclaration.Layout.Sizing.Height.MinMax.Min = ParsePixels(value, "min-height");
                        break;
                    case "max-height":
                        ElementDeclaration.Layout.Sizing.Height.MinMax.Max = ParsePixels(value, "max-height");
                        break;

                    // ---- Box model ----
                    case "padding":
                    {
                        var (t, r, b, l) = ParseEdgeValues(value, "padding");
                        ElementDeclaration.Layout.Padding = new Clay.Padding
                        {
                            Top = ToUshort(t, "padding"),
                            Right = ToUshort(r, "padding"),
                            Bottom = ToUshort(b, "padding"),
                            Left = ToUshort(l, "padding"),
                        };
                        break;
                    }
                    case "padding-top":
                        ElementDeclaration.Layout.Padding.Top = ToUshort(ParsePixels(value, "padding-top"), "padding-top");
                        break;
                    case "padding-right":
                        ElementDeclaration.Layout.Padding.Right = ToUshort(ParsePixels(value, "padding-right"), "padding-right");
                        break;
                    case "padding-bottom":
                        ElementDeclaration.Layout.Padding.Bottom = ToUshort(ParsePixels(value, "padding-bottom"), "padding-bottom");
                        break;
                    case "padding-left":
                        ElementDeclaration.Layout.Padding.Left = ToUshort(ParsePixels(value, "padding-left"), "padding-left");
                        break;
                    case "gap":
                    case "column-gap":
                    case "row-gap":
                        ElementDeclaration.Layout.ChildGap = ToUshort(ParsePixels(value, key), key);
                        break;
                    case "margin":
                    case "margin-top":
                    case "margin-right":
                    case "margin-bottom":
                    case "margin-left":
                        throw new ArgumentException($"Clay has no equivalent for '{key}' (no margin concept - use padding or gap instead).");

                    // ---- Flex Layout ----
                    case "flex-direction":
                        ElementDeclaration.Layout.LayoutDirection = ParseDirection(value);
                        break;
                    case "align-items":
                    {
                        // align-items = cross axis. row → y, column → x.
                        if (ElementDeclaration.Layout.LayoutDirection == Clay.LayoutDirection.LeftToRight)
                            ElementDeclaration.Layout.ChildAlignment.Y = ParseAlignY(value, "align-items");
                        else
                            ElementDeclaration.Layout.ChildAlignment.X = ParseAlignX(value, "align-items");
                        break;
                    }
                    case "justify-content":
                    {
                        // justify-content = main axis. row → x, column → y.
                        if (ElementDeclaration.Layout.LayoutDirection == Clay.LayoutDirection.LeftToRight)
                            ElementDeclaration.Layout.ChildAlignment.X = ParseAlignX(value, "justify-content");
                        else
                            ElementDeclaration.Layout.ChildAlignment.Y = ParseAlignY(value, "justify-content");
                        break;
                    }

                    // ---- Color ----
                    case "background-color":
                        ElementDeclaration.BackgroundColor = ParseColor(value);
                        break;
                    case "overlay-color":
                        ElementDeclaration.OverlayColor = ParseColor(value);
                        break;

                    // ---- Border ----
                    case "border":
                        ApplyBorderShorthand(value);
                        break;
                    case "border-width":
                    {
                        var (t, r, b, l) = ParseEdgeValues(value, "border-width");
                        ElementDeclaration.Border.Width = new Clay.BorderWidth
                        {
                            Top = ToUshort(t, "border-width"),
                            Right = ToUshort(r, "border-width"),
                            Bottom = ToUshort(b, "border-width"),
                            Left = ToUshort(l, "border-width"),
                            BetweenChildren = 0,
                        };
                        break;
                    }
                    case "border-top-width":
                        ElementDeclaration.Border.Width.Top = ToUshort(ParsePixels(value, "border-top-width"), "border-top-width");
                        break;
                    case "border-right-width":
                        ElementDeclaration.Border.Width.Right = ToUshort(ParsePixels(value, "border-right-width"), "border-right-width");
                        break;
                    case "border-bottom-width":
                        ElementDeclaration.Border.Width.Bottom = ToUshort(ParsePixels(value, "border-bottom-width"), "border-bottom-width");
                        break;
                    case "border-left-width":
                        ElementDeclaration.Border.Width.Left = ToUshort(ParsePixels(value, "border-left-width"), "border-left-width");
                        break;
                    case "border-color":
                        ElementDeclaration.Border.Color = ParseColor(value);
                        break;
                    case "border-style":
                        if (!ToCss(value).Trim().Equals("solid", StringComparison.OrdinalIgnoreCase))
                            throw new ArgumentException($"Unsupported border-style '{value}' (Clay supports 'solid' only).");
                        break;

                    // ---- Corner radius ----
                    case "border-radius":
                    {
                        var (t, r, b, l) = ParseEdgeValues(value, key);
                        ElementDeclaration.CornerRadius = new Clay.CornerRadiusValues
                        {
                            TopLeft = t,
                            TopRight = r,
                            BottomRight = b,
                            BottomLeft = l,
                        };
                        break;
                    }
                    case "border-top-left-radius":
                        ElementDeclaration.CornerRadius.TopLeft = ParsePixels(value, "border-top-left-radius");
                        break;
                    case "border-top-right-radius":
                        ElementDeclaration.CornerRadius.TopRight = ParsePixels(value, "border-top-right-radius");
                        break;
                    case "border-bottom-right-radius":
                        ElementDeclaration.CornerRadius.BottomRight = ParsePixels(value, "border-bottom-right-radius");
                        break;
                    case "border-bottom-left-radius":
                        ElementDeclaration.CornerRadius.BottomLeft = ParsePixels(value, "border-bottom-left-radius");
                        break;

                    // ---- Positioning / floating ----
                    case "position":
                    {
                        switch (ToCss(value).Trim().ToLowerInvariant())
                        {
                            case "static":
                            case "relative":
                            case "initial":
                                ElementDeclaration.Floating.AttachTo = Clay.FloatingAttachToElement.None;
                                break;
                            case "absolute":
                                ElementDeclaration.Floating.AttachTo = Clay.FloatingAttachToElement.Parent;
                                break;
                            case "fixed":
                                ElementDeclaration.Floating.AttachTo = Clay.FloatingAttachToElement.Root;
                                break;
                            default:
                                throw new ArgumentException($"Unsupported 'position' value: '{value}'.");
                        }
                        break;
                    }
                    case "top":
                        EnsureFloating();
                        SetAttachY(Clay.FloatingAttachPointType.LeftTop);
                        ElementDeclaration.Floating.Offset.Y = ParsePixels(value, "top");
                        break;
                    case "bottom":
                        EnsureFloating();
                        SetAttachY(Clay.FloatingAttachPointType.LeftBottom);
                        ElementDeclaration.Floating.Offset.Y = -ParsePixels(value, "bottom");
                        break;
                    case "left":
                        EnsureFloating();
                        SetAttachX(Clay.FloatingAttachPointType.LeftTop);
                        ElementDeclaration.Floating.Offset.X = ParsePixels(value, "left");
                        break;
                    case "right":
                        EnsureFloating();
                        SetAttachX(Clay.FloatingAttachPointType.RightTop);
                        ElementDeclaration.Floating.Offset.X = -ParsePixels(value, "right");
                        break;
                    case "z-index":
                        ElementDeclaration.Floating.ZIndex = (short)ParseInt(value, "z-index");
                        break;
                    case "aspect-ratio":
                        ElementDeclaration.AspectRatio.AspectRatio = ParseAspectRatio(value);
                        break;

                    // ---- Clip / scroll ----
                    case "overflow":
                    {
                        switch (ToCss(value).Trim().ToLowerInvariant())
                        {
                            case "visible":
                                ElementDeclaration.Clip.Horizontal = false;
                                ElementDeclaration.Clip.Vertical = false;
                                break;
                            case "hidden":
                            case "clip":
                            case "scroll":
                            case "auto":
                                ElementDeclaration.Clip.Horizontal = true;
                                ElementDeclaration.Clip.Vertical = true;
                                break;
                            default:
                                throw new ArgumentException($"Unsupported 'overflow' value: '{value}'.");
                        }
                        break;
                    }
                    case "overflow-x":
                        ElementDeclaration.Clip.Horizontal = ParseOverflowAxis(value, "overflow-x");
                        break;
                    case "overflow-y":
                        ElementDeclaration.Clip.Vertical = ParseOverflowAxis(value, "overflow-y");
                        break;

                    // ---- Transitions ----
                    case "transition-duration":
                        ElementDeclaration.Transition.Duration = ParseDuration(value);
                        break;
                    case "transition-property":
                        ElementDeclaration.Transition.Properties = ParseTransitionProperty(value);
                        break;

                    // ---- Text ----

                    case "font-family":
                    {
                        if (TextConfig.UserData is CustomFontInfo fontInfo)
                        {
                            fontInfo.fontFamily = ToCss(value);
                        }
                        else
                        {
                            TextConfig.UserData = new CustomFontInfo()
                            {
                                fontFamily = ToCss(value)
                            };
                        }
                        break;
                    }
                    case "font-style":
                    {
                        if (TextConfig.UserData is CustomFontInfo fontInfo)
                        {
                            fontInfo.fontStyle = ParseFontStyle(value);
                        }
                        else
                        {
                            TextConfig.UserData = new CustomFontInfo()
                            {
                                fontStyle = ParseFontStyle(value)
                            };
                        }
                        break;
                    }
                    case "color":
                        TextConfig.TextColor = ParseColor(value);
                        break;
                    case "font-size":
                        TextConfig.FontSize = ToUshort(ParsePixels(value, "font-size"), "font-size");
                        break;
                    case "letter-spacing":
                        TextConfig.LetterSpacing = ToUshort(ParsePixels(value, "letter-spacing"), "letter-spacing");
                        break;
                    case "line-height":
                        TextConfig.LineHeight = ToUshort(ParsePixels(value, "line-height"), "line-height");
                        break;
                    case "font-id":
                        TextConfig.FontId = ToUshort(ParseInt(value, "font-id"), "font-id");
                        break;
                    case "text-align":
                        TextConfig.TextAlignment = ParseTextAlignment(value);
                        break;
                    case "white-space":
                    case "text-wrap":
                        TextConfig.WrapMode = ParseWrapMode(value);
                        break;
                }
            }
        }

        void EnsureFloating()
        {
            if (ElementDeclaration.Floating.AttachTo == Clay.FloatingAttachToElement.None)
                ElementDeclaration.Floating.AttachTo = Clay.FloatingAttachToElement.Parent;
        }

        // Sets the horizontal axis of the floating attach points, preserving the vertical axis.
        void SetAttachX(Clay.FloatingAttachPointType xTemplate)
        {
            ref var p = ref ElementDeclaration.Floating.AttachPoints;
            p.Parent = WithXAxis(p.Parent, xTemplate);
            p.Element = WithXAxis(p.Element, xTemplate);
        }

        // Sets the vertical axis of the floating attach points, preserving the horizontal axis.
        void SetAttachY(Clay.FloatingAttachPointType yTemplate)
        {
            ref var p = ref ElementDeclaration.Floating.AttachPoints;
            p.Parent = WithYAxis(p.Parent, yTemplate);
            p.Element = WithYAxis(p.Element, yTemplate);
        }

        void ApplyBorderShorthand(object value)
        {
            string s = ToCss(value).Trim();
            string[] tokens = s.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (tokens.Length == 0)
                throw new ArgumentException("Empty 'border' value.");

            foreach (string token in tokens)
            {
                if (TryParseBorderWidth(token, out ushort w))
                {
                    ElementDeclaration.Border.Width = new Clay.BorderWidth { Top = w, Right = w, Bottom = w, Left = w, BetweenChildren = 0 };
                }
                else if (IsColorToken(token))
                {
                    ElementDeclaration.Border.Color = ParseColor(token);
                }
                else
                {
                    switch (token.ToLowerInvariant())
                    {
                        case "solid":
                            break; // Clay only renders solid borders; accepted and ignored.
                        default:
                            throw new ArgumentException($"Unsupported border-style '{token}' (Clay supports 'solid' only).");
                    }
                }
            }
        }
    }

    #region Parsers
    
    private static (float top, float right, float bottom, float left) ParseEdgeValues(object value, string prop)
    {
        string s = ToCss(value).Trim();
        string[] parts = s.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        return parts.Length switch
        {
            1 => (P(0), P(0), P(0), P(0)),
            2 => (P(0), P(1), P(0), P(1)),
            3 => (P(0), P(1), P(2), P(1)),
            4 => (P(0), P(1), P(2), P(3)),
            _ => throw new ArgumentException($"'{prop}' expects 1-4 values, got {parts.Length}: '{s}'."),
        };
        float P(int i) => ParsePixels(parts[i], prop);
    }

    // FloatingAttachPointType is a 3x3 grid encoded as (xAxis * 3 + yAxis):
    // xAxis 0=Left, 1=Center, 2=Right; yAxis 0=Top, 1=Center, 2=Bottom.
    // These helpers replace one axis while preserving the other, so that e.g.
    // `left` + `bottom` combine into LeftBottom.
    private static Clay.FloatingAttachPointType WithXAxis(Clay.FloatingAttachPointType p, Clay.FloatingAttachPointType xTemplate)
        => (Clay.FloatingAttachPointType)(((int)xTemplate / 3) * 3 + ((int)p % 3));

    private static Clay.FloatingAttachPointType WithYAxis(Clay.FloatingAttachPointType p, Clay.FloatingAttachPointType yTemplate)
        => (Clay.FloatingAttachPointType)(((int)p / 3) * 3 + ((int)yTemplate % 3));

    private static Clay.SizingAxis ParseSizing(object value, string prop)
    {
        string s = ToCss(value).Trim();
        if (s.Length == 0)
            throw new ArgumentException($"Empty value for '{prop}'.");

        if (s.EndsWith('%'))
        {
            float pct = float.Parse(s[..^1], NumberStyles.Float, CultureInfo.InvariantCulture) / 100f;
            return Clay.SizingPercent(pct);
        }

        switch (s.ToLowerInvariant())
        {
            case "auto":
            case "fit-content":
            case "fit":
                return Clay.SizingFit();
            case "grow":
            case "fill":
                return Clay.SizingGrow();
        }

        return Clay.SizingFixed(ParsePixels(s, prop));
    }

    private static Clay.LayoutDirection ParseDirection(object value) =>
        ToCss(value).Trim().ToLowerInvariant() switch
        {
            "row" => Clay.LayoutDirection.LeftToRight,
            "column" => Clay.LayoutDirection.TopToBottom,
            _ => throw new ArgumentException($"Unsupported 'flex-direction' value: '{value}'. Clay supports 'row' or 'column' only (no reverse/wrap)."),
        };

    private static Clay.LayoutAlignmentX ParseAlignX(object value, string prop) =>
        ToCss(value).Trim().ToLowerInvariant() switch
        {
            "flex-start" or "start" or "left" => Clay.LayoutAlignmentX.Left,
            "center" => Clay.LayoutAlignmentX.Center,
            "flex-end" or "end" or "right" => Clay.LayoutAlignmentX.Right,
            _ => throw new ArgumentException($"Unsupported '{prop}' value: '{value}'. Clay supports flex-start/center/flex-end (or left/center/right)."),
        };

    private static Clay.LayoutAlignmentY ParseAlignY(object value, string prop) =>
        ToCss(value).Trim().ToLowerInvariant() switch
        {
            "flex-start" or "start" or "top" => Clay.LayoutAlignmentY.Top,
            "center" => Clay.LayoutAlignmentY.Center,
            "flex-end" or "end" or "bottom" => Clay.LayoutAlignmentY.Bottom,
            _ => throw new ArgumentException($"Unsupported '{prop}' value: '{value}'. Clay supports flex-start/center/flex-end (or top/center/bottom)."),
        };

    private static bool ParseOverflowAxis(object value, string prop) =>
        ToCss(value).Trim().ToLowerInvariant() switch
        {
            "visible" => false,
            "hidden" or "clip" or "scroll" or "auto" => true,
            _ => throw new ArgumentException($"Unsupported '{prop}' value: '{value}'."),
        };

    private static float ParseAspectRatio(object value)
    {
        string s = ToCss(value).Trim();
        var span = s.AsSpan();
        int slash = span.IndexOf('/');
        if (slash < 0)
            return float.Parse(span, NumberStyles.Float, CultureInfo.InvariantCulture);

        float w = float.Parse(span[..slash], NumberStyles.Float, CultureInfo.InvariantCulture);
        float h = float.Parse(span[(slash + 1)..], NumberStyles.Float, CultureInfo.InvariantCulture);
        if (h == 0f)
            throw new ArgumentException($"Invalid aspect-ratio: '{value}' (height is zero).");
        return w / h;
    }

    private static float ParseDuration(object value)
    {
        string s = ToCss(value).Trim();
        var span = s.AsSpan();
        float scale = 1f;
        if (span.EndsWith("ms", StringComparison.OrdinalIgnoreCase))
        {
            span = span[..^2];
            scale = 0.001f;
        }
        else if (span.EndsWith("s", StringComparison.OrdinalIgnoreCase))
        {
            span = span[..^1];
        }
        return float.Parse(span, NumberStyles.Float, CultureInfo.InvariantCulture) * scale;
    }

    private static Clay.TransitionProperty ParseTransitionProperty(object value)
    {
        string s = ToCss(value).Trim();
        if (s.Equals("all", StringComparison.OrdinalIgnoreCase))
        {
            return Clay.TransitionProperty.X
                   | Clay.TransitionProperty.Y
                   | Clay.TransitionProperty.Width
                   | Clay.TransitionProperty.Height
                   | Clay.TransitionProperty.BackgroundColor
                   | Clay.TransitionProperty.OverlayColor
                   | Clay.TransitionProperty.CornerRadius
                   | Clay.TransitionProperty.BorderColor
                   | Clay.TransitionProperty.BorderWidth;
        }

        Clay.TransitionProperty result = Clay.TransitionProperty.None;
        foreach (string raw in s.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
        {
            result |= raw.Trim().ToLowerInvariant() switch
            {
                "x" => Clay.TransitionProperty.X,
                "y" => Clay.TransitionProperty.Y,
                "width" => Clay.TransitionProperty.Width,
                "height" => Clay.TransitionProperty.Height,
                "background-color" or "background" => Clay.TransitionProperty.BackgroundColor,
                "overlay-color" or "color" => Clay.TransitionProperty.OverlayColor,
                "corner-radius" or "border-radius" => Clay.TransitionProperty.CornerRadius,
                "border-color" => Clay.TransitionProperty.BorderColor,
                "border-width" => Clay.TransitionProperty.BorderWidth,
                _ => throw new ArgumentException($"Unsupported transition-property: '{raw}'."),
            };
        }
        return result;
    }

    private static bool TryParseBorderWidth(string token, out ushort width)
    {
        var span = token.AsSpan().Trim();
        if (span.EndsWith("px", StringComparison.OrdinalIgnoreCase))
            span = span[..^2].Trim();
        if (span.Length == 0 || span.IndexOf('%') >= 0)
        {
            width = 0;
            return false;
        }
        if (float.TryParse(span, NumberStyles.Float, CultureInfo.InvariantCulture, out float v))
        {
            width = ToUshort(v, "border");
            return true;
        }
        width = 0;
        return false;
    }

    #endregion
    
    #endregion

    #region Events
    
    public virtual void Update()
    {
        foreach (var child in GetChildSnapshot())
        {
            if (child is ClayElementBase elementBase)
                elementBase.Update();
        }
    }

    public bool HasCustomRender { get; set; }
    
    public virtual void Render(Clay.RenderCommand context)
    {
    }

    protected virtual void OnMousePressed(MouseEvent @event)
    {
    }
    
    protected virtual void OnMouseReleased(MouseEvent @event)
    {
    }
    
    protected virtual void OnMouseDragged(MouseDragEvent @event)
    {
    }

    protected virtual void OnMouseScrolled(MouseWheelEvent @event)
    {
    }

    protected virtual void OnMouseMoved(MouseMoveEvent @event)
    {
    }

    protected virtual void OnMouseEntered(MouseMoveEvent @event)
    {
    }

    protected virtual void OnMouseLeft(MouseMoveEvent @event)
    {
    }

    protected virtual void OnKeyTyped(KeyboardTypingEvent @event)
    {
    }
    
    public virtual void DispatchMouseMoved(BaseMouseMoveEvent @event)
    {
        if (Clay.PointerOver(ElementId))
        {
            var boundingBox = Clay.GetElementData(ElementId).BoundingBox;
            var relativeEvent = new MouseMoveEvent(
                Position: @event.Position,
                Buttons: @event.Buttons,
                CtrlKey: @event.CtrlKey,
                MetaKey: @event.AltKey,
                ShiftKey: @event.ShiftKey,
                RelativePosition: @event.Position - new Vector2(boundingBox.X, boundingBox.Y)
            );
            MouseMoved?.Invoke(relativeEvent);
            OnMouseMoved(relativeEvent);
        }
        foreach (var child in GetChildSnapshot())
        {
            if (child is ClayElementBase elementBase)
                elementBase.DispatchMouseMoved(@event);
        }
    }

    public virtual void DispatchMouseEntered(BaseMouseMoveEvent @event)
    {
        Logging.Info(
            $"[Node] DispatchMouseEntered {GetType().Name} Name='{DebugName}' " +
            $"Mouse=({@event.Position.X:F0},{@event.Position.Y:F0}) " +
            $"OldIsHovered={IsHovered}");
        IsHovered = true;
        var boundingBox = Clay.GetElementData(ElementId).BoundingBox;
        var relativeEvent = new MouseMoveEvent(
            Position: @event.Position,
            Buttons: @event.Buttons,
            CtrlKey: @event.CtrlKey,
            MetaKey: @event.AltKey,
            ShiftKey: @event.ShiftKey,
            RelativePosition: @event.Position - new Vector2(boundingBox.X, boundingBox.Y)
        );
        MouseEntered?.Invoke(relativeEvent);
        OnMouseEntered(relativeEvent);
        foreach (var child in GetChildSnapshot())
        {
            if (child is ClayElementBase elementBase)
                elementBase.DispatchMouseEntered(@event);
        }
    }

    public virtual void DispatchMouseLeft(BaseMouseMoveEvent @event)
    {
        IsHovered = false;
        var boundingBox = Clay.GetElementData(ElementId).BoundingBox;
        var relativeEvent = new MouseMoveEvent(
            Position: @event.Position,
            Buttons: @event.Buttons,
            CtrlKey: @event.CtrlKey,
            MetaKey: @event.AltKey,
            ShiftKey: @event.ShiftKey,
            RelativePosition: @event.Position - new Vector2(boundingBox.X, boundingBox.Y)
        );
        MouseLeft?.Invoke(relativeEvent);
        OnMouseLeft(relativeEvent);
        foreach (var child in GetChildSnapshot())
        {
            if (child is ClayElementBase elementBase)
                elementBase.DispatchMouseLeft(@event);
        }
    }

    public virtual void DispatchMousePressed(BaseMouseEvent @event)
    {
        var boundingBox = Clay.GetElementData(ElementId).BoundingBox;
        if (@event.Position.X > boundingBox.X && @event.Position.Y > boundingBox.Y && @event.Position.X < boundingBox.X + boundingBox.Width && @event.Position.Y < boundingBox.Y + boundingBox.Height)
        {
            var relativeEvent = new MouseEvent(
                Position: @event.Position,
                Button: @event.Button,
                Buttons: @event.Buttons,
                CtrlKey: @event.CtrlKey,
                MetaKey: @event.AltKey,
                ShiftKey: @event.ShiftKey,
                RelativePosition: @event.Position - new Vector2(boundingBox.X, boundingBox.Y)
            );
            if (IsFocusable)
            {
                IsActive = true;
            }

            MousePressed?.Invoke(relativeEvent);
            OnMousePressed(relativeEvent);
        }
        foreach (var child in GetChildSnapshot())
        {
            if (child is ClayElementBase elementBase)
                elementBase.DispatchMousePressed(@event);
        }
    }

    public virtual void DispatchMouseReleased(BaseMouseEvent @event)
    {
        var boundingBox = Clay.GetElementData(ElementId).BoundingBox;
        if (@event.Position.X > boundingBox.X && @event.Position.Y > boundingBox.Y && @event.Position.X < boundingBox.X + boundingBox.Width && @event.Position.Y < boundingBox.Y + boundingBox.Height)
        {
            var relativeEvent = new MouseEvent(
                Position: @event.Position,
                Button: @event.Button,
                Buttons: @event.Buttons,
                CtrlKey: @event.CtrlKey,
                MetaKey: @event.AltKey,
                ShiftKey: @event.ShiftKey,
                RelativePosition: @event.Position - new Vector2(boundingBox.X, boundingBox.Y)
            );
            if (IsFocusable)
            {
                IsActive = false;
            }

            MouseReleased?.Invoke(relativeEvent);
            OnMouseReleased(relativeEvent);
        }
        foreach (var child in GetChildSnapshot())
        {
            if (child is ClayElementBase elementBase)
                elementBase.DispatchMouseReleased(@event);
        }
    }

    public virtual void DispatchMouseDragged(BaseMouseDragEvent @event)
    {
        var boundingBox = Clay.GetElementData(ElementId).BoundingBox;
        if (@event.Position.X > boundingBox.X && @event.Position.Y > boundingBox.Y && @event.Position.X < boundingBox.X + boundingBox.Width && @event.Position.Y < boundingBox.Y + boundingBox.Height)
        {
            var relativeEvent = new MouseDragEvent(
                DragStart: @event.DragStart,
                RelativeDragStart: @event.DragStart - new Vector2(boundingBox.X, boundingBox.Y),
                Position: @event.Position,
                Button: @event.Button,
                Buttons: @event.Buttons,
                CtrlKey: @event.CtrlKey,
                MetaKey: @event.MetaKey,
                ShiftKey: @event.ShiftKey,
                RelativePosition: @event.Position - new Vector2(boundingBox.X, boundingBox.Y)
            );
            MouseDragged?.Invoke(relativeEvent);
            OnMouseDragged(relativeEvent);
        }
        foreach (var child in GetChildSnapshot())
        {
            if (child is ClayElementBase elementBase)
                elementBase.DispatchMouseDragged(@event);
        }
    }

    public virtual void DispatchMouseScrolled(BaseMouseWheelEvent @event)
    {
        var boundingBox = Clay.GetElementData(ElementId).BoundingBox;
        if (@event.Position.X > boundingBox.X && @event.Position.Y > boundingBox.Y && @event.Position.X < boundingBox.X + boundingBox.Width && @event.Position.Y < boundingBox.Y + boundingBox.Height)
        {
            var relativeEvent = new MouseWheelEvent(
                Delta: @event.Delta,
                Position: @event.Position,
                Buttons: @event.Buttons,
                CtrlKey: @event.CtrlKey,
                MetaKey: @event.MetaKey,
                ShiftKey: @event.ShiftKey,
                RelativePosition: @event.Position - new Vector2(boundingBox.X, boundingBox.Y)
            );
            MouseScrolled?.Invoke(relativeEvent);
            OnMouseScrolled(relativeEvent);
        }
        foreach (var child in GetChildSnapshot())
        {
            if (child is ClayElementBase elementBase)
                elementBase.DispatchMouseScrolled(@event);
        }
    }

    public virtual void OnKeyPressed(KeyboardEvent @event)
    {
    }

    public virtual void OnKeyReleased(KeyboardEvent @event)
    {
    }
    
    public virtual void DispatchKeyPressed(KeyboardEvent @event)
    {
        if (IsFocusable && IsFocused)
        {
            KeyPressed?.Invoke(@event);
            OnKeyPressed(@event);
        }
        foreach (var child in GetChildSnapshot())
        {
            if (child is ClayElementBase elementBase)
                elementBase.DispatchKeyPressed(@event);
        }
    }

    public virtual void DispatchKeyReleased(KeyboardEvent @event)
    {
        if (IsFocusable && IsFocused)
        {
            KeyReleased?.Invoke(@event);
            OnKeyReleased(@event);
        }
        foreach (var child in GetChildSnapshot())
        {
            if (child is ClayElementBase elementBase)
                elementBase.DispatchKeyReleased(@event);
        }
    }

    public virtual void DispatchKeyTyped(KeyboardTypingEvent @event)
    {
        if (IsFocusable && IsFocused)
        {
            KeyTyped?.Invoke(@event);
            OnKeyTyped(@event);
        }
        foreach (var child in GetChildSnapshot())
        {
            if (child is ClayElementBase elementBase)
                elementBase.DispatchKeyTyped(@event);
        }
    }
    
    #endregion
    
    #region Property Parsers
    
    private protected static string ToCss(object value) => value switch
    {
        null => throw new ArgumentException("Property value is null."),
        string s => s,
        IFormattable f => f.ToString(null, CultureInfo.InvariantCulture),
        _ => value.ToString() ?? throw new ArgumentException("Unsupported property value type."),
    };

    private protected static Clay.Color ParseColor(object value)
    {
        string s = ToCss(value).Trim();
        if (s.Length == 0)
            throw new ArgumentException("Empty color value.");
        if (s[0] == '#')
            return ParseHexColor(s);
        if (s.StartsWith("rgb", StringComparison.OrdinalIgnoreCase))
            return ParseRgbColor(s);
        if (NamedColors.TryGetValue(s, out var named))
            return named;
        throw new ArgumentException($"Unsupported color: '{s}'.");
    }

    private protected static Clay.Color ParseHexColor(string s)
    {
        var hex = s.AsSpan(1).Trim();
        return hex.Length switch
        {
            3 => new Clay.Color(HexNibble(hex[0]) * 17f, HexNibble(hex[1]) * 17f, HexNibble(hex[2]) * 17f, 255f),
            4 => new Clay.Color(HexNibble(hex[0]) * 17f, HexNibble(hex[1]) * 17f, HexNibble(hex[2]) * 17f, HexNibble(hex[3]) * 17f),
            6 => new Clay.Color(HexByte(hex[0], hex[1]), HexByte(hex[2], hex[3]), HexByte(hex[4], hex[5]), 255f),
            8 => new Clay.Color(HexByte(hex[0], hex[1]), HexByte(hex[2], hex[3]), HexByte(hex[4], hex[5]), HexByte(hex[6], hex[7])),
            _ => throw new ArgumentException($"Invalid hex color: '{s}'."),
        };

        static float HexNibble(char c) => Convert.ToInt32(c.ToString(), 16);
        static float HexByte(char hi, char lo) => Convert.ToInt32($"{hi}{lo}", 16);
    }

    private protected static Clay.Color ParseRgbColor(string s)
    {
        int open = s.IndexOf('(');
        int close = s.LastIndexOf(')');
        if (open < 0 || close <= open)
            throw new ArgumentException($"Invalid rgb() color: '{s}'.");

        string inner = s[(open + 1)..close];
        string[] parts = inner.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        if (parts.Length is not (3 or 4))
            throw new ArgumentException($"Invalid rgb() color: '{s}'.");

        float r = ParseChannel(parts[0], s);
        float g = ParseChannel(parts[1], s);
        float b = ParseChannel(parts[2], s);
        float a = parts.Length == 4 ? ParseAlpha(parts[3], s) : 255f;
        return new Clay.Color(r, g, b, a);

        static float ParseChannel(string v, string full)
        {
            var span = v.AsSpan().Trim();
            if (span.EndsWith("%", StringComparison.Ordinal))
            {
                float p = float.Parse(span[..^1], NumberStyles.Float, CultureInfo.InvariantCulture);
                return Math.Clamp(p / 100f * 255f, 0f, 255f);
            }
            float n = float.Parse(span, NumberStyles.Float, CultureInfo.InvariantCulture);
            return Math.Clamp(n, 0f, 255f);
        }

        static float ParseAlpha(string v, string full)
        {
            var span = v.AsSpan().Trim();
            if (span.EndsWith("%", StringComparison.Ordinal))
            {
                float p = float.Parse(span[..^1], NumberStyles.Float, CultureInfo.InvariantCulture);
                return Math.Clamp(p / 100f * 255f, 0f, 255f);
            }
            float n = float.Parse(span, NumberStyles.Float, CultureInfo.InvariantCulture);
            return n <= 1f ? n * 255f : Math.Clamp(n, 0f, 255f);
        }
    }

    private protected static bool IsColorToken(string token)
    {
        if (token.StartsWith('#')) return true;
        if (token.StartsWith("rgb", StringComparison.OrdinalIgnoreCase)) return true;
        return NamedColors.ContainsKey(token);
    }

    private protected static float ParsePixels(object value, string prop)
    {
        var span = ToCss(value).AsSpan().Trim();
        if (span.EndsWith("px", StringComparison.OrdinalIgnoreCase))
            span = span[..^2].Trim();
        if (span.Length == 0)
            throw new ArgumentException($"Invalid length for '{prop}': '{span}'.");
        if (span.IndexOf('%') >= 0)
            throw new ArgumentException($"'{prop}' does not accept percentages (expected pixels).");
        return float.Parse(span, NumberStyles.Float, CultureInfo.InvariantCulture);
    }

    private protected static ushort ToUshort(float v, string prop)
    {
        if (v < 0 || v > ushort.MaxValue)
            throw new ArgumentException($"Value for '{prop}' out of range (0-{ushort.MaxValue}): {v}.");
        return (ushort)MathF.Round(v);
    }

    private protected static int ParseInt(object value, string prop)
    {
        string s = ToCss(value).Trim();
        return int.Parse(s, NumberStyles.Integer, CultureInfo.InvariantCulture);
    }

    private protected static readonly Dictionary<string, Clay.Color> NamedColors = new(StringComparer.OrdinalIgnoreCase)
    {
        ["aliceblue"] = new Clay.Color(240, 248, 255, 255),
        ["antiquewhite"] = new Clay.Color(250, 235, 215, 255),
        ["aqua"] = new Clay.Color(0, 255, 255, 255),
        ["aquamarine"] = new Clay.Color(127, 255, 212, 255),
        ["azure"] = new Clay.Color(240, 255, 255, 255),
        ["beige"] = new Clay.Color(245, 245, 220, 255),
        ["bisque"] = new Clay.Color(255, 228, 196, 255),
        ["black"] = new Clay.Color(0, 0, 0, 255),
        ["blanchedalmond"] = new Clay.Color(255, 235, 205, 255),
        ["blue"] = new Clay.Color(0, 0, 255, 255),
        ["blueviolet"] = new Clay.Color(138, 43, 226, 255),
        ["brown"] = new Clay.Color(165, 42, 42, 255),
        ["burlywood"] = new Clay.Color(222, 184, 135, 255),
        ["cadetblue"] = new Clay.Color(95, 158, 160, 255),
        ["chartreuse"] = new Clay.Color(127, 255, 0, 255),
        ["chocolate"] = new Clay.Color(210, 105, 30, 255),
        ["coral"] = new Clay.Color(255, 127, 80, 255),
        ["cornflowerblue"] = new Clay.Color(100, 149, 237, 255),
        ["cornsilk"] = new Clay.Color(255, 248, 220, 255),
        ["crimson"] = new Clay.Color(220, 20, 60, 255),
        ["cyan"] = new Clay.Color(0, 255, 255, 255),
        ["darkblue"] = new Clay.Color(0, 0, 139, 255),
        ["darkcyan"] = new Clay.Color(0, 139, 139, 255),
        ["darkgoldenrod"] = new Clay.Color(184, 134, 11, 255),
        ["darkgray"] = new Clay.Color(169, 169, 169, 255),
        ["darkgreen"] = new Clay.Color(0, 100, 0, 255),
        ["darkkhaki"] = new Clay.Color(189, 183, 107, 255),
        ["darkmagenta"] = new Clay.Color(139, 0, 139, 255),
        ["darkolivegreen"] = new Clay.Color(85, 107, 47, 255),
        ["darkorange"] = new Clay.Color(255, 140, 0, 255),
        ["darkorchid"] = new Clay.Color(153, 50, 204, 255),
        ["darkred"] = new Clay.Color(139, 0, 0, 255),
        ["darksalmon"] = new Clay.Color(233, 150, 122, 255),
        ["darkseagreen"] = new Clay.Color(143, 188, 139, 255),
        ["darkslateblue"] = new Clay.Color(72, 61, 139, 255),
        ["darkslategray"] = new Clay.Color(47, 79, 79, 255),
        ["darkturquoise"] = new Clay.Color(0, 206, 209, 255),
        ["darkviolet"] = new Clay.Color(148, 0, 211, 255),
        ["deeppink"] = new Clay.Color(255, 20, 147, 255),
        ["deepskyblue"] = new Clay.Color(0, 191, 255, 255),
        ["dimgray"] = new Clay.Color(105, 105, 105, 255),
        ["dodgerblue"] = new Clay.Color(30, 144, 255, 255),
        ["firebrick"] = new Clay.Color(178, 34, 34, 255),
        ["floralwhite"] = new Clay.Color(255, 250, 240, 255),
        ["forestgreen"] = new Clay.Color(34, 139, 34, 255),
        ["fuchsia"] = new Clay.Color(255, 0, 255, 255),
        ["gainsboro"] = new Clay.Color(220, 220, 220, 255),
        ["ghostwhite"] = new Clay.Color(248, 248, 255, 255),
        ["gold"] = new Clay.Color(255, 215, 0, 255),
        ["goldenrod"] = new Clay.Color(218, 165, 32, 255),
        ["gray"] = new Clay.Color(128, 128, 128, 255),
        ["green"] = new Clay.Color(0, 128, 0, 255),
        ["greenyellow"] = new Clay.Color(173, 255, 47, 255),
        ["honeydew"] = new Clay.Color(240, 255, 240, 255),
        ["hotpink"] = new Clay.Color(255, 105, 180, 255),
        ["indianred"] = new Clay.Color(205, 92, 92, 255),
        ["indigo"] = new Clay.Color(75, 0, 130, 255),
        ["ivory"] = new Clay.Color(255, 255, 240, 255),
        ["khaki"] = new Clay.Color(240, 230, 140, 255),
        ["lavender"] = new Clay.Color(230, 230, 250, 255),
        ["lavenderblush"] = new Clay.Color(255, 240, 245, 255),
        ["lawngreen"] = new Clay.Color(124, 252, 0, 255),
        ["lemonchiffon"] = new Clay.Color(255, 250, 205, 255),
        ["lightblue"] = new Clay.Color(173, 216, 230, 255),
        ["lightcoral"] = new Clay.Color(240, 128, 128, 255),
        ["lightcyan"] = new Clay.Color(224, 255, 255, 255),
        ["lightgoldenrodyellow"] = new Clay.Color(250, 250, 210, 255),
        ["lightgray"] = new Clay.Color(211, 211, 211, 255),
        ["lightgreen"] = new Clay.Color(144, 238, 144, 255),
        ["lightpink"] = new Clay.Color(255, 182, 193, 255),
        ["lightsalmon"] = new Clay.Color(255, 160, 122, 255),
        ["lightseagreen"] = new Clay.Color(32, 178, 170, 255),
        ["lightskyblue"] = new Clay.Color(135, 206, 250, 255),
        ["lightslategray"] = new Clay.Color(119, 136, 153, 255),
        ["lightsteelblue"] = new Clay.Color(176, 196, 222, 255),
        ["lightyellow"] = new Clay.Color(255, 255, 224, 255),
        ["lime"] = new Clay.Color(0, 255, 0, 255),
        ["limegreen"] = new Clay.Color(50, 205, 50, 255),
        ["linen"] = new Clay.Color(250, 240, 230, 255),
        ["magenta"] = new Clay.Color(255, 0, 255, 255),
        ["maroon"] = new Clay.Color(128, 0, 0, 255),
        ["mediumaquamarine"] = new Clay.Color(102, 205, 170, 255),
        ["mediumblue"] = new Clay.Color(0, 0, 205, 255),
        ["mediumorchid"] = new Clay.Color(186, 85, 211, 255),
        ["mediumpurple"] = new Clay.Color(147, 112, 219, 255),
        ["mediumseagreen"] = new Clay.Color(60, 179, 113, 255),
        ["mediumslateblue"] = new Clay.Color(123, 104, 238, 255),
        ["mediumspringgreen"] = new Clay.Color(0, 250, 154, 255),
        ["mediumturquoise"] = new Clay.Color(72, 209, 204, 255),
        ["mediumvioletred"] = new Clay.Color(199, 21, 133, 255),
        ["midnightblue"] = new Clay.Color(25, 25, 112, 255),
        ["mintcream"] = new Clay.Color(245, 255, 250, 255),
        ["mistyrose"] = new Clay.Color(255, 228, 225, 255),
        ["moccasin"] = new Clay.Color(255, 228, 181, 255),
        ["navajowhite"] = new Clay.Color(255, 222, 173, 255),
        ["navy"] = new Clay.Color(0, 0, 128, 255),
        ["oldlace"] = new Clay.Color(253, 245, 230, 255),
        ["olive"] = new Clay.Color(128, 128, 0, 255),
        ["olivedrab"] = new Clay.Color(107, 142, 35, 255),
        ["orange"] = new Clay.Color(255, 165, 0, 255),
        ["orangered"] = new Clay.Color(255, 69, 0, 255),
        ["orchid"] = new Clay.Color(218, 112, 214, 255),
        ["palegoldenrod"] = new Clay.Color(238, 232, 170, 255),
        ["palegreen"] = new Clay.Color(152, 251, 152, 255),
        ["paleturquoise"] = new Clay.Color(175, 238, 238, 255),
        ["palevioletred"] = new Clay.Color(219, 112, 147, 255),
        ["papayawhip"] = new Clay.Color(255, 239, 213, 255),
        ["peachpuff"] = new Clay.Color(255, 218, 185, 255),
        ["peru"] = new Clay.Color(205, 133, 63, 255),
        ["pink"] = new Clay.Color(255, 192, 203, 255),
        ["plum"] = new Clay.Color(221, 160, 221, 255),
        ["powderblue"] = new Clay.Color(176, 224, 230, 255),
        ["purple"] = new Clay.Color(128, 0, 128, 255),
        ["red"] = new Clay.Color(255, 0, 0, 255),
        ["rosybrown"] = new Clay.Color(188, 143, 143, 255),
        ["royalblue"] = new Clay.Color(65, 105, 225, 255),
        ["saddlebrown"] = new Clay.Color(139, 69, 19, 255),
        ["salmon"] = new Clay.Color(250, 128, 114, 255),
        ["sandybrown"] = new Clay.Color(244, 164, 96, 255),
        ["seagreen"] = new Clay.Color(46, 139, 87, 255),
        ["seashell"] = new Clay.Color(255, 245, 238, 255),
        ["sienna"] = new Clay.Color(160, 82, 45, 255),
        ["silver"] = new Clay.Color(192, 192, 192, 255),
        ["skyblue"] = new Clay.Color(135, 206, 235, 255),
        ["slateblue"] = new Clay.Color(106, 90, 205, 255),
        ["slategray"] = new Clay.Color(112, 128, 144, 255),
        ["snow"] = new Clay.Color(255, 250, 250, 255),
        ["springgreen"] = new Clay.Color(0, 255, 127, 255),
        ["steelblue"] = new Clay.Color(70, 130, 180, 255),
        ["tan"] = new Clay.Color(210, 180, 140, 255),
        ["teal"] = new Clay.Color(0, 128, 128, 255),
        ["thistle"] = new Clay.Color(216, 191, 216, 255),
        ["tomato"] = new Clay.Color(255, 99, 71, 255),
        ["turquoise"] = new Clay.Color(64, 224, 208, 255),
        ["violet"] = new Clay.Color(238, 130, 238, 255),
        ["wheat"] = new Clay.Color(245, 222, 179, 255),
        ["white"] = new Clay.Color(255, 255, 255, 255),
        ["whitesmoke"] = new Clay.Color(245, 245, 245, 255),
        ["yellow"] = new Clay.Color(255, 255, 0, 255),
        ["yellowgreen"] = new Clay.Color(154, 205, 50, 255),
    };
    
    private protected static Clay.TextAlignment ParseTextAlignment(object value) =>
        ToCss(value).Trim().ToLowerInvariant() switch
        {
            "left" or "start" => Clay.TextAlignment.Left,
            "center" => Clay.TextAlignment.Center,
            "right" or "end" => Clay.TextAlignment.Right,
            _ => throw new ArgumentException($"Unsupported 'text-align' value: '{value}'. Clay supports left/center/right."),
        };

    private protected static Clay.TextElementConfigWrapMode ParseWrapMode(object value) =>
        ToCss(value).Trim().ToLowerInvariant() switch
        {
            "normal" or "wrap" or "words" => Clay.TextElementConfigWrapMode.Words,
            "nowrap" or "none" => Clay.TextElementConfigWrapMode.None,
            "pre" or "pre-wrap" or "newlines" => Clay.TextElementConfigWrapMode.Newlines,
            _ => throw new ArgumentException($"Unsupported 'white-space' value: '{value}'. Clay supports normal/nowrap/pre."),
        };

    private protected static FontStyle ParseFontStyle(object value)
    {
        var style = ToCss(value);
        switch (style)
        {
            case "normal":
                return FontStyle.Plain;
            case "italic":
                return FontStyle.Italic;
            case "bold":
                return FontStyle.Bold;
            default:
                throw new ArgumentException("Value for 'font-style' must be normal, italic, or bold.");
        }
    }

    #endregion

    #region Lua

    LuaTable? ILuaUserData.Metatable
    {
        get => _metatable;
        set => throw new InvalidOperationException("The metatable of a [LuaVisible] type cannot be assigned to.");
    }
    public static LuaTable Metatable => _metatable;
    public static LuaTable TypeTable => _typeTable;

    public static implicit operator LuaValue(ClayElementBase value)
    {
        return LuaValue.FromUserData(value);
    }

    internal static readonly LuaFunction LuaAppendChild = new("appendChild", (context, ct) =>
    {
        var instance = context.GetArgument<ClayElementBase>(0);
        var arg0 = context.GetArgumentOrNullClass<ClayNode>(1);
        instance.AppendChild(arg0!);
        return new ValueTask<int>(context.Return());
    });
    internal static readonly LuaFunction LuaInsertBefore = new("insertBefore", (context, ct) =>
    {
        var instance = context.GetArgument<ClayElementBase>(0);
        var arg0 = context.GetArgumentOrNullClass<ClayNode>(1);
        var arg1 = context.GetArgumentOrNullClass<ClayNode>(2);
        instance.InsertBefore(arg0!, arg1!);
        return new ValueTask<int>(context.Return());
    });
    internal static readonly LuaFunction LuaRemoveChild = new("removeChild", (context, ct) =>
    {
        var instance = context.GetArgument<ClayElementBase>(0);
        var arg0 = context.GetArgumentOrNullClass<ClayNode>(1);
        instance.RemoveChild(arg0!);
        return new ValueTask<int>(context.Return());
    });
    internal static readonly LuaFunction LuaSetProperty = new("setProperty", (context, ct) =>
    {
        var instance = context.GetArgument<ClayElementBase>(0);
        var arg0 = context.GetArgumentOrNullClass<string>(1);
        var arg1 = context.GetArgument<LuaValue>(2);
        instance.SetLuaProperty(arg0!, arg1, context.State);
        return new ValueTask<int>(context.Return());
    });
    internal static readonly LuaFunction LuaElementBase = new("__index", (context, ct) =>
    {
        var instance = context.GetArgument<ClayElementBase>(0);
        var key = context.GetArgument(1);
        if (key.TryRead<string>(out var stringKey))
        {
            if (stringKey == "nodeType") return new ValueTask<int>(context.Return(LuaValue.FromUserData(instance.NodeType, LuaVisibleTypeMetatableRegistry<NodeType>.Metatable!)));
            if (stringKey == "appendChild") return new ValueTask<int>(context.Return(LuaAppendChild));
            if (stringKey == "insertBefore") return new ValueTask<int>(context.Return(LuaInsertBefore));
            if (stringKey == "removeChild") return new ValueTask<int>(context.Return(LuaRemoveChild));
            if (stringKey == "setProperty") return new ValueTask<int>(context.Return(LuaSetProperty));
        }
        return new ValueTask<int>(context.Return(LuaValue.Nil));
    });
    internal static readonly LuaFunction LuaNewIndex = new("__newindex", (context, ct) =>
    {
        var instance = context.GetArgument<ClayElementBase>(0);
        var key = context.GetArgument(1);
        if (key.TryRead<string>(out var stringKey))
        {
            throw new LuaRuntimeException(context.State, $"'{stringKey}' not found or read-only.");
        }
        throw new LuaRuntimeException(context.State, $"'{key}' not found.");
    });
    internal static readonly LuaFunction LuaToString = new("__tostring", (context, ct) =>
    {
        var instance = context.GetArgument<ClayElementBase>(0);
        return new ValueTask<int>(context.Return(instance.ToString() ?? "<nil>"));
    });
    internal static LuaTable __metatable_ClayElementBase = null!;
    
    #endregion
}