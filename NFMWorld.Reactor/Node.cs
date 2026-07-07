using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Numerics;
using System.Runtime.CompilerServices;
using NFMWorld.DriverInterface;
using NFMWorld.Reactor.Events;
using Yoga;

namespace NFMWorld.Reactor;

// ReSharper disable InconsistentNaming
/// <summary>
/// Represents a single node in the Yoga layout system.
/// </summary>
[DebuggerDisplay("{DebugToString()}")]
public partial class Node : Visual, IAnimationCallback, IDisposable
{
    internal static readonly YGConfigPtr Config;
    internal YGNodePtr NodeInternal = new(Config);
    internal static readonly List<Node> __INTERNAL_YogaRootsThisFrame = [];

    internal readonly string __INTERNAL_CtorCallerFilePath = "";
    internal readonly int __INTERNAL_CtorCallerLineNumber = 0;
    internal readonly string __INTERNAL_CtorCallerMemberName = "";

    public virtual bool DebugIsContentfulNode => false;

    static Node()
    {
        Config = YGConfigPtr.GetDefault();
        Config.UseWebDefaults = true;
    }

    // ── Visual abstracts ────────────────────────────────────────────────
    public override IReadOnlyList<Visual> VisualChildren => [];
    internal override YGNodePtr Contents => NodeInternal;

    // ── Children API (no-op for leaf nodes) ──────────────────────────────
    public override bool CanHaveChildren => false;
    public override void AddChild(Visual child) { }
    public override void InsertAt(int index, Visual child) { }
    public override void RemoveAt(int index) { }

    // ── IDisposable ─────────────────────────────────────────────────────
    ~Node() { Dispose(false); }
    public void Dispose() { Dispose(true); GC.SuppressFinalize(this); }
    protected virtual void Dispose(bool disposing)
    {
        NodeInternal.Dispose();
    }
    
    [Property]
    public Action? AnimationFrameBegan { get; set; }

    [EditorBrowsable(EditorBrowsableState.Never)]
    public virtual string DebugToString()
    {
        return $"Node(Name={Name}, LayoutX={LayoutX}, LayoutY={LayoutY}, LayoutWidth={LayoutWidth}, LayoutHeight={LayoutHeight})";
    }

    #region Animations

    /// <summary>
    /// Triggered when <see cref="Visibility"/> is set to <see cref="Visibility.Visible"/>
    /// </summary>
    public Action? Shown { get; set; }
    
    /// <summary>
    /// Triggered when <see cref="Visibility"/> is set to <see cref="Visibility.Hidden"/>
    /// </summary>
    public Action? Hidden { get; set; }

    #endregion

    #region Layout

    // https://www.w3schools.com/css/css_boxmodel.asp
    private protected Vector2 _root;
    
    /// <summary>
    /// In the CSS box model, gets the top-left position of the margin box.
    /// </summary>
    public Vector2 LayoutMarginPosition => _root + new Vector2(LayoutX, LayoutY);
    
    /// <summary>
    /// In the CSS box model, gets the size of the margin box, from the top-left to the bottom-right.
    /// </summary>
    public Vector2 LayoutMarginSize => new(LayoutWidth, LayoutHeight);
    
    /// <summary>
    /// In the CSS box model, gets the top-left position of the border box.
    /// </summary>
    public Vector2 LayoutBorderPosition => _root + new Vector2(LayoutX + LayoutMarginLeft, LayoutY + LayoutMarginTop);
    
    /// <summary>
    /// In the CSS box model, gets the size of the border box, from the top-left to the bottom-right.
    /// </summary>
    public Vector2 LayoutBorderSize => new(LayoutWidth - (LayoutMarginLeft + LayoutMarginRight), LayoutHeight - (LayoutMarginTop + LayoutMarginBottom));
    
    /// <summary>
    /// In the CSS box model, gets the top-left position of the padding box.
    /// </summary>
    public Vector2 LayoutPaddingPosition => _root + new Vector2(LayoutX + LayoutMarginLeft + LayoutBorderLeft, LayoutY + LayoutMarginTop + LayoutBorderTop);
    
    /// <summary>
    /// In the CSS box model, gets the size of the padding box, from the top-left to the bottom-right.
    /// </summary>
    public Vector2 LayoutPaddingSize => new(LayoutWidth - (LayoutMarginLeft + LayoutMarginRight + LayoutBorderLeft + LayoutBorderRight), LayoutHeight - (LayoutMarginTop + LayoutMarginBottom + LayoutBorderTop + LayoutBorderBottom));
    
    /// <summary>
    /// In the CSS box model, gets the top-left position of the content box.
    /// </summary>
    public Vector2 LayoutContentPosition => _root + new Vector2(LayoutX + LayoutMarginLeft + LayoutBorderLeft + LayoutPaddingLeft, LayoutY + LayoutMarginTop + LayoutBorderTop + LayoutPaddingTop);
    
    /// <summary>
    /// In the CSS box model, gets the size of the content box, from the top-left to the bottom-right.
    /// </summary>
    public Vector2 LayoutContentSize => new(LayoutWidth - (LayoutMarginLeft + LayoutMarginRight + LayoutBorderLeft + LayoutBorderRight + LayoutPaddingLeft + LayoutPaddingRight), LayoutHeight - (LayoutMarginTop + LayoutMarginBottom + LayoutBorderTop + LayoutBorderBottom + LayoutPaddingTop + LayoutPaddingBottom));

    /// <summary>
    /// Gets the margin width and height of the node as a <see cref="Vector2"/>.
    /// </summary>
    public Vector2 LayoutMargin => new(LayoutMarginLeft + LayoutMarginRight, LayoutMarginTop + LayoutMarginBottom);
    
    /// <summary>
    /// Gets the padding width and height of the node as a <see cref="Vector2"/>.
    /// </summary>
    public Vector2 LayoutPadding => new(LayoutPaddingLeft + LayoutPaddingRight, LayoutPaddingTop + LayoutPaddingBottom);
    
    /// <summary>
    /// Gets the border width and height of the node as a <see cref="Vector2"/>.
    /// </summary>
    public Vector2 LayoutBorder => new(LayoutBorderLeft + LayoutBorderRight, LayoutBorderTop + LayoutBorderBottom);

    /// <summary>
    /// Gets the width of the node's layout as determined by the Yoga layout engine after a layout pass.
    /// This value is in points and does not include margins, borders, or padding.
    /// </summary>
    public float LayoutWidth => NodeInternal.LayoutWidth;
    
    /// <summary>
    /// Gets the height of the node's layout as determined by the Yoga layout engine after a layout pass.
    /// This value is in points and does not include margins, borders, or padding.
    /// </summary>
    public float LayoutHeight => NodeInternal.LayoutHeight;
    
    /// <summary>
    /// Gets the X position of the node's layout as determined by the Yoga layout engine after a layout pass.
    /// This value is in points and represents the distance from the left edge of the parent node's content box to the left edge of this node's margin box.
    /// </summary>
    public float LayoutX => NodeInternal.LayoutX;
    
    /// <summary>
    /// Gets the Y position of the node's layout as determined by the Yoga layout engine after a layout pass.
    /// This value is in points and represents the distance from the top edge of the parent node's content box to the top edge of this node's margin box.
    /// </summary>
    public float LayoutY => NodeInternal.LayoutY;
    
    /// <summary>
    /// Gets the layout direction of the node as determined by the Yoga layout engine after a layout pass.
    /// </summary>
    public Direction LayoutDirection => NodeInternal.LayoutDirection.ToNfmDirection();
    
    /// <summary>
    /// Gets a value indicating whether the node's content overflowed its layout bounds during the last layout pass.
    /// </summary>
    public bool HadOverflow => NodeInternal.HadOverflow;
    
    /// <summary>
    /// Gets the top margin of the node's layout as determined by the Yoga layout engine after a layout pass.
    /// This value is in points and represents the distance from the top edge of this node's margin box to the top edge of its border box.
    /// </summary>
    public float LayoutMarginTop => NodeInternal.LayoutMarginTop;
    
    /// <summary>
    /// Gets the bottom margin of the node's layout as determined by the Yoga layout engine after a layout pass.
    /// This value is in points and represents the distance from the bottom edge of this node's margin box to the bottom edge of its border box.
    /// </summary>
    public float LayoutMarginBottom => NodeInternal.LayoutMarginBottom;
    
    /// <summary>
    /// Gets the left margin of the node's layout as determined by the Yoga layout engine after a layout pass.
    /// This value is in points and represents the distance from the left edge of this node's margin box to the left edge of its border box.
    /// </summary>
    public float LayoutMarginLeft => NodeInternal.LayoutMarginLeft;
    
    /// <summary>
    /// Gets the right margin of the node's layout as determined by the Yoga layout engine after a layout pass.
    /// This value is in points and represents the distance from the right edge of this node's margin box to the right edge of its border box.
    /// This value is in points and represents the distance from the right edge of this node's margin box to the right edge of its border box.
    /// </summary>
    public float LayoutMarginRight => NodeInternal.LayoutMarginRight;
    
    /// <summary>
    /// Gets the top padding of the node's layout as determined by the Yoga layout engine after a layout pass.
    /// This value is in points and represents the distance from the top edge of this node's border box to the top edge of its padding box.
    /// </summary>
    public float LayoutPaddingTop => NodeInternal.LayoutPaddingTop;
    
    /// <summary>
    /// Gets the bottom padding of the node's layout as determined by the Yoga layout engine after a layout pass.
    /// This value is in points and represents the distance from the bottom edge of this node's border box to the bottom edge of its padding box.
    /// </summary>
    public float LayoutPaddingBottom => NodeInternal.LayoutPaddingBottom;
    
    /// <summary>
    /// Gets the left padding of the node's layout as determined by the Yoga layout engine after a layout pass.
    /// This value is in points and represents the distance from the left edge of this node's border box to the left edge of its padding box.
    /// </summary>
    public float LayoutPaddingLeft => NodeInternal.LayoutPaddingLeft;
    
    /// <summary>
    /// Gets the right padding of the node's layout as determined by the Yoga layout engine after a layout pass.
    /// This value is in points and represents the distance from the right edge of this node's border box to the right edge of its padding box.
    /// </summary>
    public float LayoutPaddingRight => NodeInternal.LayoutPaddingRight;
    
    /// <summary>
    /// Gets the top border of the node's layout as determined by the Yoga layout engine after a layout pass.
    /// This value is in points and represents the distance from the top edge of this node's border box to the top edge of its margin box.
    /// </summary>
    public float LayoutBorderTop => NodeInternal.LayoutBorderTop;
    
    /// <summary>
    /// Gets the bottom border of the node's layout as determined by the Yoga layout engine after a layout pass.
    /// This value is in points and represents the distance from the bottom edge of this node's border box to the bottom edge of its margin box.
    /// </summary>
    public float LayoutBorderBottom => NodeInternal.LayoutBorderBottom;
    
    /// <summary>
    /// Gets the left border of the node's layout as determined by the Yoga layout engine after a layout pass.
    /// This value is in points and represents the distance from the left edge of this node's border box to the left edge of its margin box.
    /// </summary>
    public float LayoutBorderLeft => NodeInternal.LayoutBorderLeft;
    
    /// <summary>
    /// Gets the right border of the node's layout as determined by the Yoga layout engine after a layout pass.
    /// This value is in points and represents the distance from the right edge of this node's border box to the right edge of its margin box.
    /// </summary>
    public float LayoutBorderRight => NodeInternal.LayoutBorderRight;

    /// <summary>
    /// Gets or sets whether the node's Yoga layout changed. Must be reset by setting it to false.
    /// </summary>
    public bool HasNewLayout
    {
        get => NodeInternal.HasNewLayout;
        set => NodeInternal.HasNewLayout = value;
    }

    /// <summary>
    /// Gets or sets whether the node's Yoga layout results are dirty due to it or its children changing.
    /// </summary>
    public bool IsDirty
    {
        get => NodeInternal.IsDirty;
        set => NodeInternal.IsDirty = value;
    }

    /// <summary>
    /// Gets or sets whether this node is set as the reference baseline.
    /// </summary>
    public bool IsReferenceBaseline
    {
        set => NodeInternal.IsReferenceBaseline = value;
        get => NodeInternal.IsReferenceBaseline;
    }

    /// <summary>
    /// Gets or sets whether a leaf node's layout results may be truncated during layout rounding.
    /// </summary>
    public NodeType NodeType
    {
        get => NodeInternal.NodeType.ToNfmNodeType();
        set => NodeInternal.NodeType = value.ToYogaNodeType();
    }

    /// <summary>
    /// Make it so that this node will always form a containing block for any
    /// descendant nodes. This is useful for when a node has a property outside of
    /// of Yoga that will form a containing block. For example, transforms or some of
    /// the others listed in
    /// https://developer.mozilla.org/en-US/docs/Web/CSS/Containing_block
    /// </summary>
    public bool AlwaysFormsContainingBlock
    {
        get => NodeInternal.AlwaysFormsContainingBlock;
        set => NodeInternal.AlwaysFormsContainingBlock = value;
    }

    #endregion

    #region Style

    protected override void UpdateStyles(StyleSheetStyles? oldStyleSheet, StyleSheetStyles? newStyleSheet)
    {
        base.UpdateStyles(oldStyleSheet, newStyleSheet);

        if (oldStyleSheet is { } oldStyleSheetValue)
        {
            if (oldStyleSheetValue.Visibility is not null) Visibility = Visibility.Visible;
            if (oldStyleSheetValue.Opacity is not null) Opacity = 1.0f;
            if (oldStyleSheetValue.Direction is not null) Direction = default;
            if (oldStyleSheetValue.FlexDirection is not null) FlexDirection = default;
            if (oldStyleSheetValue.JustifyContent is not null) JustifyContent = default;
            if (oldStyleSheetValue.AlignItems is not null) AlignItems = default;
            if (oldStyleSheetValue.AlignSelf is not null) AlignSelf = default;
            if (oldStyleSheetValue.AlignContent is not null) AlignContent = default;
            if (oldStyleSheetValue.Position is not null) Position = default;
            if (oldStyleSheetValue.FlexWrap is not null) FlexWrap = default;
            if (oldStyleSheetValue.Overflow is not null) Overflow = default;
            if (oldStyleSheetValue.Display is not null) Display = default;
            if (oldStyleSheetValue.Flex is not null) Flex = null;
            if (oldStyleSheetValue.FlexGrow is not null) FlexGrow = null;
            if (oldStyleSheetValue.FlexShrink is not null) FlexShrink = null;
            if (oldStyleSheetValue.FlexBasis is not null) FlexBasis = MeasurementFlexBasis.Undefined;
            if (oldStyleSheetValue.Left is not null) Left = MeasurementMarginPosition.Undefined;
            if (oldStyleSheetValue.Top is not null) Top = MeasurementMarginPosition.Undefined;
            if (oldStyleSheetValue.Right is not null) Right = MeasurementMarginPosition.Undefined;
            if (oldStyleSheetValue.Bottom is not null) Bottom = MeasurementMarginPosition.Undefined;
            if (oldStyleSheetValue.Margin is not null) Margin = MeasurementMarginPosition.Undefined;
            if (oldStyleSheetValue.MarginTop is not null) MarginTop = MeasurementMarginPosition.Undefined;
            if (oldStyleSheetValue.MarginBottom is not null) MarginBottom = MeasurementMarginPosition.Undefined;
            if (oldStyleSheetValue.MarginLeft is not null) MarginLeft = MeasurementMarginPosition.Undefined;
            if (oldStyleSheetValue.MarginRight is not null) MarginRight = MeasurementMarginPosition.Undefined;
            if (oldStyleSheetValue.Padding is not null) Padding = MeasurementPadding.Undefined;
            if (oldStyleSheetValue.PaddingTop is not null) PaddingTop = MeasurementPadding.Undefined;
            if (oldStyleSheetValue.PaddingBottom is not null) PaddingBottom = MeasurementPadding.Undefined;
            if (oldStyleSheetValue.PaddingLeft is not null) PaddingLeft = MeasurementPadding.Undefined;
            if (oldStyleSheetValue.PaddingRight is not null) PaddingRight = MeasurementPadding.Undefined;
            if (oldStyleSheetValue.Border is not null) Border = MeasurementMultiBorder.Undefined;
            if (oldStyleSheetValue.BorderTop is not null) BorderTop = null;
            if (oldStyleSheetValue.BorderBottom is not null) BorderBottom = null;
            if (oldStyleSheetValue.BorderLeft is not null) BorderLeft = null;
            if (oldStyleSheetValue.BorderRight is not null) BorderRight = null;
            if (oldStyleSheetValue.Gap is not null) Gap = MeasurementGap.Undefined;
            if (oldStyleSheetValue.GapColumn is not null) GapColumn = MeasurementGap.Undefined;
            if (oldStyleSheetValue.GapRow is not null) GapRow = MeasurementGap.Undefined;
            if (oldStyleSheetValue.BoxSizing is not null) BoxSizing = default;
            if (oldStyleSheetValue.Width is not null) Width = MeasurementWidthHeight.Undefined;
            if (oldStyleSheetValue.Height is not null) Height = MeasurementWidthHeight.Undefined;
            if (oldStyleSheetValue.MinWidth is not null) MinWidth = MeasurementWidthHeight.Undefined;
            if (oldStyleSheetValue.MinHeight is not null) MinHeight = MeasurementWidthHeight.Undefined;
            if (oldStyleSheetValue.MaxWidth is not null) MaxWidth = MeasurementWidthHeight.Undefined;
            if (oldStyleSheetValue.MaxHeight is not null) MaxHeight = MeasurementWidthHeight.Undefined;
            if (oldStyleSheetValue.AspectRatio is not null) AspectRatio = null;
        }
        
        if (newStyleSheet is { } newStyleSheetValue)
        {
            if (newStyleSheetValue.Visibility is { } visibility) Visibility = visibility;
            if (newStyleSheetValue.Opacity is { } opacity) Opacity = opacity;
            if (newStyleSheetValue.Direction is { } direction) Direction = direction;
            if (newStyleSheetValue.FlexDirection is { } flexDirection) FlexDirection = flexDirection;
            if (newStyleSheetValue.JustifyContent is { } justifyContent) JustifyContent = justifyContent;
            if (newStyleSheetValue.AlignItems is { } alignItems) AlignItems = alignItems;
            if (newStyleSheetValue.AlignSelf is { } alignSelf) AlignSelf = alignSelf;
            if (newStyleSheetValue.AlignContent is { } alignContent) AlignContent = alignContent;
            if (newStyleSheetValue.Position is { } position) Position = position;
            if (newStyleSheetValue.FlexWrap is { } flexWrap) FlexWrap = flexWrap;
            if (newStyleSheetValue.Overflow is { } overflow) Overflow = overflow;
            if (newStyleSheetValue.Display is { } display) Display = display;
            if (newStyleSheetValue.Flex is { } flex) Flex = flex;
            if (newStyleSheetValue.FlexGrow is { } flexGrow) FlexGrow = flexGrow;
            if (newStyleSheetValue.FlexShrink is { } flexShrink) FlexShrink = flexShrink;
            if (newStyleSheetValue.FlexBasis is { } flexBasis) FlexBasis = flexBasis;
            if (newStyleSheetValue.Left is { } left) Left = left;
            if (newStyleSheetValue.Top is { } top) Top = top;
            if (newStyleSheetValue.Right is { } right) Right = right;
            if (newStyleSheetValue.Bottom is { } bottom) Bottom = bottom;
            if (newStyleSheetValue.Margin is { } margin) Margin = margin;
            if (newStyleSheetValue.MarginTop is { } marginTop) MarginTop = marginTop;
            if (newStyleSheetValue.MarginBottom is { } marginBottom) MarginBottom = marginBottom;
            if (newStyleSheetValue.MarginLeft is { } marginLeft) MarginLeft = marginLeft;
            if (newStyleSheetValue.MarginRight is { } marginRight) MarginRight = marginRight;
            if (newStyleSheetValue.Padding is { } padding) Padding = padding;
            if (newStyleSheetValue.PaddingTop is { } paddingTop) PaddingTop = paddingTop;
            if (newStyleSheetValue.PaddingBottom is { } paddingBottom) PaddingBottom = paddingBottom;
            if (newStyleSheetValue.PaddingLeft is { } paddingLeft) PaddingLeft = paddingLeft;
            if (newStyleSheetValue.PaddingRight is { } paddingRight) PaddingRight = paddingRight;
            if (newStyleSheetValue.Border is { } border) Border = border;
            if (newStyleSheetValue.BorderTop is { } borderTop) BorderTop = borderTop;
            if (newStyleSheetValue.BorderBottom is { } borderBottom) BorderBottom = borderBottom;
            if (newStyleSheetValue.BorderLeft is { } borderLeft) BorderLeft = borderLeft;
            if (newStyleSheetValue.BorderRight is { } borderRight) BorderRight = borderRight;
            if (newStyleSheetValue.Gap is { } gap) Gap = gap;
            if (newStyleSheetValue.GapColumn is { } gapColumn) GapColumn = gapColumn;
            if (newStyleSheetValue.GapRow is { } gapRow) GapRow = gapRow;
            if (newStyleSheetValue.BoxSizing is { } boxSizing) BoxSizing = boxSizing;
            if (newStyleSheetValue.Width is { } width) Width = width;
            if (newStyleSheetValue.Height is { } height) Height = height;
            if (newStyleSheetValue.MinWidth is { } minWidth) MinWidth = minWidth;
            if (newStyleSheetValue.MinHeight is { } minHeight) MinHeight = minHeight;
            if (newStyleSheetValue.MaxWidth is { } maxWidth) MaxWidth = maxWidth;
            if (newStyleSheetValue.MaxHeight is { } maxHeight) MaxHeight = maxHeight;
            if (newStyleSheetValue.AspectRatio is { } aspectRatio) AspectRatio = aspectRatio;
        }
    }

    /// <summary>
    /// CSS: visibility - Controls whether the element is visible (visible/hidden/collapsed)
    /// </summary>
    [Property]
    public Visibility Visibility
    {
        get;
        set
        {
            var oldValue = field;
            field = value;
            if (value is Visibility.Visible && oldValue is Visibility.Hidden && Opacity > 0.0f)
                Shown?.Invoke();
            else if (value is Visibility.Hidden && oldValue is Visibility.Visible && Opacity > 0.0f)
                Hidden?.Invoke();
        }
    } = Visibility.Visible;

    /// <summary>
    /// CSS: opacity - Sets the transparency level (0.0 = fully transparent, 1.0 = fully opaque)
    /// </summary>
    [Property]
    public float Opacity
    {
        get;
        set
        {
            var oldValue = field;
            field = value;
            if (value <= 0.0f && oldValue > 0.0f && Visibility is Visibility.Visible)
                Hidden?.Invoke();
            else if (value > 0.0f && oldValue <= 0.0f && Visibility is Visibility.Visible)
                Shown?.Invoke();
        }
    } = 1.0f;

    // https://css-tricks.com/snippets/css/a-guide-to-flexbox/
    /// <summary>
    /// CSS: direction - Establishes the main-axis (ltr/rtl/inherit)
    /// </summary>
    [Property]
    public Direction Direction
    {
        get => NodeInternal.Direction.ToNfmDirection();
        set => NodeInternal.Direction = value.ToYogaDirection();
    }

    /// <summary>
    /// CSS: flex-direction - Establishes the main-axis (row/column/row-reverse/column-reverse)
    /// </summary>
    [Property]
    public FlexDirection FlexDirection
    {
        get => NodeInternal.FlexDirection.ToNfmFlexDirection();
        set => NodeInternal.FlexDirection = value.ToYogaFlexDirection();
    }

    /// <summary>
    /// CSS: justify-content - Defines alignment along the main axis
    /// </summary>
    [Property]
    public Justify JustifyContent
    {
        get => NodeInternal.JustifyContent.ToNfmJustify();
        set => NodeInternal.JustifyContent = value.ToYogaJustify();
    }

    /// <summary>
    /// CSS: align-items - Defines default alignment for all children along the cross axis
    /// </summary>
    [Property]
    public Align AlignItems
    {
        get => NodeInternal.AlignItems.ToNfmAlign();
        set => NodeInternal.AlignItems = value.ToYogaAlign();
    }

    /// <summary>
    /// CSS: align-self - Allows a child to override the default cross-axis alignment
    /// </summary>
    [Property]
    public Align AlignSelf
    {
        get => NodeInternal.AlignSelf.ToNfmAlign();
        set => NodeInternal.AlignSelf = value.ToYogaAlign();
    }

    /// <summary>
    /// CSS: align-content - Aligns flex container's lines when there is extra space in the cross-axis
    /// </summary>
    [Property]
    public Align AlignContent
    {
        get => NodeInternal.AlignContent.ToNfmAlign();
        set => NodeInternal.AlignContent = value.ToYogaAlign();
    }

    /// <summary>
    /// CSS: position - Sets how an element is positioned (static/relative/absolute/fixed)
    /// </summary>
    [Property]
    public Position Position
    {
        get => NodeInternal.PositionType.ToNfmPositionType();
        set => NodeInternal.PositionType = value.ToYogaPositionType();
    }

    /// <summary>
    /// CSS: flex-wrap - Controls whether flex items wrap onto multiple lines (nowrap/wrap/wrap-reverse)
    /// </summary>
    [Property]
    public Wrap FlexWrap
    {
        get => NodeInternal.FlexWrap.ToNfmWrap();
        set => NodeInternal.FlexWrap = value.ToYogaWrap();
    }

    /// <summary>
    /// CSS: overflow - Controls what happens to content that is too big to fit (visible/hidden/scroll)
    /// </summary>
    [Property]
    public Overflow Overflow
    {
        get => NodeInternal.Overflow.ToNfmOverflow();
        set => NodeInternal.Overflow = value.ToYogaOverflow();
    }

    /// <summary>
    /// CSS: display - Defines the display type of the element (flex/none/block)
    /// </summary>
    [Property]
    public Display Display
    {
        get => NodeInternal.Display.ToNfmDisplay();
        set => NodeInternal.Display = value.ToYogaDisplay();
    }

    /// <summary>
    /// CSS: flex - Shorthand for flex-grow, flex-shrink, and flex-basis combined
    /// </summary>
    [Property]
    public float? Flex
    {
        get => NodeInternal.Flex is var v && !float.IsNaN(v) ? v : null;
        set => NodeInternal.Flex = value ?? float.NaN;
    }

    /// <summary>
    /// CSS: flex-grow - Defines the ability for a flex item to grow if necessary
    /// </summary>
    [Property]
    public float? FlexGrow
    {
        get => NodeInternal.FlexGrow is var v && !float.IsNaN(v) ? v : null;
        set => NodeInternal.FlexGrow = value ?? float.NaN;
    }

    /// <summary>
    /// CSS: flex-shrink - Defines the ability for a flex item to shrink if necessary
    /// </summary>
    [Property]
    public float? FlexShrink
    {
        get => NodeInternal.FlexShrink is var v && !float.IsNaN(v) ? v : null;
        set => NodeInternal.FlexShrink = value ?? float.NaN;
    }

    public struct MeasurementFlexBasis
    {
        internal YGValue InternalValue;
        public YGUnit Unit => InternalValue.unit;
        public float Value => InternalValue.value;
        public float? PointValue => InternalValue.unit == YGUnit.YGUnitPoint ? InternalValue.value : null;
        public float? PercentValue => InternalValue.unit == YGUnit.YGUnitPercent ? InternalValue.value : null;

        public static implicit operator MeasurementFlexBasis(float value)
        {
            return new MeasurementFlexBasis
            {
                InternalValue = new YGValue
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

        public static MeasurementFlexBasis Undefined = new()
        {
            InternalValue = new YGValue
            {
                unit = YGUnit.YGUnitUndefined
            }
        };

        public static MeasurementFlexBasis Auto =>
            new()
            {
                InternalValue = new YGValue
                {
                    unit = YGUnit.YGUnitFitContent
                }
            };

        public static MeasurementFlexBasis MaxContent =>
            new()
            {
                InternalValue = new YGValue
                {
                    unit = YGUnit.YGUnitMaxContent
                }
            };

        public static MeasurementFlexBasis Stretch =>
            new()
            {
                InternalValue = new YGValue
                {
                    unit = YGUnit.YGUnitStretch
                }
            };

        public static MeasurementFlexBasis Percent(float value)
        {
            return new MeasurementFlexBasis
            {
                InternalValue = new YGValue
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
                InternalValue = new YGValue
                {
                    unit = YGUnit.YGUnitPoint,
                    value = value
                }
            };
        }

        public static MeasurementFlexBasis FitContent =>
            new()
            {
                InternalValue = new YGValue
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

        public static implicit operator MeasurementFlexBasis(ReadOnlySpan<char> str)
        {
            var trimmed = str.Trim();
            if (trimmed.Equals("undefined", StringComparison.OrdinalIgnoreCase))
            {
                return Undefined;
            }
            if (trimmed.Equals("auto", StringComparison.OrdinalIgnoreCase))
            {
                return Auto;
            }
            if (trimmed.Equals("max-content", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("maxcontent", StringComparison.OrdinalIgnoreCase))
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
    }

    /// <summary>
    /// CSS: flex-basis - Defines the default size of an element before remaining space is distributed
    /// </summary>
    [Property]
    public MeasurementFlexBasis FlexBasis
    {
        get;
        set
        {
            field = value;
            NodeInternal.FlexBasis = value.Scale(G.Scale);
        }
    } = MeasurementFlexBasis.Undefined;

    public struct MeasurementMarginPosition
    {
        internal YGValue InternalValue;
        public YGUnit Unit => InternalValue.unit;
        public float Value => InternalValue.value;
        public float? PointValue => InternalValue.unit == YGUnit.YGUnitPoint ? InternalValue.value : null;
        public float? PercentValue => InternalValue.unit == YGUnit.YGUnitPercent ? InternalValue.value : null;

        public static implicit operator MeasurementMarginPosition(float value)
        {
            return new MeasurementMarginPosition
            {
                InternalValue = new YGValue
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
                InternalValue = new YGValue
                {
                    unit = YGUnit.YGUnitAuto
                }
            };

        public static MeasurementMarginPosition Undefined => new()
        {
            InternalValue = new YGValue
            {
                unit = YGUnit.YGUnitUndefined
            }
        };

        public static MeasurementMarginPosition Percent(float value)
        {
            return new MeasurementMarginPosition
            {
                InternalValue = new YGValue
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
                InternalValue = new YGValue
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
        
        public static implicit operator MeasurementMarginPosition(ReadOnlySpan<char> str)
        {
            var trimmed = str.Trim();
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
    }

    /// <summary>
    /// CSS: left - Specifies the left position of a positioned element
    /// </summary>
    [Property]
    public MeasurementMarginPosition Left
    {
        get;
        set
        {
            field = value;
            NodeInternal.Left = value.Scale(G.Scale);
        }
    } = MeasurementMarginPosition.Undefined;

    /// <summary>
    /// CSS: top - Specifies the top position of a positioned element
    /// </summary>
    [Property]
    public MeasurementMarginPosition Top
    {
        get;
        set
        {
            field = value;
            NodeInternal.Top = value.Scale(G.Scale);
        }
    } = MeasurementMarginPosition.Undefined;

    /// <summary>
    /// CSS: right - Specifies the right position of a positioned element
    /// </summary>
    [Property]
    public MeasurementMarginPosition Right
    {
        get;
        set
        {
            field = value;
            NodeInternal.Right = value.Scale(G.Scale);
        }
    } = MeasurementMarginPosition.Undefined;

    /// <summary>
    /// CSS: bottom - Specifies the bottom position of a positioned element
    /// </summary>
    [Property]
    public MeasurementMarginPosition Bottom
    {
        get;
        set
        {
            field = value;
            NodeInternal.Bottom = value.Scale(G.Scale);
        }
    } = MeasurementMarginPosition.Undefined;

    public struct MeasurementMultiMargin
    {
        public InlineArray4<MeasurementMarginPosition> Sides;
        public MeasurementMarginPosition Top
        {
            get => Sides[0];
            set => Sides[0] = value;
        }
        public MeasurementMarginPosition Bottom
        {
            get => Sides[1];
            set => Sides[1] = value;
        }
        public MeasurementMarginPosition Left
        {
            get => Sides[2];
            set => Sides[2] = value;
        }
        public MeasurementMarginPosition Right
        {
            get => Sides[3];
            set => Sides[3] = value;
        }

        public static MeasurementMultiMargin Auto => MeasurementMarginPosition.Auto;

        public static MeasurementMultiMargin Undefined => MeasurementMarginPosition.Undefined;

        public static MeasurementMultiMargin All(MeasurementMarginPosition value)
        {
            return new MeasurementMultiMargin
            {
                Top = value,
                Bottom = value,
                Left = value,
                Right = value
            };
        }

        public static implicit operator MeasurementMultiMargin(MeasurementMarginPosition value) => All(value);
        public static implicit operator MeasurementMultiMargin(float value) => All(value);

        public static MeasurementMultiMargin? XY(float x, float y)
        {
            return new MeasurementMultiMargin
            {
                Left = x,
                Right = x,
                Top = y,
                Bottom = y
            };
        }

        public static implicit operator MeasurementMultiMargin(ReadOnlySpan<char> str)
        {
            var trimmed = str.Trim();
            if (trimmed.Equals("undefined", StringComparison.OrdinalIgnoreCase))
            {
                return All(MeasurementMarginPosition.Undefined);
            }
            if (trimmed.Equals("auto", StringComparison.OrdinalIgnoreCase))
            {
                return All(MeasurementMarginPosition.Auto);
            }

            var idx = 0;
            var sides = new InlineArray4<MeasurementMarginPosition>();
            foreach (var elementRange in trimmed.SplitAny(',', ' '))
            {
                var element = trimmed[elementRange];

                if (element.EndsWith("%", StringComparison.OrdinalIgnoreCase))
                {
                    if (float.TryParse(trimmed[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var percentValue))
                    {
                        sides[idx] = MeasurementMarginPosition.Percent(percentValue);
                    }
                }
                else if (element.EndsWith("px"))
                {
                    if (float.TryParse(trimmed[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out var pointValue))
                    {
                        sides[idx] = MeasurementMarginPosition.Point(pointValue);
                    }
                }
                else
                {
                    if (float.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var pointValue))
                    {
                        sides[idx] = MeasurementMarginPosition.Point(pointValue);
                    }
                }

                idx++;
            }

            if (idx == 1)
            {
                return All(sides[0]);
            }

            if (idx == 2)
            {
                return new MeasurementMultiMargin
                {
                    Top = sides[0],
                    Bottom = sides[0],
                    Left = sides[1],
                    Right = sides[1]
                };
            }

            if (idx == 4)
            {
                return new MeasurementMultiMargin
                {
                    Top = sides[0],
                    Right = sides[1],
                    Bottom = sides[2],
                    Left = sides[3]
                };
            }

            throw new FormatException($"Cannot convert '{str}' to MeasurementMultiMargin. Expected 'auto', '<number>px', '<number>%', or '<number>', as 1, 2 or 4 elements, in order top-right-bottom-left, separated by comma or space.");
        }
    }

    /// <summary>
    /// CSS: margin - Shorthand for setting all margin values (top, right, bottom, left)
    /// </summary>
    [Property]
    public MeasurementMultiMargin Margin
    {
        get => new()
        {
            Top = MarginTop,
            Bottom = MarginBottom,
            Left = MarginLeft,
            Right = MarginRight
        };
        set
        {
            MarginLeft = value.Left;
            MarginRight = value.Right;
            MarginTop = value.Top;
            MarginBottom = value.Bottom;
        }
    }

    /// <summary>
    /// CSS: margin-top - Sets the top margin space outside the element
    /// </summary>
    [Property]
    public MeasurementMarginPosition MarginTop
    {
        get;
        set
        {
            field = value;
            NodeInternal.MarginTop = value.Scale(G.Scale);
        }
    } = MeasurementMarginPosition.Undefined;

    /// <summary>
    /// CSS: margin-bottom - Sets the bottom margin space outside the element
    /// </summary>
    [Property]
    public MeasurementMarginPosition MarginBottom
    {
        get;
        set
        {
            field = value;
            NodeInternal.MarginBottom = value.Scale(G.Scale);
        }
    } = MeasurementMarginPosition.Undefined;

    /// <summary>
    /// CSS: margin-left - Sets the left margin space outside the element
    /// </summary>
    [Property]
    public MeasurementMarginPosition MarginLeft
    {
        get;
        set
        {
            field = value;
            NodeInternal.MarginLeft = value.Scale(G.Scale);
        }
    } = MeasurementMarginPosition.Undefined;

    /// <summary>
    /// CSS: margin-right - Sets the right margin space outside the element
    /// </summary>
    [Property]
    public MeasurementMarginPosition MarginRight
    {
        get;
        set
        {
            field = value;
            NodeInternal.MarginRight = value.Scale(G.Scale);
        }
    } = MeasurementMarginPosition.Undefined;

    public struct MeasurementPadding
    {
        internal YGValue InternalValue;
        public YGUnit Unit => InternalValue.unit;
        public float Value => InternalValue.value;
        public float? PointValue => InternalValue.unit == YGUnit.YGUnitPoint ? InternalValue.value : null;
        public float? PercentValue => InternalValue.unit == YGUnit.YGUnitPercent ? InternalValue.value : null;

        public static implicit operator MeasurementPadding(float value)
        {
            return new MeasurementPadding
            {
                InternalValue = new YGValue
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

        public static MeasurementPadding Undefined => new()
        {
            InternalValue = new YGValue
            {
                unit = YGUnit.YGUnitUndefined
            }
        };

        public static MeasurementPadding Percent(float value)
        {
            return new MeasurementPadding
            {
                InternalValue = new YGValue
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
                InternalValue = new YGValue
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

        public static implicit operator MeasurementPadding(ReadOnlySpan<char> str)
        {
            var trimmed = str.Trim();
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
    }

    public struct MeasurementMultiPadding
    {
        public InlineArray4<MeasurementPadding> Sides;
        public MeasurementPadding Top
        {
            get => Sides[0];
            set => Sides[0] = value;
        }
        public MeasurementPadding Bottom
        {
            get => Sides[1];
            set => Sides[1] = value;
        }
        public MeasurementPadding Left
        {
            get => Sides[2];
            set => Sides[2] = value;
        }
        public MeasurementPadding Right
        {
            get => Sides[3];
            set => Sides[3] = value;
        }

        public static MeasurementMultiPadding Undefined => MeasurementPadding.Undefined;

        public static MeasurementMultiPadding All(MeasurementPadding value)
        {
            return new MeasurementMultiPadding
            {
                Top = value,
                Bottom = value,
                Left = value,
                Right = value
            };
        }

        public static implicit operator MeasurementMultiPadding(MeasurementPadding value) => All(value);
        public static implicit operator MeasurementMultiPadding(float value) => All(value);

        public static MeasurementMultiPadding? XY(float x, float y)
        {
            return new MeasurementMultiPadding
            {
                Left = MeasurementPadding.Point(x),
                Right = MeasurementPadding.Point(x),
                Top = MeasurementPadding.Point(y),
                Bottom = MeasurementPadding.Point(y)
            };
        }

        public static implicit operator MeasurementMultiPadding(ReadOnlySpan<char> str)
        {
            var trimmed = str.Trim();
            if (trimmed.Equals("undefined", StringComparison.OrdinalIgnoreCase))
            {
                return All(MeasurementPadding.Undefined);
            }

            var idx = 0;
            var sides = new InlineArray4<MeasurementPadding>();
            foreach (var elementRange in trimmed.SplitAny(',', ' '))
            {
                var element = trimmed[elementRange];

                if (element.EndsWith("%", StringComparison.OrdinalIgnoreCase))
                {
                    if (float.TryParse(trimmed[..^1], NumberStyles.Float, CultureInfo.InvariantCulture, out var percentValue))
                    {
                        sides[idx] = MeasurementPadding.Percent(percentValue);
                    }
                }
                else if (element.EndsWith("px"))
                {
                    if (float.TryParse(trimmed[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out var pointValue))
                    {
                        sides[idx] = MeasurementPadding.Point(pointValue);
                    }
                }
                else
                {
                    if (float.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var pointValue))
                    {
                        sides[idx] = MeasurementPadding.Point(pointValue);
                    }
                }

                idx++;
            }

            if (idx == 1)
            {
                return All(sides[0]);
            }

            if (idx == 2)
            {
                return new MeasurementMultiPadding
                {
                    Top = sides[0],
                    Bottom = sides[0],
                    Left = sides[1],
                    Right = sides[1]
                };
            }

            if (idx == 4)
            {
                return new MeasurementMultiPadding
                {
                    Top = sides[0],
                    Right = sides[1],
                    Bottom = sides[2],
                    Left = sides[3]
                };
            }

            throw new FormatException($"Cannot convert '{str}' to MeasurementMultiMargin. Expected '<number>px', '<number>%', or '<number>', as 1, 2 or 4 elements, in order top-right-bottom-left, separated by comma or space.");

        }
    }

    /// <summary>
    /// CSS: padding - Shorthand for setting all padding values (top, right, bottom, left)
    /// </summary>
    [Property]
    public MeasurementMultiPadding Padding
    {
        get => new()
        {
            Left = PaddingLeft,
            Right = PaddingRight,
            Top = PaddingTop,
            Bottom = PaddingBottom
        };
        set
        {
            PaddingLeft = value.Left;
            PaddingRight = value.Right;
            PaddingTop = value.Top;
            PaddingBottom = value.Bottom;
        }
    }

    /// <summary>
    /// CSS: padding-top - Sets the top padding space inside the element
    /// </summary>
    [Property]
    public MeasurementPadding PaddingTop
    {
        get;
        set
        {
            field = value;
            NodeInternal.PaddingTop = value.Scale(G.Scale);
        }
    } = MeasurementPadding.Undefined;

    /// <summary>
    /// CSS: padding-bottom - Sets the bottom padding space inside the element
    /// </summary>
    [Property]
    public MeasurementPadding PaddingBottom
    {
        get;
        set
        {
            field = value;
            NodeInternal.PaddingBottom = value.Scale(G.Scale);
        }
    } = MeasurementPadding.Undefined;

    /// <summary>
    /// CSS: padding-left - Sets the left padding space inside the element
    /// </summary>
    [Property]
    public MeasurementPadding PaddingLeft
    {
        get;
        set
        {
            field = value;
            NodeInternal.PaddingLeft = value.Scale(G.Scale);
        }
    } = MeasurementPadding.Undefined;

    /// <summary>
    /// CSS: padding-right - Sets the right padding space inside the element
    /// </summary>
    [Property]
    public MeasurementPadding PaddingRight
    {
        get;
        set
        {
            field = value;
            NodeInternal.PaddingRight = value.Scale(G.Scale);
        }
    } = MeasurementPadding.Undefined;

    public struct MeasurementMultiBorder
    {
        public InlineArray4<float?> Sides;
        public float? Top
        {
            get => Sides[0];
            set => Sides[0] = value;
        }
        public float? Bottom
        {
            get => Sides[1];
            set => Sides[1] = value;
        }
        public float? Left
        {
            get => Sides[2];
            set => Sides[2] = value;
        }
        public float? Right
        {
            get => Sides[3];
            set => Sides[3] = value;
        }

        public static MeasurementMultiBorder Undefined => All(null);

        public static MeasurementMultiBorder All(float? value)
        {
            return new MeasurementMultiBorder
            {
                Top = value,
                Bottom = value,
                Left = value,
                Right = value
            };
        }

        public static implicit operator MeasurementMultiBorder(float? value) => All(value);
        public static MeasurementMultiBorder XY(float? x, float? y)
        {
            return new MeasurementMultiBorder
            {
                Left = x,
                Right = x,
                Top = y,
                Bottom = y
            };
        }

        public static implicit operator MeasurementMultiBorder(ReadOnlySpan<char> str)
        {
            var trimmed = str.Trim();
            if (trimmed.Equals("undefined", StringComparison.OrdinalIgnoreCase))
            {
                return Undefined;
            }

            var idx = 0;
            var sides = new InlineArray4<float>();
            foreach (var elementRange in trimmed.SplitAny(',', ' '))
            {
                var element = trimmed[elementRange];

                if (element.EndsWith("px"))
                {
                    if (float.TryParse(trimmed[..^2], NumberStyles.Float, CultureInfo.InvariantCulture, out var pointValue))
                    {
                        sides[idx] = pointValue;
                    }
                }
                else
                {
                    if (float.TryParse(trimmed, NumberStyles.Float, CultureInfo.InvariantCulture, out var pointValue))
                    {
                        sides[idx] = pointValue;
                    }
                }

                idx++;
            }

            if (idx == 1)
            {
                return All(sides[0]);
            }

            if (idx == 2)
            {
                return new MeasurementMultiBorder
                {
                    Top = sides[0],
                    Bottom = sides[0],
                    Left = sides[1],
                    Right = sides[1]
                };
            }

            if (idx == 4)
            {
                return new MeasurementMultiBorder
                {
                    Top = sides[0],
                    Right = sides[1],
                    Bottom = sides[2],
                    Left = sides[3]
                };
            }

            throw new FormatException($"Cannot convert '{str}' to MeasurementMultiMargin. Expected '<number>px' or '<number>', as 1, 2 or 4 elements, in order top-right-bottom-left, separated by comma or space.");
        }
    }

    /// <summary>
    /// CSS: border - Shorthand for setting all border widths
    /// </summary>
    [Property]
    public MeasurementMultiBorder Border
    {
        get => new()
        {
            Left = BorderLeft,
            Right = BorderRight,
            Top = BorderTop,
            Bottom = BorderBottom
        };
        set
        {
            BorderLeft = value.Left;
            BorderRight = value.Right;
            BorderTop = value.Top;
            BorderBottom = value.Bottom;
        }
    }

    /// <summary>
    /// CSS: border-top-width - Sets the width of the top border
    /// </summary>
    [Property]
    public Pixels? BorderTop
    {
        get;
        set
        {
            field = value;
            NodeInternal.BorderTop = (value?.Value * G.Scale) ?? YG.YGUndefined;
        }
    }

    /// <summary>
    /// CSS: border-bottom-width - Sets the width of the bottom border
    /// </summary>
    [Property]
    public Pixels? BorderBottom
    {
        get;
        set
        {
            field = value;
            NodeInternal.BorderBottom = (value?.Value * G.Scale) ?? YG.YGUndefined;
        }
    }

    /// <summary>
    /// CSS: border-left-width - Sets the width of the left border
    /// </summary>
    [Property]
    public Pixels? BorderLeft
    {
        get;
        set
        {
            field = value;
            NodeInternal.BorderLeft = (value?.Value * G.Scale) ?? YG.YGUndefined;
        }
    }

    /// <summary>
    /// CSS: border-right-width - Sets the width of the right border
    /// </summary>
    [Property]
    public Pixels? BorderRight
    {
        get;
        set
        {
            field = value;
            NodeInternal.BorderRight = (value?.Value * G.Scale) ?? YG.YGUndefined;
        }
    }

    public struct MeasurementGap
    {
        internal YGValue InternalValue;
        public YGUnit Unit => InternalValue.unit;
        public float Value => InternalValue.value;
        public float? PointValue => InternalValue.unit == YGUnit.YGUnitPoint ? InternalValue.value : null;
        public float? PercentValue => InternalValue.unit == YGUnit.YGUnitPercent ? InternalValue.value : null;

        public static implicit operator MeasurementGap(float value)
        {
            return new MeasurementGap
            {
                InternalValue = new YGValue
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

        public static MeasurementGap Undefined => new()
        {
            InternalValue = new YGValue
            {
                unit = YGUnit.YGUnitUndefined
            }
        };

        public static MeasurementGap Percent(float value)
        {
            return new MeasurementGap
            {
                InternalValue = new YGValue
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
                InternalValue = new YGValue
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

        public static implicit operator MeasurementGap(ReadOnlySpan<char> str)
        {
            var trimmed = str.Trim();
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
        
        // ReSharper disable once CompareOfFloatsByEqualityOperator
        public static bool operator ==(MeasurementGap left, MeasurementGap right) => left.Unit == right.Unit && left.Value == right.Value;
        public static bool operator !=(MeasurementGap left, MeasurementGap right) => !(left == right);
    }

    /// <summary>
    /// CSS: gap - Shorthand for setting row-gap and column-gap
    /// </summary>
    [Property]
    public MeasurementGap Gap
    {
        get => GapColumn == GapRow ? GapColumn : MeasurementGap.Undefined;
        set
        {
            GapColumn = value;
            GapRow = value;
        }
    }

    /// <summary>
    /// CSS: column-gap - Sets the gap between columns in a flex container
    /// </summary>
    [Property]
    public MeasurementGap GapColumn
    {
        get;
        set
        {
            field = value;
            NodeInternal.GapColumn = value;
        }
    } = MeasurementGap.Undefined;

    /// <summary>
    /// CSS: row-gap - Sets the gap between rows in a flex container
    /// </summary>
    [Property]
    public MeasurementGap GapRow
    {
        get;
        set
        {
            field = value;
            NodeInternal.GapRow = value;
        }
    } = MeasurementGap.Undefined;

    /// <summary>
    /// CSS: box-sizing - Defines how width/height calculations include padding/border (content-box/border-box)
    /// </summary>
    [Property]
    public BoxSizing BoxSizing
    {
        get => NodeInternal.BoxSizing.ToNfmBoxSizing();
        set => NodeInternal.BoxSizing = value.ToYogaBoxSizing();
    }

    public struct MeasurementWidthHeight
    {
        internal YGValue InternalValue;
        public YGUnit Unit => InternalValue.unit;
        public float Value => InternalValue.value;
        public float? PointValue => InternalValue.unit == YGUnit.YGUnitPoint ? InternalValue.value : null;
        public float? PercentValue => InternalValue.unit == YGUnit.YGUnitPercent ? InternalValue.value : null;

        public static implicit operator MeasurementWidthHeight(float value)
        {
            return new MeasurementWidthHeight
            {
                InternalValue = new YGValue
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

        public static MeasurementWidthHeight Undefined => new()
        {
            InternalValue = new YGValue
            {
                unit = YGUnit.YGUnitUndefined
            }
        };

        public static MeasurementWidthHeight Auto()
        {
            return new MeasurementWidthHeight
            {
                InternalValue = new YGValue
                {
                    unit = YGUnit.YGUnitAuto
                }
            };
        }
        public static MeasurementWidthHeight Percent(float value)
        {
            return new MeasurementWidthHeight
            {
                InternalValue = new YGValue
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
                InternalValue = new YGValue
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
                InternalValue = new YGValue
                {
                    unit = YGUnit.YGUnitFitContent
                }
            };
        }
        public static MeasurementWidthHeight MaxContent()
        {
            return new MeasurementWidthHeight
            {
                InternalValue = new YGValue
                {
                    unit = YGUnit.YGUnitMaxContent
                }
            };
        }

        public static MeasurementWidthHeight Stretch()
        {
            return new MeasurementWidthHeight
            {
                InternalValue = new YGValue
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

        public static implicit operator MeasurementWidthHeight(ReadOnlySpan<char> str)
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
            if (trimmed.Equals("fit-content", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("fitcontent", StringComparison.OrdinalIgnoreCase))
            {
                return FitContent();
            }
            if (trimmed.Equals("max-content", StringComparison.OrdinalIgnoreCase) ||
                trimmed.Equals("maxcontent", StringComparison.OrdinalIgnoreCase))
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
    }

    /// <summary>
    /// CSS: width - Sets the width of the element
    /// </summary>
    [Property]
    public MeasurementWidthHeight Width
    {
        get;
        set
        {
            field = value;
            NodeInternal.Width = value.Scale(G.Scale);
        }
    } = MeasurementWidthHeight.Undefined;

    /// <summary>
    /// CSS: height - Sets the height of the element
    /// </summary>
    [Property]
    public MeasurementWidthHeight Height
    {
        get;
        set
        {
            field = value;
            NodeInternal.Height = value.Scale(G.Scale);
        }
    } = MeasurementWidthHeight.Undefined;

    /// <summary>
    /// CSS: min-width - Sets the minimum width of the element
    /// </summary>
    [Property]
    public MeasurementWidthHeight MinWidth
    {
        get;
        set
        {
            field = value;
            NodeInternal.MinWidth = value.Scale(G.Scale);
        }
    } = MeasurementWidthHeight.Undefined;

    /// <summary>
    /// CSS: min-height - Sets the minimum height of the element
    /// </summary>
    [Property]
    public MeasurementWidthHeight MinHeight
    {
        get;
        set
        {
            field = value;
            NodeInternal.MinHeight = value.Scale(G.Scale);
        }
    } = MeasurementWidthHeight.Undefined;

    /// <summary>
    /// CSS: max-width - Sets the maximum width of the element
    /// </summary>
    [Property]
    public MeasurementWidthHeight MaxWidth
    {
        get;
        set
        {
            field = value;
            NodeInternal.MaxWidth = value.Scale(G.Scale);
        }
    } = MeasurementWidthHeight.Undefined;

    /// <summary>
    /// CSS: max-height - Sets the maximum height of the element
    /// </summary>
    [Property]
    public MeasurementWidthHeight MaxHeight
    {
        get;
        set
        {
            field = value;
            NodeInternal.MaxHeight = value.Scale(G.Scale);
        }
    } = MeasurementWidthHeight.Undefined;

    /// <summary>
    /// CSS: aspect-ratio - Sets the preferred aspect ratio for the element (width / height)
    /// </summary>
    [Property]
    public Pixels? AspectRatio
    {
        get => NodeInternal.AspectRatio is var v && !float.IsNaN(v) ? v : null;
        set => NodeInternal.AspectRatio = value?.Value ?? float.NaN;
    }

    public readonly struct Pixels(float value)
    {
        public readonly float Value = value;
        
        public static implicit operator float(Pixels value) => value.Value;
        public static implicit operator Pixels(float value) => new(value);

        public static implicit operator Pixels(ReadOnlySpan<char> str)
        {
            var trimmed = str.Trim();
            if (trimmed.Equals("undefined", StringComparison.OrdinalIgnoreCase))
            {
                return new Pixels(float.NaN);
            }
            if (trimmed.Equals("none", StringComparison.OrdinalIgnoreCase))
            {
                return 0;
            }
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

            throw new FormatException($"Cannot convert '{str}' to pixels or undefined.");
        }
    }

    #endregion

    #region Focus
    
    public override Vector2 FocusOrigin => LayoutPaddingPosition;
    public override Vector2 FocusSize => LayoutPaddingSize;
    
    [Property]
    public Action<NodeEventArgs<MouseEvent>>? MousePressed { get; set; }

    [Property]
    public Action<NodeEventArgs<MouseEvent>>? MouseReleased { get; set; }
    
    [Property]
    public Action<NodeEventArgs<MouseDragEvent>>? MouseDragged { get; set; }
    
    [Property]
    public Action<NodeEventArgs<MouseWheelEvent>>? MouseScrolled { get; set; }
    
    [Property]
    public Action<NodeEventArgs<MouseMoveEvent>>? MouseMoved { get; set; }
    
    [Property]
    public Action<NodeEventArgs<MouseMoveEvent>>? MouseEntered { get; set; }
    
    [Property]
    public Action<NodeEventArgs<MouseMoveEvent>>? MouseLeft { get; set; }
    
    [Property]
    public Action<NodeEventArgs<KeyboardTypingEvent>>? KeyTyped { get; set; }

    [Property]
    public Action<NodeEventArgs<KeyboardEvent>>? KeyPressed { get; set; }

    [Property]
    public Action<NodeEventArgs<KeyboardEvent>>? KeyReleased { get; set; }

    #endregion

    private float _lastScale = 1f;

    /// <summary>
    /// Do not use directly.
    /// </summary>
    /// <returns>true if scale changed</returns>
    internal bool Rescale()
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

    internal sealed override void NotifyUiScaleChanged()
    {
        if (Rescale())
        {
            OnScaleChanged();
            foreach (var child in GetChildSnapshot())
            {
                child.NotifyUiScaleChanged();
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

    public sealed override void Render(RenderContext context)
    {
        OnAnimationFrameBegan();
        _root = context.TopLeft;
        if (Display != Display.None && Visibility == Visibility.Visible && Opacity > 0f)
        {
            var ownOpacity = context.InheritedOpacity * Opacity;
            G.Alpha = ownOpacity;
            RenderBackground(LayoutPaddingPosition, LayoutPaddingSize);
            RenderBorder(LayoutBorderPosition, LayoutBorderSize);
            RenderContent(LayoutContentPosition, LayoutContentSize);
            foreach (var child in GetChildSnapshot())
            {
                child.Render(new RenderContext(_root + new Vector2(LayoutX, LayoutY), ownOpacity)); // todo should this be LayoutContentPosition
            }
            G.Alpha = 1f;
        }
    }

    private protected void OnAnimationFrameBegan()
    {
        AnimationFrameBegan?.Invoke();
    }

    protected virtual void GameTick()
    {
    }

    public void LayoutAndRender(Vector2 availableSize, Vector2? origin = null)
    {
#if DEBUG
        __INTERNAL_YogaRootsThisFrame.Add(this);
#endif

        NotifyUiScaleChanged();
        NodeInternal.CalculateLayout(availableSize, YGDirection.YGDirectionLTR);
        Render(new RenderContext(origin ?? Vector2.Zero));
    }

    public sealed override void Update(FocusManager focusManager)
    {
        GameTick();
        base.Update(focusManager);
    }

    protected virtual void OnMousePressed(FocusManager focusManager, MouseEvent @event)
    {
    }
    
    protected virtual void OnMouseReleased(FocusManager focusManager, MouseEvent @event)
    {
    }
    
    protected virtual void OnMouseDragged(FocusManager focusManager, MouseDragEvent @event)
    {
    }

    protected virtual void OnMouseScrolled(FocusManager focusManager, MouseWheelEvent @event)
    {
    }

    protected virtual void OnMouseMoved(FocusManager focusManager, MouseMoveEvent @event)
    {
    }

    protected virtual void OnMouseEntered(FocusManager focusManager, MouseMoveEvent @event)
    {
    }

    protected virtual void OnMouseLeft(FocusManager focusManager, MouseMoveEvent @event)
    {
    }

    protected virtual void OnKeyTyped(FocusManager focusManager, KeyboardTypingEvent @event)
    {
    }

    public override void DispatchMouseMoved(FocusManager focusManager, BaseMouseMoveEvent @event)
    {
        if (@event.Position.X > LayoutPaddingPosition.X && @event.Position.Y > LayoutPaddingPosition.Y && @event.Position.X < LayoutPaddingPosition.X + LayoutPaddingSize.X && @event.Position.Y < LayoutPaddingPosition.Y + LayoutPaddingSize.Y)
        {
            var relativeEvent = new MouseMoveEvent(
                Position: @event.Position,
                Buttons: @event.Buttons,
                CtrlKey: @event.CtrlKey,
                MetaKey: @event.AltKey,
                ShiftKey: @event.ShiftKey,
                RelativePosition: @event.Position - LayoutPaddingPosition
            );
            MouseMoved?.Invoke(new NodeEventArgs<MouseMoveEvent>(relativeEvent, focusManager));
            OnMouseMoved(focusManager, relativeEvent);
        }
        base.DispatchMouseMoved(focusManager, @event);
    }

    public override void DispatchMouseEntered(FocusManager focusManager, BaseMouseMoveEvent @event)
    {
        Logging.Info(
            $"[Node] DispatchMouseEntered {GetType().Name} Name='{Name}' " +
            $"Pos=({LayoutPaddingPosition.X:F0},{LayoutPaddingPosition.Y:F0}) " +
            $"Size=({LayoutPaddingSize.X:F0}x{LayoutPaddingSize.Y:F0}) " +
            $"Mouse=({@event.Position.X:F0},{@event.Position.Y:F0}) " +
            $"OldIsHovered={IsHovered}");
        IsHovered = true;
        var relativeEvent = new MouseMoveEvent(
            Position: @event.Position,
            Buttons: @event.Buttons,
            CtrlKey: @event.CtrlKey,
            MetaKey: @event.AltKey,
            ShiftKey: @event.ShiftKey,
            RelativePosition: @event.Position - LayoutPaddingPosition
        );
        MouseEntered?.Invoke(new NodeEventArgs<MouseMoveEvent>(relativeEvent, focusManager));
        OnMouseEntered(focusManager, relativeEvent);
        base.DispatchMouseEntered(focusManager, @event);
    }

    public override void DispatchMouseLeft(FocusManager focusManager, BaseMouseMoveEvent @event)
    {
        IsHovered = false;
        var relativeEvent = new MouseMoveEvent(
            Position: @event.Position,
            Buttons: @event.Buttons,
            CtrlKey: @event.CtrlKey,
            MetaKey: @event.AltKey,
            ShiftKey: @event.ShiftKey,
            RelativePosition: @event.Position - LayoutPaddingPosition
        );
        MouseLeft?.Invoke(new NodeEventArgs<MouseMoveEvent>(relativeEvent, focusManager));
        OnMouseLeft(focusManager, relativeEvent);
        base.DispatchMouseLeft(focusManager, @event);
    }

    public sealed override void DispatchMousePressed(FocusManager focusManager, BaseMouseEvent @event)
    {
        if (@event.Position.X > LayoutPaddingPosition.X && @event.Position.Y > LayoutPaddingPosition.Y && @event.Position.X < LayoutPaddingPosition.X + LayoutPaddingSize.X && @event.Position.Y < LayoutPaddingPosition.Y + LayoutPaddingSize.Y)
        {
            var relativeEvent = new MouseEvent(
                Position: @event.Position,
                Button: @event.Button,
                Buttons: @event.Buttons,
                CtrlKey: @event.CtrlKey,
                MetaKey: @event.AltKey,
                ShiftKey: @event.ShiftKey,
                RelativePosition: @event.Position - LayoutPaddingPosition
            );
            if (IsFocusable)
            {
                IsActive = true;
            }

            if (MousePressed != null)
            {
                Logging.Info(
                    $"[Node] DispatchMousePressed EXECUTING {GetType().Name} Name='{Name}' " +
                    $"Command={MousePressed.GetType().Name}");
                MousePressed?.Invoke(new NodeEventArgs<MouseEvent>(relativeEvent, focusManager));
            }
            else
            {
                Logging.Info(
                    $"[Node] DispatchMousePressed SKIP {GetType().Name} Name='{Name}' " +
                    $"MousePressed={(MousePressed is null ? "NULL" : MousePressed.GetType().Name)} " +
                    $"Pos=({LayoutPaddingPosition.X:F0},{LayoutPaddingPosition.Y:F0}) " +
                    $"Mouse=({@event.Position.X:F0},{@event.Position.Y:F0})");
            }
            OnMousePressed(focusManager, relativeEvent);
        }
        base.DispatchMousePressed(focusManager, @event);
    }

    public sealed override void DispatchMouseReleased(FocusManager focusManager, BaseMouseEvent @event)
    {
        if (@event.Position.X > LayoutPaddingPosition.X && @event.Position.Y > LayoutPaddingPosition.Y && @event.Position.X < LayoutPaddingPosition.X + LayoutPaddingSize.X && @event.Position.Y < LayoutPaddingPosition.Y + LayoutPaddingSize.Y)
        {
            var relativeEvent = new MouseEvent(
                Position: @event.Position,
                Button: @event.Button,
                Buttons: @event.Buttons,
                CtrlKey: @event.CtrlKey,
                MetaKey: @event.AltKey,
                ShiftKey: @event.ShiftKey,
                RelativePosition: @event.Position - LayoutPaddingPosition
            );
            if (IsFocusable)
            {
                IsActive = false;
            }

            MouseReleased?.Invoke(new NodeEventArgs<MouseEvent>(relativeEvent, focusManager));
            OnMouseReleased(focusManager, relativeEvent);
        }
        base.DispatchMouseReleased(focusManager, @event);
    }

    public sealed override void DispatchMouseDragged(FocusManager focusManager, BaseMouseDragEvent @event)
    {
        if (@event.DragStart.X > LayoutPaddingPosition.X && @event.DragStart.Y > LayoutPaddingPosition.Y && @event.DragStart.X < LayoutPaddingPosition.X + LayoutPaddingSize.X && @event.DragStart.Y < LayoutPaddingPosition.Y + LayoutPaddingSize.Y)
        {
            var relativeEvent = new MouseDragEvent(
                DragStart: @event.DragStart,
                RelativeDragStart: @event.DragStart - LayoutPaddingPosition,
                Position: @event.Position,
                Button: @event.Button,
                Buttons: @event.Buttons,
                CtrlKey: @event.CtrlKey,
                MetaKey: @event.MetaKey,
                ShiftKey: @event.ShiftKey,
                RelativePosition: @event.Position - LayoutPaddingPosition
            );
            MouseDragged?.Invoke(new NodeEventArgs<MouseDragEvent>(relativeEvent, focusManager));
            OnMouseDragged(focusManager, relativeEvent);
        }
        base.DispatchMouseDragged(focusManager, @event);
    }

    public sealed override void DispatchMouseScrolled(FocusManager focusManager, BaseMouseWheelEvent @event)
    {
        if (@event.Position.X > LayoutPaddingPosition.X && @event.Position.Y > LayoutPaddingPosition.Y && @event.Position.X < LayoutPaddingPosition.X + LayoutPaddingSize.X && @event.Position.Y < LayoutPaddingPosition.Y + LayoutPaddingSize.Y)
        {
            var relativeEvent = new MouseWheelEvent(
                Delta: @event.Delta,
                Position: @event.Position,
                Buttons: @event.Buttons,
                CtrlKey: @event.CtrlKey,
                MetaKey: @event.MetaKey,
                ShiftKey: @event.ShiftKey,
                RelativePosition: @event.Position - LayoutPaddingPosition
            );
            MouseScrolled?.Invoke(new NodeEventArgs<MouseWheelEvent>(relativeEvent, focusManager));
            OnMouseScrolled(focusManager, relativeEvent);
        }
        base.DispatchMouseScrolled(focusManager, @event);
    }

    public virtual void OnKeyPressed(FocusManager focusManager, KeyboardEvent @event)
    {
    }

    public virtual void OnKeyReleased(FocusManager focusManager, KeyboardEvent @event)
    {
    }

    public sealed override void DispatchKeyPressed(FocusManager focusManager, KeyboardEvent @event)
    {
        if (IsFocusable && IsFocused)
        {
            KeyPressed?.Invoke(new NodeEventArgs<KeyboardEvent>(@event, focusManager));
            OnKeyPressed(focusManager, @event);
        }
        base.DispatchKeyPressed(focusManager, @event);
    }

    public sealed override void DispatchKeyReleased(FocusManager focusManager, KeyboardEvent @event)
    {
        if (IsFocusable && IsFocused)
        {
            KeyReleased?.Invoke(new NodeEventArgs<KeyboardEvent>(@event, focusManager));
            OnKeyReleased(focusManager, @event);
        }
        base.DispatchKeyReleased(focusManager, @event);
    }

    public sealed override void DispatchKeyTyped(FocusManager focusManager, KeyboardTypingEvent @event)
    {
        if (IsFocusable && IsFocused)
        {
            KeyTyped?.Invoke(new NodeEventArgs<KeyboardTypingEvent>(@event, focusManager));
            OnKeyTyped(focusManager, @event);
        }
        base.DispatchKeyTyped(focusManager, @event);
    }
}