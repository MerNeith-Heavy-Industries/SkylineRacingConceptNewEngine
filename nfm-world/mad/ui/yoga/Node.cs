using System.Collections;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;
using Avalonia;
using Avalonia.Metadata;
using Maxine.Extensions;
using nfm_world.ui.yoga.xaml;
using Yoga;

namespace nfm_world.ui.yoga;

// ReSharper disable InconsistentNaming
[DebuggerDisplay("{DebugToString()}")]
public class Node : IDisposable, INamed
{
    internal static readonly YGConfigPtr Config;

    internal YGNodePtr NodeInternal = new(Config);

    internal string __INTERNAL_CtorCallerFilePath = "";
    internal int __INTERNAL_CtorCallerLineNumber = 0;
    internal string __INTERNAL_CtorCallerMemberName = "";

    internal static List<Node> __INTERNAL_YogaRootsThisFrame = new();

    #if DEBUG
    [MethodImpl(MethodImplOptions.NoInlining)]
    #endif
    public Node()
    {
        Children = new(this);

#if DEBUG
        var stackTrace = new StackTrace(1, true);
        // skip inherited constructors
        var stackFrame = stackTrace.GetFrames()
            .FirstOrDefault(e => e.GetMethod()?.DeclaringType?.IsAssignableTo(typeof(Node)) != true);
        __INTERNAL_CtorCallerFilePath = stackFrame?.GetFileName() ?? "";
        __INTERNAL_CtorCallerLineNumber = stackFrame?.GetFileLineNumber() ?? 0;
        __INTERNAL_CtorCallerMemberName = stackFrame?.GetMethod()?.Name ?? "";
#endif
    }

    [Content]
    public NodeChildCollection Children { get; }

    public string? Name { get; set; }

    public string DebugToString()
    {
        using var sb = new ValueStringBuilder(stackalloc char[ValueStringBuilder.StackallocCharBufferSizeLimit]);
        sb.Append($"Node(Name={Name}, LayoutX={LayoutX}, LayoutY={LayoutY}, LayoutWidth={LayoutWidth}, LayoutHeight={LayoutHeight})");
        foreach (var child in Children)
        {
            sb.AppendLine();
            sb.Append('{');
            sb.Append(child.DebugToString().Replace("\n", "\n  "));
            sb.Append('}');
        }
        return sb.ToString();
    }

    #region Layout

    // https://www.w3schools.com/css/css_boxmodel.asp
    private Vector2 _root;
    public Vector2 LayoutMarginPosition => _root + new Vector2(LayoutX, LayoutY);
    public Vector2 LayoutMarginSize => new(LayoutWidth, LayoutHeight);
    public Vector2 LayoutBorderPosition => _root + new Vector2(LayoutX + LayoutMarginLeft, LayoutY + LayoutMarginTop);
    public Vector2 LayoutBorderSize => new(LayoutWidth - (LayoutMarginLeft + LayoutMarginRight), LayoutHeight - (LayoutMarginTop + LayoutMarginBottom));
    public Vector2 LayoutPaddingPosition => _root + new Vector2(LayoutX + LayoutMarginLeft + LayoutBorderLeft, LayoutY + LayoutMarginTop + LayoutBorderTop);
    public Vector2 LayoutPaddingSize => new(LayoutWidth - (LayoutMarginLeft + LayoutMarginRight + LayoutBorderLeft + LayoutBorderRight), LayoutHeight - (LayoutMarginTop + LayoutMarginBottom + LayoutBorderTop + LayoutBorderBottom));
    public Vector2 LayoutContentPosition => _root + new Vector2(LayoutX + LayoutMarginLeft + LayoutBorderLeft + LayoutPaddingLeft, LayoutY + LayoutMarginTop + LayoutBorderTop + LayoutPaddingTop);
    public Vector2 LayoutContentSize => new(LayoutWidth - (LayoutMarginLeft + LayoutMarginRight + LayoutBorderLeft + LayoutBorderRight + LayoutPaddingLeft + LayoutPaddingRight), LayoutHeight - (LayoutMarginTop + LayoutMarginBottom + LayoutBorderTop + LayoutBorderBottom + LayoutPaddingTop + LayoutPaddingBottom));

    public Vector2 LayoutMargin => new(LayoutMarginLeft + LayoutMarginRight, LayoutMarginTop + LayoutMarginBottom);
    public Vector2 LayoutPadding => new(LayoutPaddingLeft + LayoutPaddingRight, LayoutPaddingTop + LayoutPaddingBottom);
    public Vector2 LayoutBorder => new(LayoutBorderLeft + LayoutBorderRight, LayoutBorderTop + LayoutBorderBottom);

    public float LayoutWidth => NodeInternal.LayoutWidth;
    public float LayoutHeight => NodeInternal.LayoutHeight;
    public float LayoutX => NodeInternal.LayoutX;
    public float LayoutY => NodeInternal.LayoutY;
    public YgDirection LayoutDirection => NodeInternal.LayoutDirection.ToNfmDirection();
    public bool HadOverflow => NodeInternal.HadOverflow;
    public float LayoutMarginTop => NodeInternal.LayoutMarginTop;
    public float LayoutMarginBottom => NodeInternal.LayoutMarginBottom;
    public float LayoutMarginLeft => NodeInternal.LayoutMarginLeft;
    public float LayoutMarginRight => NodeInternal.LayoutMarginRight;
    public float LayoutPaddingTop => NodeInternal.LayoutPaddingTop;
    public float LayoutPaddingBottom => NodeInternal.LayoutPaddingBottom;
    public float LayoutPaddingLeft => NodeInternal.LayoutPaddingLeft;
    public float LayoutPaddingRight => NodeInternal.LayoutPaddingRight;
    public float LayoutBorderTop => NodeInternal.LayoutBorderTop;
    public float LayoutBorderBottom => NodeInternal.LayoutBorderBottom;
    public float LayoutBorderLeft => NodeInternal.LayoutBorderLeft;
    public float LayoutBorderRight => NodeInternal.LayoutBorderRight;

    public bool HasNewLayout
    {
        get => NodeInternal.HasNewLayout;
        set => NodeInternal.HasNewLayout = value;
    }

    public bool IsDirty
    {
        get => NodeInternal.IsDirty;
        set => NodeInternal.IsDirty = value;
    }

    public bool IsReferenceBaseline
    {
        set => NodeInternal.IsReferenceBaseline = value;
        get => NodeInternal.IsReferenceBaseline;
    }

    public YgNodeType NodeType
    {
        get => NodeInternal.NodeType.ToNfmNodeType();
        set => NodeInternal.NodeType = value.ToYogaNodeType();
    }

    public bool AlwaysFormsContainingBlock
    {
        get => NodeInternal.AlwaysFormsContainingBlock;
        set => NodeInternal.AlwaysFormsContainingBlock = value;
    }

    #endregion

    #region Style

    public Visibility Visibility { get; set; } = Visibility.Visible;
    
    public float Opacity { get; set; } = 1.0f;

    // https://css-tricks.com/snippets/css/a-guide-to-flexbox/
    public YgDirection Direction
    {
        get => NodeInternal.Direction.ToNfmDirection();
        set => NodeInternal.Direction = value.ToYogaDirection();
    }
    public YgFlexDirection FlexDirection
    {
        get => NodeInternal.FlexDirection.ToNfmFlexDirection();
        set => NodeInternal.FlexDirection = value.ToYogaFlexDirection();
    }
    public YgJustify JustifyContent
    {
        get => NodeInternal.JustifyContent.ToNfmJustify();
        set => NodeInternal.JustifyContent = value.ToYogaJustify();
    }
    public YgAlign AlignItems
    {
        get => NodeInternal.AlignItems.ToNfmAlign();
        set => NodeInternal.AlignItems = value.ToYogaAlign();
    }
    public YgAlign AlignSelf
    {
        get => NodeInternal.AlignSelf.ToNfmAlign();
        set => NodeInternal.AlignSelf = value.ToYogaAlign();
    }
    public YgAlign AlignContent
    {
        get => NodeInternal.AlignContent.ToNfmAlign();
        set => NodeInternal.AlignContent = value.ToYogaAlign();
    }
    public YgPositionType Position
    {
        get => NodeInternal.PositionType.ToNfmPositionType();
        set => NodeInternal.PositionType = value.ToYogaPositionType();
    }
    public YgWrap FlexWrap
    {
        get => NodeInternal.FlexWrap.ToNfmWrap();
        set => NodeInternal.FlexWrap = value.ToYogaWrap();
    }
    public YgOverflow Overflow
    {
        get => NodeInternal.Overflow.ToNfmOverflow();
        set => NodeInternal.Overflow = value.ToYogaOverflow();
    }
    public YgDisplay Display
    {
        get => NodeInternal.Display.ToNfmDisplay();
        set => NodeInternal.Display = value.ToYogaDisplay();
    }

    public sealed class PixelsConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }
        
        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (value is string str)
            {
                var trimmed = str.AsSpan().Trim();
                if (trimmed.EndsWith("px"))
                {
                    if (float.TryParse(trimmed[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out var pointValue))
                    {
                        return pointValue;
                    }
                }
                else
                {
                    if (float.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var pointValue))
                    {
                        return pointValue;
                    }
                }
                
                throw new FormatException($"Cannot convert '{str}' to pixels.");
            }
            return base.ConvertFrom(context, culture, value);
        }
    }

    public class PixelsOrUndefinedConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
        {
            return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
        }
        
        public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
        {
            if (value is string str)
            {
                var trimmed = str.AsSpan().Trim();
                if (trimmed.Equals("undefined", StringComparison.OrdinalIgnoreCase))
                {
                    return null;
                }
                if (trimmed.Equals("none", StringComparison.OrdinalIgnoreCase))
                {
                    return (float?)0;
                }
                if (trimmed.EndsWith("px"))
                {
                    if (float.TryParse(trimmed[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out var pointValue))
                    {
                        return (float?)pointValue;
                    }
                }
                else
                {
                    if (float.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var pointValue))
                    {
                        return (float?)pointValue;
                    }
                }
                
                throw new FormatException($"Cannot convert '{str}' to pixels or undefined.");
            }
            return base.ConvertFrom(context, culture, value);
        }
    }

    public float Flex
    {
        get => NodeInternal.Flex;
        set => NodeInternal.Flex = value;
    }
    public float FlexGrow
    {
        get => NodeInternal.FlexGrow;
        set => NodeInternal.FlexGrow = value;
    }
    public float FlexShrink
    {
        get => NodeInternal.FlexShrink;
        set => NodeInternal.FlexShrink = value;
    }

    public Action<Node> Ref
    {
        set => value(this);
    }
    
    [TypeConverter(typeof(MeasurementFlexBasisTypeConverter))]
    public struct MeasurementFlexBasis
    {
        public sealed class MeasurementFlexBasisTypeConverter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
            {
                return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
            }

            public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
            {
                if (value is string str)
                {
                    var trimmed = str.AsSpan().Trim();
                    if (trimmed.Equals("undefined", StringComparison.OrdinalIgnoreCase))
                    {
                        return Undefined;
                    }
                    if (trimmed.Equals("auto", StringComparison.OrdinalIgnoreCase))
                    {
                        return Auto;
                    }
                    if (trimmed.Equals("max-content", StringComparison.OrdinalIgnoreCase))
                    {
                        return MaxContent;
                    }
                    if (trimmed.Equals("stretch", StringComparison.OrdinalIgnoreCase))
                    {
                        return Stretch;
                    }
                    if (trimmed.EndsWith("%", StringComparison.OrdinalIgnoreCase))
                    {
                        if (float.TryParse(trimmed[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var percentValue))
                        {
                            return Percent(percentValue);
                        }
                    }
                    else if (trimmed.EndsWith("px"))
                    {
                        if (float.TryParse(trimmed[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out var pointValue))
                        {
                            return Point(pointValue);
                        }
                    }
                    else
                    {
                        if (float.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var pointValue))
                        {
                            return Point(pointValue);
                        }
                    }
                    
                    throw new FormatException($"Cannot convert '{str}' to MeasurementFlexBasis. Expected 'auto', 'max-content', 'stretch', '<number>px', '<number>%', or '<number>'.");
                }
                return base.ConvertFrom(context, culture, value);
            }
        }

        public YGValue InternalValue;

        public static implicit operator MeasurementFlexBasis(float value)
        {
            return new MeasurementFlexBasis
            {
                InternalValue = new YGValue()
                {
                    unit = YGUnit.YGUnitPoint,
                    value = value
                }
            };
        }
        public static implicit operator MeasurementFlexBasis(YGValue value)
        {
            return new MeasurementFlexBasis
            {
                InternalValue = value
            };
        }
        public static implicit operator YGValue(MeasurementFlexBasis value)
        {
            return value.InternalValue;
        }

        public static MeasurementFlexBasis Undefined = new MeasurementFlexBasis
        {
            InternalValue = new YGValue()
            {
                unit = YGUnit.YGUnitUndefined
            }
        };

        public static MeasurementFlexBasis Auto =>
            new()
            {
                InternalValue = new YGValue()
                {
                    unit = YGUnit.YGUnitFitContent
                }
            };

        public static MeasurementFlexBasis MaxContent =>
            new()
            {
                InternalValue = new YGValue()
                {
                    unit = YGUnit.YGUnitMaxContent
                }
            };

        public static MeasurementFlexBasis Stretch =>
            new()
            {
                InternalValue = new YGValue()
                {
                    unit = YGUnit.YGUnitStretch
                }
            };

        public static MeasurementFlexBasis Percent(float value)
        {
            return new MeasurementFlexBasis
            {
                InternalValue = new YGValue()
                {
                    unit = YGUnit.YGUnitPercent,
                    value = value
                }
            };
        }

        public static MeasurementFlexBasis Point(float value)
        {
            return new MeasurementFlexBasis
            {
                InternalValue = new YGValue()
                {
                    unit = YGUnit.YGUnitPoint,
                    value = value
                }
            };
        }

        public static MeasurementFlexBasis FitContent =>
            new()
            {
                InternalValue = new YGValue()
                {
                    unit = YGUnit.YGUnitFitContent
                }
            };

        public MeasurementFlexBasis Scale(float scale)
        {
            if (InternalValue.unit == YGUnit.YGUnitPoint)
            {
                return Point(InternalValue.value * scale);
            }

            return this;
        }
    }

    public MeasurementFlexBasis FlexBasis
    {
        get;
        set
        {
            field = value;
            NodeInternal.FlexBasis = value.Scale(G.Scale);
        }
    } = MeasurementFlexBasis.Undefined;

    [TypeConverter(typeof(MeasurementMarginPositionTypeConverter))]
    public struct MeasurementMarginPosition
    {
        public sealed class MeasurementMarginPositionTypeConverter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
            {
                return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
            }
            
            public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
            {
                if (value is string str)
                {
                    var trimmed = str.AsSpan().Trim();
                    if (trimmed.Equals("undefined", StringComparison.OrdinalIgnoreCase))
                    {
                        return Undefined;
                    }
                    if (trimmed.Equals("auto", StringComparison.OrdinalIgnoreCase))
                    {
                        return Auto;
                    }
                    if (trimmed.EndsWith("%", StringComparison.OrdinalIgnoreCase))
                    {
                        if (float.TryParse(trimmed[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var percentValue))
                        {
                            return Percent(percentValue);
                        }
                    }
                    else if (trimmed.EndsWith("px"))
                    {
                        if (float.TryParse(trimmed[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out var pointValue))
                        {
                            return Point(pointValue);
                        }
                    }
                    else
                    {
                        if (float.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var pointValue))
                        {
                            return Point(pointValue);
                        }
                    }
                    
                    throw new FormatException($"Cannot convert '{str}' to MeasurementMarginPosition. Expected 'auto', '<number>px', '<number>%', or '<number>'.");
                }
                return base.ConvertFrom(context, culture, value);
            }
        }
        
        public YGValue InternalValue;

        public static implicit operator MeasurementMarginPosition(float value)
        {
            return new MeasurementMarginPosition
            {
                InternalValue = new YGValue()
                {
                    unit = YGUnit.YGUnitPoint,
                    value = value
                }
            };
        }
        public static implicit operator MeasurementMarginPosition(YGValue value)
        {
            return new MeasurementMarginPosition
            {
                InternalValue = value
            };
        }
        public static implicit operator YGValue(MeasurementMarginPosition value)
        {
            return value.InternalValue;
        }

        public static MeasurementMarginPosition Auto =>
            new()
            {
                InternalValue = new YGValue()
                {
                    unit = YGUnit.YGUnitAuto
                }
            };

        public static MeasurementMarginPosition Undefined => new MeasurementMarginPosition
        {
            InternalValue = new YGValue()
            {
                unit = YGUnit.YGUnitUndefined
            }
        };

        public static MeasurementMarginPosition Percent(float value)
        {
            return new MeasurementMarginPosition
            {
                InternalValue = new YGValue()
                {
                    unit = YGUnit.YGUnitPercent,
                    value = value
                }
            };
        }
        public static MeasurementMarginPosition Point(float value)
        {
            return new MeasurementMarginPosition
            {
                InternalValue = new YGValue()
                {
                    unit = YGUnit.YGUnitPoint,
                    value = value
                }
            };
        }

        public MeasurementMarginPosition Scale(float scale)
        {
            if (InternalValue.unit == YGUnit.YGUnitPoint)
            {
                return Point(InternalValue.value * scale);
            }

            return this;
        }
    }

    public MeasurementMarginPosition Left
    {
        get;
        set
        {
            field = value;
            NodeInternal.Left = value.Scale(G.Scale);
        }
    } = MeasurementMarginPosition.Undefined;

    public MeasurementMarginPosition Top
    {
        get;
        set
        {
            field = value;
            NodeInternal.Top = value.Scale(G.Scale);
        }
    } = MeasurementMarginPosition.Undefined;

    public MeasurementMarginPosition Right
    {
        get;
        set
        {
            field = value;
            NodeInternal.Right = value.Scale(G.Scale);
        }
    } = MeasurementMarginPosition.Undefined;

    public MeasurementMarginPosition Bottom
    {
        get;
        set
        {
            field = value;
            NodeInternal.Bottom = value.Scale(G.Scale);
        }
    } = MeasurementMarginPosition.Undefined;

    public MeasurementMarginPosition MarginTop
    {
        get;
        set
        {
            field = value;
            NodeInternal.MarginTop = value.Scale(G.Scale);
        }
    } = MeasurementMarginPosition.Undefined;

    public MeasurementMarginPosition MarginBottom
    {
        get;
        set
        {
            field = value;
            NodeInternal.MarginBottom = value.Scale(G.Scale);
        }
    } = MeasurementMarginPosition.Undefined;

    public MeasurementMarginPosition MarginLeft
    {
        get;
        set
        {
            field = value;
            NodeInternal.MarginLeft = value.Scale(G.Scale);
        }
    } = MeasurementMarginPosition.Undefined;

    public MeasurementMarginPosition MarginRight
    {
        get;
        set
        {
            field = value;
            NodeInternal.MarginRight = value.Scale(G.Scale);
        }
    } = MeasurementMarginPosition.Undefined;

    [TypeConverter(typeof(MeasurementPaddingTypeConverter))]
    public struct MeasurementPadding
    {
        public sealed class MeasurementPaddingTypeConverter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
            {
                return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
            }

            public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
            {
                if (value is string str)
                {
                    var trimmed = str.AsSpan().Trim();
                    if (trimmed.Equals("undefined", StringComparison.OrdinalIgnoreCase))
                    {
                        return Undefined;
                    }
                    if (trimmed.EndsWith("%", StringComparison.OrdinalIgnoreCase))
                    {
                        if (float.TryParse(trimmed[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var percentValue))
                        {
                            return Percent(percentValue);
                        }
                    }
                    else if (trimmed.EndsWith("px"))
                    {
                        if (float.TryParse(trimmed[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out var pointValue))
                        {
                            return Point(pointValue);
                        }
                    }
                    else
                    {
                        if (float.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var pointValue))
                        {
                            return Point(pointValue);
                        }
                    }
                    
                    throw new FormatException($"Cannot convert '{str}' to MeasurementPadding. Expected '<number>px', '<number>%', or '<number>'.");
                }
                return base.ConvertFrom(context, culture, value);
            }
        }

        public YGValue InternalValue;

        public static implicit operator MeasurementPadding(float value)
        {
            return new MeasurementPadding
            {
                InternalValue = new YGValue()
                {
                    unit = YGUnit.YGUnitPoint,
                    value = value
                }
            };
        }
        public static implicit operator MeasurementPadding(YGValue value)
        {
            return new MeasurementPadding
            {
                InternalValue = value
            };
        }
        public static implicit operator YGValue(MeasurementPadding value)
        {
            return value.InternalValue;
        }

        public static MeasurementPadding Undefined => new MeasurementPadding
        {
            InternalValue = new YGValue()
            {
                unit = YGUnit.YGUnitUndefined
            }
        };

        public static MeasurementPadding Percent(float value)
        {
            return new MeasurementPadding
            {
                InternalValue = new YGValue()
                {
                    unit = YGUnit.YGUnitPercent,
                    value = value
                }
            };
        }
        public static MeasurementPadding Point(float value)
        {
            return new MeasurementPadding
            {
                InternalValue = new YGValue()
                {
                    unit = YGUnit.YGUnitPoint,
                    value = value
                }
            };
        }

        public MeasurementPadding Scale(float scale)
        {
            if (InternalValue.unit == YGUnit.YGUnitPoint)
            {
                return Point(InternalValue.value * scale);
            }

            return this;
        }
    }

    public MeasurementPadding Padding
    {
        set
        {
            PaddingLeft = value;
            PaddingRight = value;
            PaddingTop = value;
            PaddingBottom = value;
        }
    }

    public MeasurementPadding PaddingTop
    {
        get;
        set
        {
            field = value;
            NodeInternal.PaddingTop = value.Scale(G.Scale);
        }
    } = MeasurementPadding.Undefined;

    public MeasurementPadding PaddingBottom
    {
        get;
        set
        {
            field = value;
            NodeInternal.PaddingBottom = value.Scale(G.Scale);
        }
    } = MeasurementPadding.Undefined;

    public MeasurementPadding PaddingLeft
    {
        get;
        set
        {
            field = value;
            NodeInternal.PaddingLeft = value.Scale(G.Scale);
        }
    } = MeasurementPadding.Undefined;

    public MeasurementPadding PaddingRight
    {
        get;
        set
        {
            field = value;
            NodeInternal.PaddingRight = value.Scale(G.Scale);
        }
    } = MeasurementPadding.Undefined;

    [TypeConverter(typeof(PixelsConverter))]
    public float Border
    {
        set
        {
            BorderLeft = value;
            BorderRight = value;
            BorderTop = value;
            BorderBottom = value;
        }
    }

    [TypeConverter(typeof(PixelsOrUndefinedConverter))]
    public float? BorderTop
    {
        get;
        set
        {
            field = value;
            NodeInternal.BorderTop = (value * G.Scale) ?? YG.YGUndefined;
        }
    }

    [TypeConverter(typeof(PixelsOrUndefinedConverter))]
    public float? BorderBottom
    {
        get;
        set
        {
            field = value;
            NodeInternal.BorderBottom = (value * G.Scale) ?? YG.YGUndefined;
        }
    }
    
    [TypeConverter(typeof(PixelsOrUndefinedConverter))]
    public float? BorderLeft
    {
        get;
        set
        {
            field = value;
            NodeInternal.BorderLeft = (value * G.Scale) ?? YG.YGUndefined;
        }
    }
    
    [TypeConverter(typeof(PixelsOrUndefinedConverter))]
    public float? BorderRight
    {
        get;
        set
        {
            field = value;
            NodeInternal.BorderRight = (value * G.Scale) ?? YG.YGUndefined;
        }
    }

    [TypeConverter(typeof(MeasurementGapTypeConverter))]
    public struct MeasurementGap
    {
        public class MeasurementGapTypeConverter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
            {
                return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
            }
            
            public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
            {
                if (value is string str)
                {
                    var trimmed = str.AsSpan().Trim();
                    if (trimmed.Equals("undefined", StringComparison.OrdinalIgnoreCase))
                    {
                        return Undefined;
                    }
                    if (trimmed.EndsWith("%", StringComparison.OrdinalIgnoreCase))
                    {
                        if (float.TryParse(trimmed[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var percentValue))
                        {
                            return Percent(percentValue);
                        }
                    }
                    else if (trimmed.EndsWith("px"))
                    {
                        if (float.TryParse(trimmed[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out var pointValue))
                        {
                            return Point(pointValue);
                        }
                    }
                    else
                    {
                        if (float.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var pointValue))
                        {
                            return Point(pointValue);
                        }
                    }
                    
                    throw new FormatException($"Cannot convert '{str}' to MeasurementGap. Expected '<number>px', '<number>%', or '<number>'.");
                }
                return base.ConvertFrom(context, culture, value);
            }
        }

        public YGValue InternalValue;

        public static implicit operator MeasurementGap(float value)
        {
            return new MeasurementGap
            {
                InternalValue = new YGValue()
                {
                    unit = YGUnit.YGUnitPoint,
                    value = value
                }
            };
        }
        public static implicit operator MeasurementGap(YGValue value)
        {
            return new MeasurementGap
            {
                InternalValue = value
            };
        }
        public static implicit operator YGValue(MeasurementGap value)
        {
            return value.InternalValue;
        }

        public static MeasurementGap Undefined => new MeasurementGap
        {
            InternalValue = new YGValue()
            {
                unit = YGUnit.YGUnitUndefined
            }
        };

        public static MeasurementGap Percent(float value)
        {
            return new MeasurementGap
            {
                InternalValue = new YGValue()
                {
                    unit = YGUnit.YGUnitPercent,
                    value = value
                }
            };
        }
        public static MeasurementGap Point(float value)
        {
            return new MeasurementGap
            {
                InternalValue = new YGValue()
                {
                    unit = YGUnit.YGUnitPoint,
                    value = value
                }
            };
        }

        public MeasurementGap Scale(float scale)
        {
            if (InternalValue.unit == YGUnit.YGUnitPoint)
            {
                return Point(InternalValue.value * scale);
            }

            return this;
        }
    }

    public MeasurementGap Gap
    {
        set
        {
            GapColumn = value;
            GapRow = value;
        }
    }

    public MeasurementGap GapColumn
    {
        get;
        set
        {
            field = value;
            NodeInternal.GapColumn = value;
        }
    } = MeasurementGap.Undefined;

    public MeasurementGap GapRow
    {
        get;
        set
        {
            field = value;
            NodeInternal.GapRow = value;
        }
    } = MeasurementGap.Undefined;

    public YgBoxSizing BoxSizing
    {
        get => NodeInternal.BoxSizing.ToNfmBoxSizing();
        set => NodeInternal.BoxSizing = value.ToYogaBoxSizing();
    }

    [TypeConverter(typeof(MeasurementWidthHeightTypeConverter))]
    public struct MeasurementWidthHeight
    {
        /// <summary>
        /// Type converter for Node.MeasurementWidthHeight.
        /// Parses values like "100", "50%", "auto", "stretch", etc.
        /// </summary>
        public class MeasurementWidthHeightTypeConverter : TypeConverter
        {
            public override bool CanConvertFrom(ITypeDescriptorContext? context, Type sourceType)
            {
                return sourceType == typeof(string) || base.CanConvertFrom(context, sourceType);
            }

            public override object? ConvertFrom(ITypeDescriptorContext? context, CultureInfo? culture, object value)
            {
                if (value is string str)
                {
                    var trimmed = str.Trim();

                    if (trimmed.Equals("auto", StringComparison.OrdinalIgnoreCase))
                    {
                        return Auto();
                    }
                    if (trimmed.Equals("stretch", StringComparison.OrdinalIgnoreCase))
                    {
                        return Stretch();
                    }
                    if (trimmed.Equals("fit-content", StringComparison.OrdinalIgnoreCase))
                    {
                        return FitContent();
                    }
                    if (trimmed.Equals("max-content", StringComparison.OrdinalIgnoreCase))
                    {
                        return MaxContent();
                    }
                    if (trimmed.EndsWith('%'))
                    {
                        if (float.TryParse(trimmed[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var percentValue))
                        {
                            return Percent(percentValue);
                        }
                    }
                    else if (trimmed.EndsWith("px", StringComparison.OrdinalIgnoreCase))
                    {
                        if (float.TryParse(trimmed[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out var floatValue))
                        {
                            return Point(floatValue);
                        }
                    }
                    else if (float.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var floatValue))
                    {
                        return Point(floatValue);
                    }

                    throw new FormatException($"Cannot convert {str} to MeasurementWidthHeight. Expected a number, percentage, 'auto', 'stretch', 'fit-content', or 'max-content'.");
                }
                return base.ConvertFrom(context, culture, value);
            }
        }

        public YGValue InternalValue;

        public static implicit operator MeasurementWidthHeight(float value)
        {
            return new MeasurementWidthHeight
            {
                InternalValue = new YGValue()
                {
                    unit = YGUnit.YGUnitPoint,
                    value = value
                }
            };
        }
        public static implicit operator MeasurementWidthHeight(YGValue value)
        {
            return new MeasurementWidthHeight
            {
                InternalValue = value
            };
        }
        public static implicit operator YGValue(MeasurementWidthHeight value)
        {
            return value.InternalValue;
        }

        public static MeasurementWidthHeight Undefined => new MeasurementWidthHeight
        {
            InternalValue = new YGValue()
            {
                unit = YGUnit.YGUnitUndefined
            }
        };

        public static MeasurementWidthHeight Auto()
        {
            return new MeasurementWidthHeight
            {
                InternalValue = new YGValue()
                {
                    unit = YGUnit.YGUnitAuto
                }
            };
        }
        public static MeasurementWidthHeight Percent(float value)
        {
            return new MeasurementWidthHeight
            {
                InternalValue = new YGValue()
                {
                    unit = YGUnit.YGUnitPercent,
                    value = value
                }
            };
        }
        public static MeasurementWidthHeight Point(float value)
        {
            return new MeasurementWidthHeight
            {
                InternalValue = new YGValue()
                {
                    unit = YGUnit.YGUnitPoint,
                    value = value
                }
            };
        }

        public static MeasurementWidthHeight FitContent()
        {
            return new MeasurementWidthHeight
            {
                InternalValue = new YGValue()
                {
                    unit = YGUnit.YGUnitFitContent
                }
            };
        }
        public static MeasurementWidthHeight MaxContent()
        {
            return new MeasurementWidthHeight
            {
                InternalValue = new YGValue()
                {
                    unit = YGUnit.YGUnitMaxContent
                }
            };
        }

        public static MeasurementWidthHeight Stretch()
        {
            return new MeasurementWidthHeight
            {
                InternalValue = new YGValue()
                {
                    unit = YGUnit.YGUnitStretch
                }
            };
        }

        public MeasurementWidthHeight Scale(float scale)
        {
            if (InternalValue.unit == YGUnit.YGUnitPoint)
            {
                return Point(InternalValue.value * scale);
            }

            return this;
        }
    }

    public MeasurementWidthHeight Width
    {
        get;
        set
        {
            field = value;
            NodeInternal.Width = value.Scale(G.Scale);
        }
    } = MeasurementWidthHeight.Undefined;

    public MeasurementWidthHeight Height
    {
        get;
        set
        {
            field = value;
            NodeInternal.Height = value.Scale(G.Scale);
        }
    } = MeasurementWidthHeight.Undefined;

    public MeasurementWidthHeight MinWidth
    {
        get;
        set
        {
            field = value;
            NodeInternal.MinWidth = value.Scale(G.Scale);
        }
    } = MeasurementWidthHeight.Undefined;

    public MeasurementWidthHeight MinHeight
    {
        get;
        set
        {
            field = value;
            NodeInternal.MinHeight = value.Scale(G.Scale);
        }
    } = MeasurementWidthHeight.Undefined;

    public MeasurementWidthHeight MaxWidth
    {
        get;
        set
        {
            field = value;
            NodeInternal.MaxWidth = value.Scale(G.Scale);
        }
    } = MeasurementWidthHeight.Undefined;

    public MeasurementWidthHeight MaxHeight
    {
        get;
        set
        {
            field = value;
            NodeInternal.MaxHeight = value.Scale(G.Scale);
        }
    } = MeasurementWidthHeight.Undefined;

    [TypeConverter(typeof(PixelsConverter))]
    public float AspectRatio
    {
        get => NodeInternal.AspectRatio;
        set => NodeInternal.AspectRatio = value;
    }

    #endregion

    private float _lastScale = 1f;

    static Node()
    {
        Config = YGConfigPtr.GetDefault();
        Config.UseWebDefaults = true;
    }

    ~Node()
    {
        Dispose(false);
    }

    private void ReleaseUnmanagedResources()
    {
        NodeInternal.Dispose();
    }

    protected virtual void Dispose(bool disposing)
    {
        ReleaseUnmanagedResources();
        if (disposing)
        {
            // Free any other managed objects here.
        }
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    private bool Rescale()
    {
        if (Math.Abs(_lastScale - G.Scale) > 0.001f)
        {
            // Update all size related properties to trigger re-calculation with new scale
#pragma warning disable CA2245
            Width = Width;
            Height = Height;
            MinWidth = MinWidth;
            MinHeight = MinHeight;
            MaxWidth = MaxWidth;
            MaxHeight = MaxHeight;
            MarginTop = MarginTop;
            MarginBottom = MarginBottom;
            MarginLeft = MarginLeft;
            MarginRight = MarginRight;
            PaddingTop = PaddingTop;
            PaddingBottom = PaddingBottom;
            PaddingLeft = PaddingLeft;
            PaddingRight = PaddingRight;
            BorderTop = BorderTop;
            BorderBottom = BorderBottom;
            BorderLeft = BorderLeft;
            BorderRight = BorderRight;
            GapColumn = GapColumn;
            GapRow = GapRow;
            FlexBasis = FlexBasis;
            Left = Left;
            Top = Top;
            Right = Right;
            Bottom = Bottom;
#pragma warning restore CA2245

            _lastScale = G.Scale;

            return true;
        }

        return false;
    }

    protected virtual void OnScaleChanged()
    {
    }

    private void RescaleRecursive()
    {
        if (Rescale())
        {
            OnScaleChanged();
            foreach (var child in Children)
            {
                child.RescaleRecursive();
            }
        }
    }

    protected virtual void RenderBackground(Vector2 position, Vector2 size)
    {
    }

    protected virtual void RenderBorder(Vector2 position, Vector2 size)
    {
    }

    protected virtual void RenderContent(Vector2 position, Vector2 size)
    {
    }

    protected virtual void Render()
    {
        G.SetAlpha(Opacity);
        RenderBackground(LayoutPaddingPosition, LayoutPaddingSize);
        RenderBorder(LayoutBorderPosition, LayoutBorderSize);
        RenderContent(LayoutContentPosition, LayoutContentSize);
        G.SetAlpha(1f);
    }

    private void RenderRecursive(Vector2 root, float rootOpacity = 1f)
    {
        _root = root;
        if (Display != YgDisplay.None && Visibility == Visibility.Visible && Opacity > 0f)
        {
            var ownOpacity = rootOpacity * Opacity;
            G.SetAlpha(ownOpacity);
            Render();
            foreach (var child in Children)
            {
                child.RenderRecursive(root + new Vector2(LayoutX, LayoutY), ownOpacity); // todo should this be LayoutContentPosition
            }
            G.SetAlpha(1f);
        }
    }

    protected virtual void GameTick()
    {
    }

    public void LayoutAndRender(Vector2 availableSize, Vector2? origin = null)
    {
#if DEBUG
        __INTERNAL_YogaRootsThisFrame.Add(this);
#endif

        RescaleRecursive();
        NodeInternal.CalculateLayout(availableSize, YGDirection.YGDirectionLTR);
        RenderRecursive(origin ?? Vector2.Zero);
    }

    public void Update()
    {
        GameTick();
        foreach (var child in Children)
        {
            child.Update();
        }
    }
}

public class NodeChildCollection(Node parent) : IList<Node>
{
    private List<Node> _internalList = new();

    public IEnumerator<Node> GetEnumerator()
    {
        return _internalList.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public void Add(Node item)
    {
        parent.NodeInternal.InsertChild(item.NodeInternal, parent.NodeInternal.GetChildCount());
        _internalList.Add(item);
    }

    public void Clear()
    {
        parent.NodeInternal.RemoveAllChildren();
        _internalList.Clear();
    }

    public bool Contains(Node item)
    {
        return _internalList.Contains(item);
    }

    public void CopyTo(Node[] array, int arrayIndex)
    {
        _internalList.CopyTo(array, arrayIndex);
    }

    public bool Remove(Node item)
    {
        if (_internalList.Remove(item))
        {
            parent.NodeInternal.RemoveChild(item.NodeInternal);
            return true;
        }

        return false;
    }

    public int Count => _internalList.Count;
    public bool IsReadOnly => false;
    public int IndexOf(Node item)
    {
        return _internalList.IndexOf(item);
    }

    public void Insert(int index, Node item)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(index, 0);
        parent.NodeInternal.InsertChild(item.NodeInternal, (uint)index);
        _internalList.Insert(index, item);
    }

    public void RemoveAt(int index)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(index, 0);
        var item = _internalList[index];
        parent.NodeInternal.RemoveChild(item.NodeInternal);
        _internalList.RemoveAt(index);
    }

    public Node this[int index]
    {
        get => _internalList[index];
        set
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(index, 0);
            _internalList[index] = value;
            parent.NodeInternal.SwapChild(value.NodeInternal, (uint)index);
        }
    }
}