using System.Globalization;
using Lua;
using NFMWorld.DriverInterface.DriverInterface;
using NFMWorld.DriverInterface.UI;
using NFMWorld.Reactor;

namespace NFMWorldLibrary.Util;

public static class LuaUiLibrary
{
    public static void Register(LuaState state, Action<View> setActiveRoot, Action<string, LuaValue> call, Func<string, Action<LuaValue>, Action> onEvent)
    {
        var library = new LuaTable()
        {
            ["createRoot"] = CreateRoot,
            ["createInstance"] = CreateInstance,
            ["createTextInstance"] = CreateTextInstance,
            ["appendChild"] = AppendChild,
            ["insertBefore"] = InsertBefore,
            ["removeChild"] = RemoveChild,
            ["setProperty"] = SetProperty,
            ["commitTextUpdate"] = CommitTextUpdate,
            ["setActiveRoot"] = new LuaFunction("setActiveRoot", (context, ct) =>
            {
                var view = context.GetArgument<View>(0);
                setActiveRoot(view);

                return new ValueTask<int>(context.Return());
            }),
            ["call"] = new LuaFunction("call", (context, ct) =>
            {
                var method = context.GetArgument<string>(0);
                var payload = context.GetArgument(1);
                call(method, payload);
                return new ValueTask<int>(context.Return());
            }),
            ["onEvent"] = new LuaFunction("onEvent", (context, ct) =>
            {
                var @event = context.GetArgument<string>(0);
                var callback = context.GetArgument<LuaFunction>(1);

                var unregister = onEvent(@event, value =>
                {
                    state.Call(callback, [value]);
                });

                return new ValueTask<int>(context.Return(new LuaFunction("unregister", (context, ct) =>
                {
                    unregister();
                    return new ValueTask<int>(context.Return());
                })));
            })
        };

        state.Environment["UiLib"] = library;
    }

    internal static readonly LuaFunction CreateRoot = new("createRoot", (context, ct) =>
    {
        return new ValueTask<int>(context.Return(new View()));
    });

    internal static readonly LuaFunction CreateInstance = new("createInstance", (context, ct) =>
    {
        var vtype = context.GetArgument<string>(0);
        var props = context.GetArgument<LuaTable>(1);
        switch (vtype)
        {
            case "view":
                var view = new View();

                foreach (var (rawkey, rawvalue) in props)
                {
                    if (!rawkey.TryRead<string>(out var key))
                    {
                        continue;
                    }

                    AssignComponentProperty(key, rawvalue, view, context.State);
                }

                return new ValueTask<int>(context.Return(view));
            case "image":
                var image = new Image();

                foreach (var (rawkey, rawvalue) in props)
                {
                    if (!rawkey.TryRead<string>(out var key))
                    {
                        continue;
                    }

                    AssignComponentProperty(key, rawvalue, image, context.State);
                }

                return new ValueTask<int>(context.Return(image));
            case "text":
                var text = new Text();

                foreach (var (rawkey, rawvalue) in props)
                {
                    if (!rawkey.TryRead<string>(out var key))
                    {
                        continue;
                    }

                    AssignComponentProperty(key, rawvalue, text, context.State);
                }

                return new ValueTask<int>(context.Return(text));
            case "textinput":
                var textinput = new TextInput();

                foreach (var (rawkey, rawvalue) in props)
                {
                    if (!rawkey.TryRead<string>(out var key))
                    {
                        continue;
                    }

                    AssignComponentProperty(key, rawvalue, textinput, context.State);
                }

                return new ValueTask<int>(context.Return(textinput));
        }

        throw new LuaRuntimeException(context.State, new InvalidOperationException($"Unknown vnode type {vtype}"));
    });

    internal static readonly LuaFunction CreateTextInstance = new("createTextInstance", (context, ct) =>
    {
        var text = context.GetArgument<string>(0);
        return new ValueTask<int>(context.Return( new TextNode() { Text = text }));
    });

    internal static readonly LuaFunction AppendChild = new("appendChild", (context, ct) =>
    {
        var parent = context.GetArgument<Node>(0);
        var child = context.GetArgument<Node>(1);

        if (parent is Component cmp)
        {
            cmp.AddChild(child);
        }

        // Any structural change can invalidate hovered positions or dispose
        // nodes still referenced by the hover chain (e.g. React unmounting
        // menu items during navigation). Drop it so the next mouse move
        // recomputes from scratch instead of diffing against stale nodes.
        FocusManager.ResetHover();

        return new ValueTask<int>(context.Return());
    });

    internal static readonly LuaFunction InsertBefore = new("insertBefore", (context, ct) =>
    {
        var parent = context.GetArgument<Node>(0);
        var child = context.GetArgument<Node>(1);
        var before = context.GetArgument<Node>(2);

        if (parent is Component cmp)
        {
            cmp.InsertAt(parent.VisualChildren.IndexOf(before), child);
        }

        FocusManager.ResetHover();

        return new ValueTask<int>(context.Return());
    });

    internal static readonly LuaFunction RemoveChild = new("removeChild", (context, ct) =>
    {
        var parent = context.GetArgument<Node>(0);
        var child = context.GetArgument<Node>(1);

        if (parent is Component cmp)
        {
            cmp.RemoveAt(parent.VisualChildren.IndexOf(child));
        }

        FocusManager.ResetHover();

        return new ValueTask<int>(context.Return());
    });

    internal static readonly LuaFunction SetProperty = new("setProperty", (context, ct) =>
    {
        var instance = context.GetArgument<Node>(0);
        var key = context.GetArgument<string>(1);
        var value = context.GetArgument(2);

        if (instance is Component cmp)
        {
            AssignComponentProperty(key, value, cmp, context.State);
        }

        return new ValueTask<int>(context.Return());
    });

    internal static readonly LuaFunction CommitTextUpdate = new("commitTextUpdate", (context, ct) =>
    {
        var textInstance = context.GetArgument<TextNode>(0);
        var oldText = context.GetArgumentOrNullClass<string>(1);
        var newText = context.GetArgumentOrNullClass<string>(2);

        textInstance.Text = newText;

        return new ValueTask<int>(context.Return());
    });

    private static void AssignComponentProperty(string key, LuaValue rawvalue, Component cmp, LuaState state)
    {
        switch (key)
        {
            case "name" when rawvalue.TryRead<string>(out var str) :
                cmp.Name = str;
                break;
            case "style" when rawvalue.TryRead<LuaTable>(out var value):
                cmp.Styles = AssignStylesProps(value);
                if (cmp is Text text)
                {
                    text.TextStyles = AssignTextStylesProps(value);
                }
                else if (cmp is TextInput textInput)
                {
                    textInput.TextStyles = AssignTextStylesProps(value);
                    textInput.TextInputStyles = AssignTextInputStylesProps(value);
                }
                break;
            case "onanimationframebegan" when rawvalue.TryRead<LuaFunction>(out var func):
                cmp.AnimationFrameBegan = () =>
                {
                    state.Call(func, []);
                };
                break;
            case "active" when rawvalue.TryRead<bool>(out var b):
                cmp.IsActive = b;
                break;
            case "focused" when rawvalue.TryRead<bool>(out var b):
                cmp.IsFocused = b;
                break;
            case "taborder" when rawvalue.TryRead<int>(out var i):
                cmp.TabOrder = i;
                break;
            // NOTE: These bindings must REPLACE the previous handler rather than
            // accumulate with +=. React re-creates Lua closures on every parent
            // render, and diffProps re-sets any prop whose value changed, so a
            // plain += would stack a new handler on each render — causing a
            // single click to fire the action N times (e.g. navigation firing
            // multiple times, popping several menu pages at once). These event
            // props map one-to-one to a single host delegate, so clearing first
            // is correct.
            case "onmousedown" when rawvalue.TryRead<LuaFunction>(out var func):
                cmp.MousePressed = null;
                cmp.MousePressed += @event =>
                {
                    state.Call(func, [@event]);
                };
                break;
            case "onmouseup" when rawvalue.TryRead<LuaFunction>(out var func):
                cmp.MouseReleased = null;
                cmp.MouseReleased += @event =>
                {
                    state.Call(func, [@event]);
                };
                break;
            case "onmousedrag" when rawvalue.TryRead<LuaFunction>(out var func):
                cmp.MouseDragged = null;
                cmp.MouseDragged += @event =>
                {
                    state.Call(func, [@event]);
                };
                break;
            case "onmousescroll" when rawvalue.TryRead<LuaFunction>(out var func):
                cmp.MouseScrolled = null;
                cmp.MouseScrolled += @event =>
                {
                    state.Call(func, [@event]);
                };
                break;
            case "onmousemove" when rawvalue.TryRead<LuaFunction>(out var func):
                cmp.MouseMoved = null;
                cmp.MouseMoved += @event =>
                {
                    state.Call(func, [@event]);
                };
                break;
            case "onmouseenter" when rawvalue.TryRead<LuaFunction>(out var func):
                cmp.MouseEntered = null;
                cmp.MouseEntered += @event =>
                {
                    state.Call(func, [@event]);
                };
                break;
            case "onmouseleave" when rawvalue.TryRead<LuaFunction>(out var func):
                cmp.MouseLeft = null;
                cmp.MouseLeft += @event =>
                {
                    state.Call(func, [@event]);
                };
                break;
            case "onkeytype" when rawvalue.TryRead<LuaFunction>(out var func):
                cmp.KeyTyped = null;
                cmp.KeyTyped += @event =>
                {
                    state.Call(func, [@event]);
                };
                break;
            case "onkeydown" when rawvalue.TryRead<LuaFunction>(out var func):
                cmp.KeyPressed = null;
                cmp.KeyPressed += @event =>
                {
                    state.Call(func, [@event]);
                };
                break;
            case "onkeyup" when rawvalue.TryRead<LuaFunction>(out var func):
                cmp.KeyReleased = null;
                cmp.KeyReleased += @event =>
                {
                    state.Call(func, [@event]);
                };
                break;
            case "onfocus" when rawvalue.TryRead<LuaFunction>(out var func):
                cmp.Focused = null;
                cmp.Focused += () =>
                {
                    state.Call(func, []);
                };
                break;
            case "onblur" when rawvalue.TryRead<LuaFunction>(out var func):
                cmp.Unfocused = null;
                cmp.Unfocused += () =>
                {
                    state.Call(func, []);
                };
                break;
            case "src" when cmp is Image image && rawvalue.TryRead<string>(out var str):
                image.ImageData = G.LoadImage(str);
                break;
            case "scale" when cmp is Image image && rawvalue.TryRead<float>(out var f):
                image.Scale = f;
                break;
            case "value" when cmp is TextInput textInput && rawvalue.TryRead<string>(out var str):
                textInput.Text = str;
                break;
            case "placeholder" when cmp is TextInput textInput && rawvalue.TryRead<string>(out var str):
                textInput.Placeholder = str;
                break;
            case "onsubmit" when cmp is TextInput textInput && rawvalue.TryRead<LuaFunction>(out var func):
                textInput.Submitted += value =>
                {
                    state.Call(func, [value]);
                };
                break;
            case "onchange" when cmp is TextInput textInput && rawvalue.TryRead<LuaFunction>(out var func):
                textInput.TextChanged += value =>
                {
                    state.Call(func, [value]);
                };
                break;
        }
    }

    private static TextInputStyles AssignTextInputStylesProps(LuaTable props)
    {
        var styles = new TextInputStyles();

        foreach (var (rawkey, rawvalue) in props)
        {
            if (!rawkey.TryRead<string>(out var key))
            {
                continue;
            }

            styles = key switch
            {
                "cursor-color" or "cursorColor" when rawvalue.TryRead<string>(out var v) => styles with
                {
                    CursorColor = ParseColor(v) ?? new Color()
                },
                "selection-color" or "selectionColor" when rawvalue.TryRead<string>(out var v) => styles with
                {
                    SelectionColor = ParseColor(v) ?? new Color()
                },
                "placeholder-color" or "placeholderColor" when rawvalue.TryRead<string>(out var v) => styles with
                {
                    PlaceholderColor = ParseColor(v) ?? new Color()
                },
                _ => styles
            };
        }

        return styles;
    }

    private static TextStyles AssignTextStylesProps(LuaTable props)
    {
        var styles = new TextStyles();

        foreach (var (rawkey, rawvalue) in props)
        {
            if (!rawkey.TryRead<string>(out var key))
            {
                continue;
            }

            styles = key switch
            {
                "color" when rawvalue.TryRead<string>(out var v) => styles with
                {
                    ForegroundColor = ParseColor(v) ?? new Color()
                },
                "stroke" or "strokeColor" or "stroke-color" when rawvalue.TryRead<string>(out var v) => styles with
                {
                    StrokeColor = ParseColor(v)
                },
                "font-family" or "fontFamily" when rawvalue.TryRead<string>(out var v) => styles with
                {
                    FontFamily = v switch
                    {
                        "Adventure" => FontFamily.Adventure,
                        "AventureHollow" => FontFamily.AdventureHollow,
                        "Droid Sans" or "DroidSans" => FontFamily.DroidSans,
                        "Roboto Mono" or "RobotoMono" or "Roboto" => FontFamily.RobotoMono,
                        _ => styles.FontFamily,
                    }
                },
                "font-size" or "fontSize" => styles with { FontSize = ParseFontSize(rawvalue, styles.FontSize) },
                "font-style" or "fontStyle" when rawvalue.TryRead<string>(out var v) => styles with
                {
                    FontStyle = v switch
                    {
                        "bold" => FontStyle.Bold,
                        "italic" => FontStyle.Italic,
                        "plain" => FontStyle.Plain,
                        "bold italic" => FontStyle.Bold | FontStyle.Italic,
                        _ => styles.FontStyle
                    }
                },
                "word-break" or "wordBreak" when rawvalue.TryRead<string>(out var v) => styles with
                {
                    BreakType = v switch
                    {
                        "normal" => BreakType.Word,
                        "break-word" or "break-all" => BreakType.Character,
                        "keep-all" => BreakType.None,
                        _ => styles.BreakType
                    },
                },
                "vertical-align" or "verticalAlign" when rawvalue.TryRead<string>(out var v) => styles with
                {
                    VerticalAlignment = v switch
                    {
                        "top" => TextVerticalAlignment.Top,
                        "bottom" => TextVerticalAlignment.Bottom,
                        "middle" => TextVerticalAlignment.Center,
                        _ => styles.VerticalAlignment
                    },
                },
                "text-align" or "textAlign" when rawvalue.TryRead<string>(out var v) => styles with
                {
                    HorizontalAlignment = v switch
                    {
                        // todo rtl/ltr
                        "start" or "left" => TextHorizontalAlignment.Left,
                        "end" or "right" => TextHorizontalAlignment.Right,
                        "center" => TextHorizontalAlignment.Center,
                        _ => styles.HorizontalAlignment
                    },
                },
                _ => styles
            };
        }

        return styles;
    }

    private static Styles AssignStylesProps(LuaTable props)
    {
        var styles = new Styles();

        foreach (var (rawkey, rawvalue) in props)
        {
            if (!rawkey.TryRead<string>(out var key))
            {
                continue;
            }

            styles = key switch
            {
                // Layout / flex
                "direction" when rawvalue.TryRead<string>(out var v) => styles with { Direction = ParseDirection(v, styles.Direction) },
                "flex-direction" or "flexDirection" when rawvalue.TryRead<string>(out var v) => styles with { FlexDirection = ParseFlexDirection(v, styles.FlexDirection) },
                "justify-content" or "justifyContent" when rawvalue.TryRead<string>(out var v) => styles with { JustifyContent = ParseJustify(v, styles.JustifyContent) },
                "align-items" or "alignItems" when rawvalue.TryRead<string>(out var v) => styles with { AlignItems = ParseAlign(v, styles.AlignItems) },
                "align-self" or "alignSelf" when rawvalue.TryRead<string>(out var v) => styles with { AlignSelf = ParseAlign(v, styles.AlignSelf) },
                "align-content" or "alignContent" when rawvalue.TryRead<string>(out var v) => styles with { AlignContent = ParseAlign(v, styles.AlignContent) },
                "position" when rawvalue.TryRead<string>(out var v) => styles with { Position = ParsePosition(v, styles.Position) },
                "flex-wrap" or "flexWrap" when rawvalue.TryRead<string>(out var v) => styles with { FlexWrap = ParseWrap(v, styles.FlexWrap) },
                "overflow" when rawvalue.TryRead<string>(out var v) => styles with { Overflow = ParseOverflow(v, styles.Overflow) },
                "display" when rawvalue.TryRead<string>(out var v) => styles with { Display = ParseDisplay(v, styles.Display) },
                "box-sizing" or "boxSizing" when rawvalue.TryRead<string>(out var v) => styles with { BoxSizing = ParseBoxSizing(v, styles.BoxSizing) },
                "visibility" when rawvalue.TryRead<string>(out var v) => styles with { Visibility = ParseVisibility(v, styles.Visibility) },

                // Flex sizing
                "flex" => styles with { Flex = ParseNullableFloat(rawvalue, styles.Flex) },
                "flex-grow" or "flexGrow" => styles with { FlexGrow = ParseNullableFloat(rawvalue, styles.FlexGrow) },
                "flex-shrink" or "flexShrink" => styles with { FlexShrink = ParseNullableFloat(rawvalue, styles.FlexShrink) },
                "flex-basis" or "flexBasis" => styles with { FlexBasis = ParseFlexBasis(rawvalue, styles.FlexBasis) },

                // Position offsets
                "left" => styles with { Left = ParseMarginPosition(rawvalue, styles.Left) },
                "top" => styles with { Top = ParseMarginPosition(rawvalue, styles.Top) },
                "right" => styles with { Right = ParseMarginPosition(rawvalue, styles.Right) },
                "bottom" => styles with { Bottom = ParseMarginPosition(rawvalue, styles.Bottom) },

                // Margin
                "margin-top" or "marginTop" => styles with { MarginTop = ParseMarginPosition(rawvalue, styles.MarginTop) },
                "margin-bottom" or "marginBottom" => styles with { MarginBottom = ParseMarginPosition(rawvalue, styles.MarginBottom) },
                "margin-left" or "marginLeft" => styles with { MarginLeft = ParseMarginPosition(rawvalue, styles.MarginLeft) },
                "margin-right" or "marginRight" => styles with { MarginRight = ParseMarginPosition(rawvalue, styles.MarginRight) },
                "margin" => styles with
                {
                    MarginTop = ParseMarginPosition(rawvalue, styles.MarginTop),
                    MarginBottom = ParseMarginPosition(rawvalue, styles.MarginBottom),
                    MarginLeft = ParseMarginPosition(rawvalue, styles.MarginLeft),
                    MarginRight = ParseMarginPosition(rawvalue, styles.MarginRight)
                },

                // Padding
                "padding-top" or "paddingTop" => styles with { PaddingTop = ParsePadding(rawvalue, styles.PaddingTop) },
                "padding-bottom" or "paddingBottom" => styles with { PaddingBottom = ParsePadding(rawvalue, styles.PaddingBottom) },
                "padding-left" or "paddingLeft" => styles with { PaddingLeft = ParsePadding(rawvalue, styles.PaddingLeft) },
                "padding-right" or "paddingRight" => styles with { PaddingRight = ParsePadding(rawvalue, styles.PaddingRight) },
                "padding" => styles with
                {
                    PaddingTop = ParsePadding(rawvalue, styles.PaddingTop),
                    PaddingBottom = ParsePadding(rawvalue, styles.PaddingBottom),
                    PaddingLeft = ParsePadding(rawvalue, styles.PaddingLeft),
                    PaddingRight = ParsePadding(rawvalue, styles.PaddingRight),
                },

                // Border widths
                "border-top-width" or "borderTopWidth" => styles with { BorderTop = ParsePixels(rawvalue, styles.BorderTop) },
                "border-bottom-width" or "borderBottomWidth" => styles with { BorderBottom = ParsePixels(rawvalue, styles.BorderBottom) },
                "border-left-width" or "borderLeftWidth" => styles with { BorderLeft = ParsePixels(rawvalue, styles.BorderLeft) },
                "border-right-width" or "borderRightWidth" => styles with { BorderRight = ParsePixels(rawvalue, styles.BorderRight) },
                "border-width" or "borderWidth" => styles with
                {
                    BorderTop = ParsePixels(rawvalue, styles.BorderTop),
                    BorderBottom = ParsePixels(rawvalue, styles.BorderBottom),
                    BorderLeft = ParsePixels(rawvalue, styles.BorderLeft),
                    BorderRight = ParsePixels(rawvalue, styles.BorderRight)
                },

                // Gaps
                "column-gap" or "columnGap" => styles with { GapColumn = ParseGap(rawvalue, styles.GapColumn) },
                "row-gap" or "rowGap" => styles with { GapRow = ParseGap(rawvalue, styles.GapRow) },
                "gap" => styles with
                {
                    GapColumn = ParseGap(rawvalue, styles.GapColumn),
                    GapRow = ParseGap(rawvalue, styles.GapRow)
                },

                // Sizing
                "width" => styles with { Width = ParseWidthHeight(rawvalue, styles.Width) },
                "height" => styles with { Height = ParseWidthHeight(rawvalue, styles.Height) },
                "min-width" or "minWidth" => styles with { MinWidth = ParseWidthHeight(rawvalue, styles.MinWidth) },
                "min-height" or "minHeight" => styles with { MinHeight = ParseWidthHeight(rawvalue, styles.MinHeight) },
                "max-width" or "maxWidth" => styles with { MaxWidth = ParseWidthHeight(rawvalue, styles.MaxWidth) },
                "max-height" or "maxHeight" => styles with { MaxHeight = ParseWidthHeight(rawvalue, styles.MaxHeight) },
                "aspect-ratio" or "aspectRatio" => styles with { AspectRatio = ParseAspectRatio(rawvalue, styles.AspectRatio) },

                // Colors
                "border-color" or "borderColor" => styles with { BorderColor = ParseColor(rawvalue) ?? styles.BorderColor },
                "background-color" or "backgroundColor" => styles with { BackgroundColor = ParseColor(rawvalue) ?? styles.BackgroundColor },

                // Border radius
                "border-top-left-radius" or "borderTopLeftRadius" => styles with { BorderTopLeftRadius = ParseFloat(rawvalue, styles.BorderTopLeftRadius) },
                "border-top-right-radius" or "borderTopRightRadius" => styles with { BorderTopRightRadius = ParseFloat(rawvalue, styles.BorderTopRightRadius) },
                "border-bottom-left-radius" or "borderBottomLeftRadius" => styles with { BorderBottomLeftRadius = ParseFloat(rawvalue, styles.BorderBottomLeftRadius) },
                "border-bottom-right-radius" or "borderBottomRightRadius" => styles with { BorderBottomRightRadius = ParseFloat(rawvalue, styles.BorderBottomRightRadius) },
                "border-radius" or "borderRadius" => styles with
                {
                    BorderTopLeftRadius = ParseFloat(rawvalue, styles.BorderTopLeftRadius),
                    BorderTopRightRadius = ParseFloat(rawvalue, styles.BorderTopRightRadius),
                    BorderBottomLeftRadius = ParseFloat(rawvalue, styles.BorderBottomLeftRadius),
                    BorderBottomRightRadius = ParseFloat(rawvalue, styles.BorderBottomRightRadius),
                },

                // Opacity
                "opacity" => styles with { Opacity = ParseFloat(rawvalue, styles.Opacity) },

                _ => styles
            };
        }

        return styles;
    }

    private static Direction ParseDirection(string v, Direction current) => v switch
    {
        "ltr" => Direction.Ltr,
        "rtl" => Direction.Rtl,
        "inherit" => Direction.Inherit,
        _ => current
    };

    private static FlexDirection ParseFlexDirection(string v, FlexDirection current) => v switch
    {
        "row" => FlexDirection.Row,
        "row-reverse" => FlexDirection.RowReverse,
        "column" => FlexDirection.Column,
        "column-reverse" => FlexDirection.ColumnReverse,
        _ => current
    };

    private static Justify ParseJustify(string v, Justify current) => v switch
    {
        "flex-start" => Justify.FlexStart,
        "center" => Justify.Center,
        "flex-end" => Justify.FlexEnd,
        "space-between" => Justify.SpaceBetween,
        "space-around" => Justify.SpaceAround,
        "space-evenly" => Justify.SpaceEvenly,
        _ => current
    };

    private static Align ParseAlign(string v, Align current) => v switch
    {
        "auto" => Align.Auto,
        "flex-start" => Align.FlexStart,
        "center" => Align.Center,
        "flex-end" => Align.FlexEnd,
        "stretch" => Align.Stretch,
        "baseline" => Align.Baseline,
        "space-between" => Align.SpaceBetween,
        "space-around" => Align.SpaceAround,
        "space-evenly" => Align.SpaceEvenly,
        _ => current
    };

    private static Position ParsePosition(string v, Position current) => v switch
    {
        "static" => Position.Static,
        "relative" => Position.Relative,
        "absolute" => Position.Absolute,
        _ => current
    };

    private static Wrap ParseWrap(string v, Wrap current) => v switch
    {
        "nowrap" => Wrap.NoWrap,
        "wrap" => Wrap.Wrap,
        "wrap-reverse" => Wrap.WrapReverse,
        _ => current
    };

    private static Overflow ParseOverflow(string v, Overflow current) => v switch
    {
        "visible" => Overflow.Visible,
        "hidden" => Overflow.Hidden,
        "scroll" => Overflow.Scroll,
        _ => current
    };

    private static Display ParseDisplay(string v, Display current) => v switch
    {
        "flex" => Display.Flex,
        "none" => Display.None,
        "contents" => Display.Contents,
        _ => current
    };

    private static BoxSizing ParseBoxSizing(string v, BoxSizing current) => v switch
    {
        "border-box" => BoxSizing.BorderBox,
        "content-box" => BoxSizing.ContentBox,
        _ => current
    };

    private static Visibility ParseVisibility(string v, Visibility current) => v switch
    {
        "visible" => Visibility.Visible,
        "hidden" => Visibility.Hidden,
        "collapse" => Visibility.Hidden,
        _ => current
    };

    private static float ParseFloat(LuaValue value, float current)
    {
        if (value.TryRead<float>(out var f))
        {
            return f;
        }

        if (value.TryRead<string>(out var s))
        {
            var span = s.AsSpan();
            if (span.EndsWith("px"))
                span = span[..^2];

            if (float.TryParse(span, NumberStyles.Float, CultureInfo.InvariantCulture, out f))
            {
                return f;
            }
        }

        return current;
    }

    private static float ParseFontSize(LuaValue value, float current)
    {
        if (value.TryRead<float>(out var f))
        {
            return f;
        }

        if (value.TryRead<string>(out var s))
        {
            var span = s.AsSpan();
            if (span.EndsWith("pt"))
                span = span[..^2];

            if (float.TryParse(span, NumberStyles.Float, CultureInfo.InvariantCulture, out f))
            {
                return f;
            }
        }

        return current;
    }

    private static float? ParseNullableFloat(LuaValue value, float? current)
    {
        if (value.Type == LuaValueType.Nil)
        {
            return null;
        }

        if (value.TryRead<float>(out var f))
        {
            return f;
        }

        if (value.TryRead<string>(out var s))
        {
            var span = s.AsSpan();
            if (span.EndsWith("px"))
                span = span[..^2];

            if (float.TryParse(span, NumberStyles.Float, CultureInfo.InvariantCulture, out f))
            {
                return f;
            }
        }

        return current;
    }

    private static MeasurementWidthHeight ParseWidthHeight(LuaValue value, MeasurementWidthHeight current)
    {
        if (value.TryRead<float>(out var f))
        {
            return f;
        }

        if (value.TryRead<string>(out var s))
        {
            try { return MeasurementWidthHeight.FromString(s); }
            catch (FormatException) { return current; }
        }

        return current;
    }

    private static MeasurementMarginPosition ParseMarginPosition(LuaValue value, MeasurementMarginPosition current)
    {
        if (value.TryRead<float>(out var f))
        {
            return f;
        }

        if (value.TryRead<string>(out var s))
        {
            try { return MeasurementMarginPosition.FromString(s); }
            catch (FormatException) { return current; }
        }

        return current;
    }

    private static MeasurementPadding ParsePadding(LuaValue value, MeasurementPadding current)
    {
        if (value.TryRead<float>(out var f))
        {
            return f;
        }

        if (value.TryRead<string>(out var s))
        {
            try { return MeasurementPadding.FromString(s); }
            catch (FormatException) { return current; }
        }

        return current;
    }

    private static MeasurementGap ParseGap(LuaValue value, MeasurementGap current)
    {
        if (value.TryRead<float>(out var f))
        {
            return f;
        }

        if (value.TryRead<string>(out var s))
        {
            try { return MeasurementGap.FromString(s); }
            catch (FormatException) { return current; }
        }

        return current;
    }

    private static MeasurementFlexBasis ParseFlexBasis(LuaValue value, MeasurementFlexBasis current)
    {
        if (value.TryRead<float>(out var f))
        {
            return f;
        }

        if (value.TryRead<string>(out var s))
        {
            try { return MeasurementFlexBasis.FromString(s); }
            catch (FormatException) { return current; }
        }

        return current;
    }

    private static Pixels? ParsePixels(LuaValue value, Pixels? current)
    {
        if (value.TryRead<float>(out var f))
        {
            return f;
        }

        if (value.TryRead<string>(out var s))
        {
            try { return Pixels.FromString(s); }
            catch (FormatException) { return current; }
        }

        return current;
    }

    private static Pixels? ParseAspectRatio(LuaValue value, Pixels? current)
    {
        if (value.TryRead<float>(out var f))
        {
            return f;
        }

        if (value.TryRead<string>(out var s))
        {
            var slash = s.IndexOf('/');
            if (slash > 0)
            {
                if (float.TryParse(s[..slash], NumberStyles.Float, CultureInfo.InvariantCulture, out var w) &&
                    float.TryParse(s[(slash + 1)..], NumberStyles.Float, CultureInfo.InvariantCulture, out var h) &&
                    h != 0f)
                {
                    return w / h;
                }
            }
            else if (float.TryParse(s, NumberStyles.Float, CultureInfo.InvariantCulture, out var ratio))
            {
                return ratio;
            }
        }

        return current;
    }

    private static Color? ParseColor(LuaValue value)
    {
        if (!value.TryRead<string>(out var s))
        {
            return null;
        }

        var str = s.Trim();
        if (str.Length == 0)
        {
            return null;
        }

        if (str.Equals("transparent", StringComparison.OrdinalIgnoreCase))
        {
            return Color.Transparent;
        }

        if (str[0] == '#')
        {
            return ParseHexColor(str.AsSpan(1));
        }

        if (str.StartsWith("rgba", StringComparison.OrdinalIgnoreCase))
        {
            return ParseRgbaColor(str);
        }

        if (str.StartsWith("rgb", StringComparison.OrdinalIgnoreCase))
        {
            return ParseRgbColor(str);
        }

        return null;
    }

    private static Color? ParseHexColor(ReadOnlySpan<char> hex)
    {
        if (hex.Length != 3 && hex.Length != 4 && hex.Length != 6 && hex.Length != 8)
        {
            return null;
        }

        var digits = hex.Length <= 4 ? 1 : 2;
        var r = HexByte(hex, 0, digits);
        var g = HexByte(hex, digits, digits);
        var b = HexByte(hex, digits * 2, digits);
        var a = hex.Length is 4 or 8 ? HexByte(hex, digits * 3, digits) : 255;

        if (r < 0 || g < 0 || b < 0 || a < 0)
        {
            return null;
        }

        return new Color(r, g, b, a);
    }

    private static int HexByte(ReadOnlySpan<char> hex, int offset, int count)
    {
        var value = 0;
        for (var i = 0; i < count; i++)
        {
            var digit = HexDigit(hex[offset + i]);
            if (digit < 0)
            {
                return -1;
            }

            value = value * 16 + digit;
        }

        return count == 1 ? value * 17 : value;
    }

    private static int HexDigit(char c) => c switch
    {
        >= '0' and <= '9' => c - '0',
        >= 'a' and <= 'f' => c - 'a' + 10,
        >= 'A' and <= 'F' => c - 'A' + 10,
        _ => -1
    };

    private static Color? ParseRgbColor(string str)
    {
        var open = str.IndexOf('(');
        var close = str.LastIndexOf(')');
        if (open < 0 || close <= open)
        {
            return null;
        }

        var parts = str[(open + 1)..close].Split(',');
        if (parts.Length < 3)
        {
            return null;
        }

        var r = ParseColorChannel(parts[0]);
        var g = ParseColorChannel(parts[1]);
        var b = ParseColorChannel(parts[2]);
        var a = parts.Length >= 4 ? ParseColorChannel(parts[3]) : 255;

        if (r < 0 || g < 0 || b < 0 || a < 0)
        {
            return null;
        }

        return new Color(r, g, b, a);
    }

    private static Color? ParseRgbaColor(string str)
    {
        var open = str.IndexOf('(');
        var close = str.LastIndexOf(')');
        if (open < 0 || close <= open)
        {
            return null;
        }

        var parts = str[(open + 1)..close].Split(',');
        if (parts.Length < 3)
        {
            return null;
        }

        var r = ParseColorChannel(parts[0]);
        var g = ParseColorChannel(parts[1]);
        var b = ParseColorChannel(parts[2]);
        var a = parts.Length >= 4 ? ParseAlphaChannel(parts[3]) : 1;

        if (r < 0 || g < 0 || b < 0 || a < 0)
        {
            return null;
        }

        return new Color(r, g, b, (byte)MathF.Round(a * 255f));
    }

    private static int ParseColorChannel(string channel)
    {
        channel = channel.Trim();

        if (channel.EndsWith('%'))
        {
            if (float.TryParse(channel.AsSpan(0, channel.Length - 1), NumberStyles.Float, CultureInfo.InvariantCulture, out var percent))
            {
                return (int)MathF.Round(percent * 2.55f);
            }

            return -1;
        }

        if (float.TryParse(channel, NumberStyles.Float, CultureInfo.InvariantCulture, out var f))
        {
            return (int)MathF.Round(f);
        }

        return -1;
    }

    private static float ParseAlphaChannel(string channel)
    {
        channel = channel.Trim();

        if (channel.EndsWith('%'))
        {
            if (float.TryParse(channel.AsSpan(0, channel.Length - 1), NumberStyles.Float, CultureInfo.InvariantCulture, out var percent))
            {
                return percent / 100f;
            }

            return -1;
        }

        if (float.TryParse(channel, NumberStyles.Float, CultureInfo.InvariantCulture, out var f))
        {
            return f;
        }

        return -1;
    }
}