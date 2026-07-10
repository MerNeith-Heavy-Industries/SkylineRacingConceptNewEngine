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

    public Node()
    {
        Visibility = new(Reactor.Visibility.Visible, this, static (ctx, o, n) =>
        {
            Node node = (Node)ctx!;
            if (n is Reactor.Visibility.Visible && o is Reactor.Visibility.Hidden && node.Opacity.ComputedValue > 0.0f)
                node.Shown?.Invoke();
            else if (n is Reactor.Visibility.Hidden && o is Reactor.Visibility.Visible && node.Opacity.ComputedValue > 0.0f)
                node.Hidden?.Invoke();
        });
        Opacity = new(1.0f, this, static (ctx, o, n) =>
        {
            Node node = (Node)ctx!;
            if (n <= 0.0f && o > 0.0f && node.Visibility.ComputedValue is Reactor.Visibility.Visible)
                node.Hidden?.Invoke();
            else if (n > 0.0f && o <= 0.0f && node.Visibility.ComputedValue is Reactor.Visibility.Visible)
                node.Shown?.Invoke();
        });
        Direction = new(NodeInternal.Direction.ToNfmDirection(), this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.Direction = n.ToYogaDirection());
        FlexDirection = new(NodeInternal.FlexDirection.ToNfmFlexDirection(), this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.FlexDirection = n.ToYogaFlexDirection());
        JustifyContent = new(NodeInternal.JustifyContent.ToNfmJustify(), this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.JustifyContent = n.ToYogaJustify());
        AlignItems = new(NodeInternal.AlignItems.ToNfmAlign(), this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.AlignItems = n.ToYogaAlign());
        AlignSelf = new(NodeInternal.AlignSelf.ToNfmAlign(), this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.AlignSelf = n.ToYogaAlign());
        AlignContent = new(NodeInternal.AlignContent.ToNfmAlign(), this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.AlignContent = n.ToYogaAlign());
        Position = new(NodeInternal.PositionType.ToNfmPositionType(), this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.PositionType = n.ToYogaPositionType());
        FlexWrap = new(NodeInternal.FlexWrap.ToNfmWrap(), this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.FlexWrap = n.ToYogaWrap());
        Overflow = new(NodeInternal.Overflow.ToNfmOverflow(), this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.Overflow = n.ToYogaOverflow());
        Display = new(NodeInternal.Display.ToNfmDisplay(), this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.Display = n.ToYogaDisplay());
        BoxSizing = new(NodeInternal.BoxSizing.ToNfmBoxSizing(), this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.BoxSizing = n.ToYogaBoxSizing());
        Flex = new(null, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.Flex = n ?? float.NaN);
        FlexGrow = new(null, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.FlexGrow = n ?? float.NaN);
        FlexShrink = new(null, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.FlexShrink = n ?? float.NaN);
        FlexBasis = new(MeasurementFlexBasis.Undefined, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.FlexBasis = n.Scale(G.Scale));
        Left = new(MeasurementMarginPosition.Undefined, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.Left = n.Scale(G.Scale));
        Top = new(MeasurementMarginPosition.Undefined, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.Top = n.Scale(G.Scale));
        Right = new(MeasurementMarginPosition.Undefined, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.Right = n.Scale(G.Scale));
        Bottom = new(MeasurementMarginPosition.Undefined, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.Bottom = n.Scale(G.Scale));
        MarginTop = new(MeasurementMarginPosition.Undefined, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.MarginTop = n.Scale(G.Scale));
        MarginBottom = new(MeasurementMarginPosition.Undefined, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.MarginBottom = n.Scale(G.Scale));
        MarginLeft = new(MeasurementMarginPosition.Undefined, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.MarginLeft = n.Scale(G.Scale));
        MarginRight = new(MeasurementMarginPosition.Undefined, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.MarginRight = n.Scale(G.Scale));
        PaddingTop = new(MeasurementPadding.Undefined, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.PaddingTop = n.Scale(G.Scale));
        PaddingBottom = new(MeasurementPadding.Undefined, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.PaddingBottom = n.Scale(G.Scale));
        PaddingLeft = new(MeasurementPadding.Undefined, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.PaddingLeft = n.Scale(G.Scale));
        PaddingRight = new(MeasurementPadding.Undefined, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.PaddingRight = n.Scale(G.Scale));
        BorderTop = new(null, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.BorderTop = n?.Value * G.Scale ?? YG.YGUndefined);
        BorderBottom = new(null, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.BorderBottom = n?.Value * G.Scale ?? YG.YGUndefined);
        BorderLeft = new(null, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.BorderLeft = n?.Value * G.Scale ?? YG.YGUndefined);
        BorderRight = new(null, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.BorderRight = n?.Value * G.Scale ?? YG.YGUndefined);
        GapColumn = new(MeasurementGap.Undefined, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.GapColumn = n);
        GapRow = new(MeasurementGap.Undefined, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.GapRow = n);
        Width = new(MeasurementWidthHeight.Undefined, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.Width = n.Scale(G.Scale));
        Height = new(MeasurementWidthHeight.Undefined, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.Height = n.Scale(G.Scale));
        MinWidth = new(MeasurementWidthHeight.Undefined, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.MinWidth = n.Scale(G.Scale));
        MinHeight = new(MeasurementWidthHeight.Undefined, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.MinHeight = n.Scale(G.Scale));
        MaxWidth = new(MeasurementWidthHeight.Undefined, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.MaxWidth = n.Scale(G.Scale));
        MaxHeight = new(MeasurementWidthHeight.Undefined, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.MaxHeight = n.Scale(G.Scale));
        AspectRatio = new(null, this, (PropertyChangedHandler<Pixels?>)(static (ctx, o, n) => ((Node)ctx!).NodeInternal.AspectRatio = n?.Value ?? float.NaN));
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
    /// Triggered when <see cref="Reactor.Visibility"/> is set to <see cref="Visibility.Visible"/>
    /// </summary>
    public Action? Shown { get; set; }
    
    /// <summary>
    /// Triggered when <see cref="Reactor.Visibility"/> is set to <see cref="Visibility.Hidden"/>
    /// </summary>
    public Action? Hidden { get; set; }

    #endregion

    #region Layout

    // https://www.w3schools.com/css/css_boxmodel.asp
    private protected Vector2 Root;
    
    /// <summary>
    /// In the CSS box model, gets the top-left position of the margin box.
    /// </summary>
    public Vector2 LayoutMarginPosition => Root + new Vector2(LayoutX, LayoutY);
    
    /// <summary>
    /// In the CSS box model, gets the size of the margin box, from the top-left to the bottom-right.
    /// </summary>
    public Vector2 LayoutMarginSize => new(LayoutWidth, LayoutHeight);
    
    /// <summary>
    /// In the CSS box model, gets the top-left position of the border box.
    /// </summary>
    public Vector2 LayoutBorderPosition => Root + new Vector2(LayoutX + LayoutMarginLeft, LayoutY + LayoutMarginTop);
    
    /// <summary>
    /// In the CSS box model, gets the size of the border box, from the top-left to the bottom-right.
    /// </summary>
    public Vector2 LayoutBorderSize => new(LayoutWidth - (LayoutMarginLeft + LayoutMarginRight), LayoutHeight - (LayoutMarginTop + LayoutMarginBottom));
    
    /// <summary>
    /// In the CSS box model, gets the top-left position of the padding box.
    /// </summary>
    public Vector2 LayoutPaddingPosition => Root + new Vector2(LayoutX + LayoutMarginLeft + LayoutBorderLeft, LayoutY + LayoutMarginTop + LayoutBorderTop);
    
    /// <summary>
    /// In the CSS box model, gets the size of the padding box, from the top-left to the bottom-right.
    /// </summary>
    public Vector2 LayoutPaddingSize => new(LayoutWidth - (LayoutMarginLeft + LayoutMarginRight + LayoutBorderLeft + LayoutBorderRight), LayoutHeight - (LayoutMarginTop + LayoutMarginBottom + LayoutBorderTop + LayoutBorderBottom));
    
    /// <summary>
    /// In the CSS box model, gets the top-left position of the content box.
    /// </summary>
    public Vector2 LayoutContentPosition => Root + new Vector2(LayoutX + LayoutMarginLeft + LayoutBorderLeft + LayoutPaddingLeft, LayoutY + LayoutMarginTop + LayoutBorderTop + LayoutPaddingTop);
    
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
            if (oldStyleSheetValue.Visibility is not null) Visibility.ClearStyleValue();
            if (oldStyleSheetValue.Opacity is not null) Opacity.ClearStyleValue();
            if (oldStyleSheetValue.Direction is not null) Direction.ClearStyleValue();
            if (oldStyleSheetValue.FlexDirection is not null) FlexDirection.ClearStyleValue();
            if (oldStyleSheetValue.JustifyContent is not null) JustifyContent.ClearStyleValue();
            if (oldStyleSheetValue.AlignItems is not null) AlignItems.ClearStyleValue();
            if (oldStyleSheetValue.AlignSelf is not null) AlignSelf.ClearStyleValue();
            if (oldStyleSheetValue.AlignContent is not null) AlignContent.ClearStyleValue();
            if (oldStyleSheetValue.Position is not null) Position.ClearStyleValue();
            if (oldStyleSheetValue.FlexWrap is not null) FlexWrap.ClearStyleValue();
            if (oldStyleSheetValue.Overflow is not null) Overflow.ClearStyleValue();
            if (oldStyleSheetValue.Display is not null) Display.ClearStyleValue();
            if (oldStyleSheetValue.Flex is not null) Flex.ClearStyleValue();
            if (oldStyleSheetValue.FlexGrow is not null) FlexGrow.ClearStyleValue();
            if (oldStyleSheetValue.FlexShrink is not null) FlexShrink.ClearStyleValue();
            if (oldStyleSheetValue.FlexBasis is not null) FlexBasis.ClearStyleValue();
            if (oldStyleSheetValue.Left is not null) Left.ClearStyleValue();
            if (oldStyleSheetValue.Top is not null) Top.ClearStyleValue();
            if (oldStyleSheetValue.Right is not null) Right.ClearStyleValue();
            if (oldStyleSheetValue.Bottom is not null) Bottom.ClearStyleValue();
            if (oldStyleSheetValue.Margin is not null)
            {
                MarginTop.ClearStyleValue();
                MarginBottom.ClearStyleValue();
                MarginLeft.ClearStyleValue();
                MarginRight.ClearStyleValue();
            }
            if (oldStyleSheetValue.MarginTop is not null) MarginTop.ClearStyleValue();
            if (oldStyleSheetValue.MarginBottom is not null) MarginBottom.ClearStyleValue();
            if (oldStyleSheetValue.MarginLeft is not null) MarginLeft.ClearStyleValue();
            if (oldStyleSheetValue.MarginRight is not null) MarginRight.ClearStyleValue();
            if (oldStyleSheetValue.Padding is not null)
            {
                PaddingTop.ClearStyleValue();
                PaddingBottom.ClearStyleValue();
                PaddingLeft.ClearStyleValue();
                PaddingRight.ClearStyleValue();
            }

            if (oldStyleSheetValue.PaddingTop is not null) PaddingTop.ClearStyleValue();
            if (oldStyleSheetValue.PaddingBottom is not null) PaddingBottom.ClearStyleValue();
            if (oldStyleSheetValue.PaddingLeft is not null) PaddingLeft.ClearStyleValue();
            if (oldStyleSheetValue.PaddingRight is not null) PaddingRight.ClearStyleValue();
            if (oldStyleSheetValue.Border is not null)
            {
                BorderTop.ClearStyleValue();
                BorderBottom.ClearStyleValue();
                BorderLeft.ClearStyleValue();
                BorderRight.ClearStyleValue();
            }

            if (oldStyleSheetValue.BorderTop is not null) BorderTop.ClearStyleValue();
            if (oldStyleSheetValue.BorderBottom is not null) BorderBottom.ClearStyleValue();
            if (oldStyleSheetValue.BorderLeft is not null) BorderLeft.ClearStyleValue();
            if (oldStyleSheetValue.BorderRight is not null) BorderRight.ClearStyleValue();
            if (oldStyleSheetValue.Gap is not null)
            {
                GapColumn.ClearStyleValue();
                GapRow.ClearStyleValue();
            }
            if (oldStyleSheetValue.GapColumn is not null) GapColumn.ClearStyleValue();
            if (oldStyleSheetValue.GapRow is not null) GapRow.ClearStyleValue();
            if (oldStyleSheetValue.BoxSizing is not null) BoxSizing.ClearStyleValue();
            if (oldStyleSheetValue.Width is not null) Width.ClearStyleValue();
            if (oldStyleSheetValue.Height is not null) Height.ClearStyleValue();
            if (oldStyleSheetValue.MinWidth is not null) MinWidth.ClearStyleValue();
            if (oldStyleSheetValue.MinHeight is not null) MinHeight.ClearStyleValue();
            if (oldStyleSheetValue.MaxWidth is not null) MaxWidth.ClearStyleValue();
            if (oldStyleSheetValue.MaxHeight is not null) MaxHeight.ClearStyleValue();
            if (oldStyleSheetValue.AspectRatio is not null) AspectRatio.ClearStyleValue();
        }
        
        if (newStyleSheet is { } newStyleSheetValue)
        {
            if (newStyleSheetValue.Visibility is { } visibility) Visibility.StyleValue = visibility;
            if (newStyleSheetValue.Opacity is { } opacity) Opacity.StyleValue = opacity;
            if (newStyleSheetValue.Direction is { } direction) Direction.StyleValue = direction;
            if (newStyleSheetValue.FlexDirection is { } flexDirection) FlexDirection.StyleValue = flexDirection;
            if (newStyleSheetValue.JustifyContent is { } justifyContent) JustifyContent.StyleValue = justifyContent;
            if (newStyleSheetValue.AlignItems is { } alignItems) AlignItems.StyleValue = alignItems;
            if (newStyleSheetValue.AlignSelf is { } alignSelf) AlignSelf.StyleValue = alignSelf;
            if (newStyleSheetValue.AlignContent is { } alignContent) AlignContent.StyleValue = alignContent;
            if (newStyleSheetValue.Position is { } position) Position.StyleValue = position;
            if (newStyleSheetValue.FlexWrap is { } flexWrap) FlexWrap.StyleValue = flexWrap;
            if (newStyleSheetValue.Overflow is { } overflow) Overflow.StyleValue = overflow;
            if (newStyleSheetValue.Display is { } display) Display.StyleValue = display;
            if (newStyleSheetValue.Flex is { } flex) Flex.StyleValue = flex;
            if (newStyleSheetValue.FlexGrow is { } flexGrow) FlexGrow.StyleValue = flexGrow;
            if (newStyleSheetValue.FlexShrink is { } flexShrink) FlexShrink.StyleValue = flexShrink;
            if (newStyleSheetValue.FlexBasis is { } flexBasis) FlexBasis.StyleValue = flexBasis;
            if (newStyleSheetValue.Left is { } left) Left.StyleValue = left;
            if (newStyleSheetValue.Top is { } top) Top.StyleValue = top;
            if (newStyleSheetValue.Right is { } right) Right.StyleValue = right;
            if (newStyleSheetValue.Bottom is { } bottom) Bottom.StyleValue = bottom;
            if (newStyleSheetValue.Margin is { } margin)
            {
                MarginTop.StyleValue = margin.Top;
                MarginBottom.StyleValue = margin.Bottom;
                MarginLeft.StyleValue = margin.Left;
                MarginRight.StyleValue = margin.Right;
            }

            if (newStyleSheetValue.MarginTop is { } marginTop) MarginTop.StyleValue = marginTop;
            if (newStyleSheetValue.MarginBottom is { } marginBottom) MarginBottom.StyleValue = marginBottom;
            if (newStyleSheetValue.MarginLeft is { } marginLeft) MarginLeft.StyleValue = marginLeft;
            if (newStyleSheetValue.MarginRight is { } marginRight) MarginRight.StyleValue = marginRight;
            if (newStyleSheetValue.Padding is { } padding)
            {
                PaddingTop.StyleValue = padding.Top;
                PaddingBottom.StyleValue = padding.Bottom;
                PaddingLeft.StyleValue = padding.Left;
                PaddingRight.StyleValue = padding.Right;
            }

            if (newStyleSheetValue.PaddingTop is { } paddingTop) PaddingTop.StyleValue = paddingTop;
            if (newStyleSheetValue.PaddingBottom is { } paddingBottom) PaddingBottom.StyleValue = paddingBottom;
            if (newStyleSheetValue.PaddingLeft is { } paddingLeft) PaddingLeft.StyleValue = paddingLeft;
            if (newStyleSheetValue.PaddingRight is { } paddingRight) PaddingRight.StyleValue = paddingRight;
            if (newStyleSheetValue.Border is { } border)
            {
                BorderTop.StyleValue = border.Top;
                BorderBottom.StyleValue = border.Bottom;
                BorderLeft.StyleValue = border.Left;
                BorderRight.StyleValue = border.Right;
            }

            if (newStyleSheetValue.BorderTop is { } borderTop) BorderTop.StyleValue = borderTop;
            if (newStyleSheetValue.BorderBottom is { } borderBottom) BorderBottom.StyleValue = borderBottom;
            if (newStyleSheetValue.BorderLeft is { } borderLeft) BorderLeft.StyleValue = borderLeft;
            if (newStyleSheetValue.BorderRight is { } borderRight) BorderRight.StyleValue = borderRight;
            if (newStyleSheetValue.Gap is { } gap)
            {
                GapColumn.StyleValue = gap;
                GapRow.StyleValue = gap;
            }

            if (newStyleSheetValue.GapColumn is { } gapColumn) GapColumn.StyleValue = gapColumn;
            if (newStyleSheetValue.GapRow is { } gapRow) GapRow.StyleValue = gapRow;
            if (newStyleSheetValue.BoxSizing is { } boxSizing) BoxSizing.StyleValue = boxSizing;
            if (newStyleSheetValue.Width is { } width) Width.StyleValue = width;
            if (newStyleSheetValue.Height is { } height) Height.StyleValue = height;
            if (newStyleSheetValue.MinWidth is { } minWidth) MinWidth.StyleValue = minWidth;
            if (newStyleSheetValue.MinHeight is { } minHeight) MinHeight.StyleValue = minHeight;
            if (newStyleSheetValue.MaxWidth is { } maxWidth) MaxWidth.StyleValue = maxWidth;
            if (newStyleSheetValue.MaxHeight is { } maxHeight) MaxHeight.StyleValue = maxHeight;
            if (newStyleSheetValue.AspectRatio is { } aspectRatio) AspectRatio.StyleValue = aspectRatio;
        }
    }

    /// <summary>
    /// CSS: visibility - Controls whether the element is visible (visible/hidden/collapsed)
    /// </summary>
    public StyledProperty<Visibility> Visibility;
    
    /// <summary>
    /// CSS: opacity - Sets the transparency level (0.0 = fully transparent, 1.0 = fully opaque)
    /// </summary>
    public StyledProperty<float> Opacity;
    
    // https://css-tricks.com/snippets/css/a-guide-to-flexbox/
    /// <summary>
    /// CSS: direction - Establishes the main-axis (ltr/rtl/inherit)
    /// </summary>
    public StyledProperty<Direction> Direction;
    
    /// <summary>
    /// CSS: flex-direction - Establishes the main-axis (row/column/row-reverse/column-reverse)
    /// </summary>
    public StyledProperty<FlexDirection> FlexDirection;
    
    /// <summary>
    /// CSS: justify-content - Defines alignment along the main axis
    /// </summary>
    public StyledProperty<Justify> JustifyContent;
    
    /// <summary>
    /// CSS: align-items - Defines default alignment for all children along the cross axis
    /// </summary>
    public StyledProperty<Align> AlignItems;
    
    /// <summary>
    /// CSS: align-self - Allows a child to override the default cross-axis alignment
    /// </summary>
    public StyledProperty<Align> AlignSelf;
    
    /// <summary>
    /// CSS: align-content - Aligns flex container's lines when there is extra space in the cross-axis
    /// </summary>
    public StyledProperty<Align> AlignContent;
    
    /// <summary>
    /// CSS: position - Sets how an element is positioned (static/relative/absolute/fixed)
    /// </summary>
    public StyledProperty<Position> Position;
    
    /// <summary>
    /// CSS: flex-wrap - Controls whether flex items wrap onto multiple lines (nowrap/wrap/wrap-reverse)
    /// </summary>
    public StyledProperty<Wrap> FlexWrap;
    
    /// <summary>
    /// CSS: overflow - Controls what happens to content that is too big to fit (visible/hidden/scroll)
    /// </summary>
    public StyledProperty<Overflow> Overflow;
    
    /// <summary>
    /// CSS: display - Defines the display type of the element (flex/none/block)
    /// </summary>
    public StyledProperty<Display> Display;
    
    /// <summary>
    /// CSS: box-sizing - Defines how width/height calculations include padding/border (content-box/border-box)
    /// </summary>
    public StyledProperty<BoxSizing> BoxSizing;
    
    /// <summary>
    /// CSS: flex - Shorthand for flex-grow, flex-shrink, and flex-basis combined
    /// </summary>
    public StyledProperty<float?> Flex;
    
    /// <summary>
    /// CSS: flex-grow - Defines the ability for a flex item to grow if necessary
    /// </summary>
    public StyledProperty<float?> FlexGrow;
    
    /// <summary>
    /// CSS: flex-shrink - Defines the ability for a flex item to shrink if necessary
    /// </summary>
    public StyledProperty<float?> FlexShrink;
    
    /// <summary>
    /// CSS: flex-basis - Defines the default size of an element before remaining space is distributed
    /// </summary>
    public StyledProperty<MeasurementFlexBasis> FlexBasis;
    
    /// <summary>
    /// CSS: left - Specifies the left position of a positioned element
    /// </summary>
    public StyledProperty<MeasurementMarginPosition> Left;

    /// <summary>
    /// CSS: top - Specifies the top position of a positioned element
    /// </summary>
    public StyledProperty<MeasurementMarginPosition> Top;

    /// <summary>
    /// CSS: right - Specifies the right position of a positioned element
    /// </summary>
    public StyledProperty<MeasurementMarginPosition> Right;

    /// <summary>
    /// CSS: bottom - Specifies the bottom position of a positioned element
    /// </summary>
    public StyledProperty<MeasurementMarginPosition> Bottom;

    /// <summary>
    /// CSS: margin-top - Sets the top margin space outside the element
    /// </summary>
    public StyledProperty<MeasurementMarginPosition> MarginTop;

    /// <summary>
    /// CSS: margin-bottom - Sets the bottom margin space outside the element
    /// </summary>
    public StyledProperty<MeasurementMarginPosition> MarginBottom;

    /// <summary>
    /// CSS: margin-left - Sets the left margin space outside the element
    /// </summary>
    public StyledProperty<MeasurementMarginPosition> MarginLeft;

    /// <summary>
    /// CSS: margin-right - Sets the right margin space outside the element
    /// </summary>
    public StyledProperty<MeasurementMarginPosition> MarginRight;

    /// <summary>
    /// CSS: padding-top - Sets the top padding space inside the element
    /// </summary>
    public StyledProperty<MeasurementPadding> PaddingTop;

    /// <summary>
    /// CSS: padding-bottom - Sets the bottom padding space inside the element
    /// </summary>
    public StyledProperty<MeasurementPadding> PaddingBottom;

    /// <summary>
    /// CSS: padding-left - Sets the left padding space inside the element
    /// </summary>
    public StyledProperty<MeasurementPadding> PaddingLeft;

    /// <summary>
    /// CSS: padding-right - Sets the right padding space inside the element
    /// </summary>
    public StyledProperty<MeasurementPadding> PaddingRight;

    /// <summary>
    /// CSS: border-top-width - Sets the width of the top border
    /// </summary>
    public StyledProperty<Pixels?> BorderTop;

    /// <summary>
    /// CSS: border-bottom-width - Sets the width of the bottom border
    /// </summary>
    public StyledProperty<Pixels?> BorderBottom;

    /// <summary>
    /// CSS: border-left-width - Sets the width of the left border
    /// </summary>
    public StyledProperty<Pixels?> BorderLeft;

    /// <summary>
    /// CSS: border-right-width - Sets the width of the right border
    /// </summary>
    public StyledProperty<Pixels?> BorderRight;
    
    /// <summary>
    /// CSS: column-gap - Sets the gap between columns in a flex container
    /// </summary>
    public StyledProperty<MeasurementGap> GapColumn;

    /// <summary>
    /// CSS: row-gap - Sets the gap between rows in a flex container
    /// </summary>
    public StyledProperty<MeasurementGap> GapRow;
    
    /// <summary>
    /// CSS: width - Sets the width of the element
    /// </summary>
    public StyledProperty<MeasurementWidthHeight> Width;

    /// <summary>
    /// CSS: height - Sets the height of the element
    /// </summary>
    public StyledProperty<MeasurementWidthHeight> Height;

    /// <summary>
    /// CSS: min-width - Sets the minimum width of the element
    /// </summary>
    public StyledProperty<MeasurementWidthHeight> MinWidth;

    /// <summary>
    /// CSS: min-height - Sets the minimum height of the element
    /// </summary>
    public StyledProperty<MeasurementWidthHeight> MinHeight;

    /// <summary>
    /// CSS: max-width - Sets the maximum width of the element
    /// </summary>
    public StyledProperty<MeasurementWidthHeight> MaxWidth;

    /// <summary>
    /// CSS: max-height - Sets the maximum height of the element
    /// </summary>
    public StyledProperty<MeasurementWidthHeight> MaxHeight;

    /// <summary>
    /// CSS: aspect-ratio - Sets the preferred aspect ratio for the element (width / height)
    /// </summary>
    public StyledProperty<Pixels?> AspectRatio;


    /// <summary>
    /// CSS: margin - Shorthand for setting all margin values (top, right, bottom, left)
    /// </summary>
    [Property]
    public MeasurementMultiMargin Margin
    {
        get => new()
        {
            Top = MarginTop.OverrideValue,
            Bottom = MarginBottom.OverrideValue,
            Left = MarginLeft.OverrideValue,
            Right = MarginRight.OverrideValue
        };
        set
        {
            MarginLeft.OverrideValue = value.Left;
            MarginRight.OverrideValue = value.Right;
            MarginTop.OverrideValue = value.Top;
            MarginBottom.OverrideValue = value.Bottom;
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
            Left = PaddingLeft.OverrideValue,
            Right = PaddingRight.OverrideValue,
            Top = PaddingTop.OverrideValue,
            Bottom = PaddingBottom.OverrideValue
        };
        set
        {
            PaddingLeft.OverrideValue = value.Left;
            PaddingRight.OverrideValue = value.Right;
            PaddingTop.OverrideValue = value.Top;
            PaddingBottom.OverrideValue = value.Bottom;
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
            Left = BorderLeft.OverrideValue,
            Right = BorderRight.OverrideValue,
            Top = BorderTop.OverrideValue,
            Bottom = BorderBottom.OverrideValue
        };
        set
        {
            BorderLeft.OverrideValue = value.Left;
            BorderRight.OverrideValue = value.Right;
            BorderTop.OverrideValue = value.Top;
            BorderBottom.OverrideValue = value.Bottom;
        }
    }

    /// <summary>
    /// CSS: gap - Shorthand for setting row-gap and column-gap
    /// </summary>
    [Property]
    public MeasurementGap Gap
    {
        get => GapColumn.OverrideValue == GapRow.OverrideValue ? GapColumn.OverrideValue : MeasurementGap.Undefined;
        set
        {
            GapColumn.OverrideValue = value;
            GapRow.OverrideValue = value;
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
            // Re-trigger all size-related onChanged handlers so they re-scale with new G.Scale
#pragma warning disable CA2245
            NodeInternal.Width = Width.ComputedValue.Scale(G.Scale);
            NodeInternal.Height = Height.ComputedValue.Scale(G.Scale);
            NodeInternal.MinWidth = MinWidth.ComputedValue.Scale(G.Scale);
            NodeInternal.MinHeight = MinHeight.ComputedValue.Scale(G.Scale);
            NodeInternal.MaxWidth = MaxWidth.ComputedValue.Scale(G.Scale);
            NodeInternal.MaxHeight = MaxHeight.ComputedValue.Scale(G.Scale);
            NodeInternal.MarginTop = MarginTop.ComputedValue.Scale(G.Scale);
            NodeInternal.MarginBottom = MarginBottom.ComputedValue.Scale(G.Scale);
            NodeInternal.MarginLeft = MarginLeft.ComputedValue.Scale(G.Scale);
            NodeInternal.MarginRight = MarginRight.ComputedValue.Scale(G.Scale);
            NodeInternal.PaddingTop = PaddingTop.ComputedValue.Scale(G.Scale);
            NodeInternal.PaddingBottom = PaddingBottom.ComputedValue.Scale(G.Scale);
            NodeInternal.PaddingLeft = PaddingLeft.ComputedValue.Scale(G.Scale);
            NodeInternal.PaddingRight = PaddingRight.ComputedValue.Scale(G.Scale);
            NodeInternal.BorderTop = BorderTop.ComputedValue?.Value * G.Scale ?? YG.YGUndefined;
            NodeInternal.BorderBottom = BorderBottom.ComputedValue?.Value * G.Scale ?? YG.YGUndefined;
            NodeInternal.BorderLeft = BorderLeft.ComputedValue?.Value * G.Scale ?? YG.YGUndefined;
            NodeInternal.BorderRight = BorderRight.ComputedValue?.Value * G.Scale ?? YG.YGUndefined;
            NodeInternal.GapColumn = GapColumn.ComputedValue;
            NodeInternal.GapRow = GapRow.ComputedValue;
            NodeInternal.FlexBasis = FlexBasis.ComputedValue.Scale(G.Scale);
            NodeInternal.Left = Left.ComputedValue.Scale(G.Scale);
            NodeInternal.Top = Top.ComputedValue.Scale(G.Scale);
            NodeInternal.Right = Right.ComputedValue.Scale(G.Scale);
            NodeInternal.Bottom = Bottom.ComputedValue.Scale(G.Scale);
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
        Root = context.TopLeft;
        if (Display.ComputedValue != Reactor.Display.None && Visibility.ComputedValue == Reactor.Visibility.Visible && Opacity.ComputedValue > 0f)
        {
            var ownOpacity = context.InheritedOpacity * Opacity.ComputedValue;
            G.Alpha = ownOpacity;
            RenderBackground(LayoutPaddingPosition, LayoutPaddingSize);
            RenderBorder(LayoutBorderPosition, LayoutBorderSize);
            RenderContent(LayoutContentPosition, LayoutContentSize);
            foreach (var child in GetChildSnapshot())
            {
                child.Render(new RenderContext(Root + new Vector2(LayoutX, LayoutY), ownOpacity)); // todo should this be LayoutContentPosition
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

public struct MeasurementMarginPosition : IEquatable<MeasurementMarginPosition>
{
    internal YGValue InternalValue;
    public YGUnit Unit => InternalValue.unit;
    public float Value => InternalValue.value;
    public float? PointValue => InternalValue.unit == YGUnit.YGUnitPoint ? InternalValue.value : null;
    public float? PercentValue => InternalValue.unit == YGUnit.YGUnitPercent ? InternalValue.value : null;

    public bool Equals(MeasurementMarginPosition other) => InternalValue.unit == other.InternalValue.unit && InternalValue.value == other.InternalValue.value;
    public override bool Equals(object? obj) => obj is MeasurementMarginPosition other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(InternalValue.unit, InternalValue.value);

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

public struct MeasurementMultiMargin : IEquatable<MeasurementMultiMargin>
{
    public InlineArray4<MeasurementMarginPosition> Sides;

    public bool Equals(MeasurementMultiMargin other) => Sides[0].Equals(other.Sides[0]) && Sides[1].Equals(other.Sides[1]) && Sides[2].Equals(other.Sides[2]) && Sides[3].Equals(other.Sides[3]);
    public override bool Equals(object? obj) => obj is MeasurementMultiMargin other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Sides[0], Sides[1], Sides[2], Sides[3]);
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

public struct MeasurementPadding : IEquatable<MeasurementPadding>
{
    internal YGValue InternalValue;
    public YGUnit Unit => InternalValue.unit;
    public float Value => InternalValue.value;
    public float? PointValue => InternalValue.unit == YGUnit.YGUnitPoint ? InternalValue.value : null;
    public float? PercentValue => InternalValue.unit == YGUnit.YGUnitPercent ? InternalValue.value : null;

    public bool Equals(MeasurementPadding other) => InternalValue.unit == other.InternalValue.unit && InternalValue.value == other.InternalValue.value;
    public override bool Equals(object? obj) => obj is MeasurementPadding other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(InternalValue.unit, InternalValue.value);

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

public struct MeasurementMultiPadding : IEquatable<MeasurementMultiPadding>
{
    public InlineArray4<MeasurementPadding> Sides;

    public bool Equals(MeasurementMultiPadding other) => Sides[0].Equals(other.Sides[0]) && Sides[1].Equals(other.Sides[1]) && Sides[2].Equals(other.Sides[2]) && Sides[3].Equals(other.Sides[3]);
    public override bool Equals(object? obj) => obj is MeasurementMultiPadding other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Sides[0], Sides[1], Sides[2], Sides[3]);
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

public struct MeasurementMultiBorder : IEquatable<MeasurementMultiBorder>
{
    public InlineArray4<float?> Sides;

    public bool Equals(MeasurementMultiBorder other) => Nullable.Equals(Sides[0], other.Sides[0]) && Nullable.Equals(Sides[1], other.Sides[1]) && Nullable.Equals(Sides[2], other.Sides[2]) && Nullable.Equals(Sides[3], other.Sides[3]);
    public override bool Equals(object? obj) => obj is MeasurementMultiBorder other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(Sides[0], Sides[1], Sides[2], Sides[3]);
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

public struct MeasurementGap : IEquatable<MeasurementGap>
{
    internal YGValue InternalValue;
    public YGUnit Unit => InternalValue.unit;
    public float Value => InternalValue.value;
    public float? PointValue => InternalValue.unit == YGUnit.YGUnitPoint ? InternalValue.value : null;
    public float? PercentValue => InternalValue.unit == YGUnit.YGUnitPercent ? InternalValue.value : null;

    public bool Equals(MeasurementGap other) => InternalValue.unit == other.InternalValue.unit && InternalValue.value == other.InternalValue.value;
    public override bool Equals(object? obj) => obj is MeasurementGap other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(InternalValue.unit, InternalValue.value);

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

public readonly struct Pixels(float value) : IEquatable<Pixels>
{
    public readonly float Value = value;

    public bool Equals(Pixels other) => Value == other.Value;
    public override bool Equals(object? obj) => obj is Pixels other && Equals(other);
    public override int GetHashCode() => Value.GetHashCode();
        
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

public struct MeasurementFlexBasis : IEquatable<MeasurementFlexBasis>
{
    internal YGValue InternalValue;
    public YGUnit Unit => InternalValue.unit;
    public float Value => InternalValue.value;
    public float? PointValue => InternalValue.unit == YGUnit.YGUnitPoint ? InternalValue.value : null;
    public float? PercentValue => InternalValue.unit == YGUnit.YGUnitPercent ? InternalValue.value : null;

    public bool Equals(MeasurementFlexBasis other) => InternalValue.unit == other.InternalValue.unit && InternalValue.value == other.InternalValue.value;
    public override bool Equals(object? obj) => obj is MeasurementFlexBasis other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(InternalValue.unit, InternalValue.value);

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

public struct MeasurementWidthHeight : IEquatable<MeasurementWidthHeight>
{
    internal YGValue InternalValue;
    public YGUnit Unit => InternalValue.unit;
    public float Value => InternalValue.value;
    public float? PointValue => InternalValue.unit == YGUnit.YGUnitPoint ? InternalValue.value : null;
    public float? PercentValue => InternalValue.unit == YGUnit.YGUnitPercent ? InternalValue.value : null;

    public bool Equals(MeasurementWidthHeight other) => InternalValue.unit == other.InternalValue.unit && InternalValue.value == other.InternalValue.value;
    public override bool Equals(object? obj) => obj is MeasurementWidthHeight other && Equals(other);
    public override int GetHashCode() => HashCode.Combine(InternalValue.unit, InternalValue.value);

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