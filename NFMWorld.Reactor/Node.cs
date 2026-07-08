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
        _opacity = new(1.0f, this, static (ctx, o, n) =>
        {
            Node tempQualifier = (Node)ctx!;
            if (n <= 0.0f && o > 0.0f && tempQualifier.Visibility is Visibility.Visible)
                tempQualifier.Hidden?.Invoke();
            else if (n > 0.0f && o <= 0.0f && tempQualifier.Visibility is Visibility.Visible)
                tempQualifier.Shown?.Invoke();
        });
        _direction = new(NodeInternal.Direction.ToNfmDirection(), this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.Direction = n.ToYogaDirection());
        _flexDirection = new(NodeInternal.FlexDirection.ToNfmFlexDirection(), this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.FlexDirection = n.ToYogaFlexDirection());
        _justifyContent = new(NodeInternal.JustifyContent.ToNfmJustify(), this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.JustifyContent = n.ToYogaJustify());
        _alignItems = new(NodeInternal.AlignItems.ToNfmAlign(), this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.AlignItems = n.ToYogaAlign());
        _alignSelf = new(NodeInternal.AlignSelf.ToNfmAlign(), this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.AlignSelf = n.ToYogaAlign());
        _alignContent = new(NodeInternal.AlignContent.ToNfmAlign(), this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.AlignContent = n.ToYogaAlign());
        _position = new(NodeInternal.PositionType.ToNfmPositionType(), this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.PositionType = n.ToYogaPositionType());
        _flexWrap = new(NodeInternal.FlexWrap.ToNfmWrap(), this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.FlexWrap = n.ToYogaWrap());
        _overflow = new(NodeInternal.Overflow.ToNfmOverflow(), this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.Overflow = n.ToYogaOverflow());
        _display = new(NodeInternal.Display.ToNfmDisplay(), this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.Display = n.ToYogaDisplay());
        _boxSizing = new(NodeInternal.BoxSizing.ToNfmBoxSizing(), this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.BoxSizing = n.ToYogaBoxSizing());
        _flex = new(null, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.Flex = n ?? float.NaN);
        _flexGrow = new(null, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.FlexGrow = n ?? float.NaN);
        _flexShrink = new(null, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.FlexShrink = n ?? float.NaN);
        _flexBasis = new(MeasurementFlexBasis.Undefined, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.FlexBasis = n.Scale(G.Scale));
        _left = new(MeasurementMarginPosition.Undefined, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.Left = n.Scale(G.Scale));
        _top = new(MeasurementMarginPosition.Undefined, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.Top = n.Scale(G.Scale));
        _right = new(MeasurementMarginPosition.Undefined, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.Right = n.Scale(G.Scale));
        _bottom = new(MeasurementMarginPosition.Undefined, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.Bottom = n.Scale(G.Scale));
        _marginTop = new(MeasurementMarginPosition.Undefined, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.MarginTop = n.Scale(G.Scale));
        _marginBottom = new(MeasurementMarginPosition.Undefined, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.MarginBottom = n.Scale(G.Scale));
        _marginLeft = new(MeasurementMarginPosition.Undefined, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.MarginLeft = n.Scale(G.Scale));
        _marginRight = new(MeasurementMarginPosition.Undefined, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.MarginRight = n.Scale(G.Scale));
        _paddingTop = new(MeasurementPadding.Undefined, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.PaddingTop = n.Scale(G.Scale));
        _paddingBottom = new(MeasurementPadding.Undefined, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.PaddingBottom = n.Scale(G.Scale));
        _paddingLeft = new(MeasurementPadding.Undefined, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.PaddingLeft = n.Scale(G.Scale));
        _paddingRight = new(MeasurementPadding.Undefined, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.PaddingRight = n.Scale(G.Scale));
        _borderTop = new(null, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.BorderTop = n?.Value * G.Scale ?? YG.YGUndefined);
        _borderBottom = new(null, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.BorderBottom = n?.Value * G.Scale ?? YG.YGUndefined);
        _borderLeft = new(null, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.BorderLeft = n?.Value * G.Scale ?? YG.YGUndefined);
        _borderRight = new(null, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.BorderRight = n?.Value * G.Scale ?? YG.YGUndefined);
        _gapColumn = new(MeasurementGap.Undefined, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.GapColumn = n);
        _gapRow = new(MeasurementGap.Undefined, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.GapRow = n);
        _width = new(MeasurementWidthHeight.Undefined, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.Width = n.Scale(G.Scale));
        _height = new(MeasurementWidthHeight.Undefined, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.Height = n.Scale(G.Scale));
        _minWidth = new(MeasurementWidthHeight.Undefined, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.MinWidth = n.Scale(G.Scale));
        _minHeight = new(MeasurementWidthHeight.Undefined, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.MinHeight = n.Scale(G.Scale));
        _maxWidth = new(MeasurementWidthHeight.Undefined, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.MaxWidth = n.Scale(G.Scale));
        _maxHeight = new(MeasurementWidthHeight.Undefined, this, static (ctx, o, n) => ((Node)ctx!).NodeInternal.MaxHeight = n.Scale(G.Scale));
        _aspectRatio = new(null, this, (PropertyChangedHandler<Pixels?>)(static (ctx, o, n) => ((Node)ctx!).NodeInternal.AspectRatio = n?.Value ?? float.NaN));
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
            if (oldStyleSheetValue.Opacity is not null) _opacity.ClearStyleValue();
            if (oldStyleSheetValue.Direction is not null) _direction.ClearStyleValue();
            if (oldStyleSheetValue.FlexDirection is not null) _flexDirection.ClearStyleValue();
            if (oldStyleSheetValue.JustifyContent is not null) _justifyContent.ClearStyleValue();
            if (oldStyleSheetValue.AlignItems is not null) _alignItems.ClearStyleValue();
            if (oldStyleSheetValue.AlignSelf is not null) _alignSelf.ClearStyleValue();
            if (oldStyleSheetValue.AlignContent is not null) _alignContent.ClearStyleValue();
            if (oldStyleSheetValue.Position is not null) _position.ClearStyleValue();
            if (oldStyleSheetValue.FlexWrap is not null) _flexWrap.ClearStyleValue();
            if (oldStyleSheetValue.Overflow is not null) _overflow.ClearStyleValue();
            if (oldStyleSheetValue.Display is not null) _display.ClearStyleValue();
            if (oldStyleSheetValue.Flex is not null) _flex.ClearStyleValue();
            if (oldStyleSheetValue.FlexGrow is not null) _flexGrow.ClearStyleValue();
            if (oldStyleSheetValue.FlexShrink is not null) _flexShrink.ClearStyleValue();
            if (oldStyleSheetValue.FlexBasis is not null) _flexBasis.ClearStyleValue();
            if (oldStyleSheetValue.Left is not null) _left.ClearStyleValue();
            if (oldStyleSheetValue.Top is not null) _top.ClearStyleValue();
            if (oldStyleSheetValue.Right is not null) _right.ClearStyleValue();
            if (oldStyleSheetValue.Bottom is not null) _bottom.ClearStyleValue();
            if (oldStyleSheetValue.Margin is not null) { _marginTop.ClearStyleValue(); _marginBottom.ClearStyleValue(); _marginLeft.ClearStyleValue(); _marginRight.ClearStyleValue(); }
            if (oldStyleSheetValue.MarginTop is not null) _marginTop.ClearStyleValue();
            if (oldStyleSheetValue.MarginBottom is not null) _marginBottom.ClearStyleValue();
            if (oldStyleSheetValue.MarginLeft is not null) _marginLeft.ClearStyleValue();
            if (oldStyleSheetValue.MarginRight is not null) _marginRight.ClearStyleValue();
            if (oldStyleSheetValue.Padding is not null) { _paddingTop.ClearStyleValue(); _paddingBottom.ClearStyleValue(); _paddingLeft.ClearStyleValue(); _paddingRight.ClearStyleValue(); }
            if (oldStyleSheetValue.PaddingTop is not null) _paddingTop.ClearStyleValue();
            if (oldStyleSheetValue.PaddingBottom is not null) _paddingBottom.ClearStyleValue();
            if (oldStyleSheetValue.PaddingLeft is not null) _paddingLeft.ClearStyleValue();
            if (oldStyleSheetValue.PaddingRight is not null) _paddingRight.ClearStyleValue();
            if (oldStyleSheetValue.Border is not null) { _borderTop.ClearStyleValue(); _borderBottom.ClearStyleValue(); _borderLeft.ClearStyleValue(); _borderRight.ClearStyleValue(); }
            if (oldStyleSheetValue.BorderTop is not null) _borderTop.ClearStyleValue();
            if (oldStyleSheetValue.BorderBottom is not null) _borderBottom.ClearStyleValue();
            if (oldStyleSheetValue.BorderLeft is not null) _borderLeft.ClearStyleValue();
            if (oldStyleSheetValue.BorderRight is not null) _borderRight.ClearStyleValue();
            if (oldStyleSheetValue.Gap is not null) { _gapColumn.ClearStyleValue(); _gapRow.ClearStyleValue(); }
            if (oldStyleSheetValue.GapColumn is not null) _gapColumn.ClearStyleValue();
            if (oldStyleSheetValue.GapRow is not null) _gapRow.ClearStyleValue();
            if (oldStyleSheetValue.BoxSizing is not null) _boxSizing.ClearStyleValue();
            if (oldStyleSheetValue.Width is not null) _width.ClearStyleValue();
            if (oldStyleSheetValue.Height is not null) _height.ClearStyleValue();
            if (oldStyleSheetValue.MinWidth is not null) _minWidth.ClearStyleValue();
            if (oldStyleSheetValue.MinHeight is not null) _minHeight.ClearStyleValue();
            if (oldStyleSheetValue.MaxWidth is not null) _maxWidth.ClearStyleValue();
            if (oldStyleSheetValue.MaxHeight is not null) _maxHeight.ClearStyleValue();
            if (oldStyleSheetValue.AspectRatio is not null) _aspectRatio.ClearStyleValue();
        }
        
        if (newStyleSheet is { } newStyleSheetValue)
        {
            if (newStyleSheetValue.Visibility is { } visibility) Visibility = visibility;
            if (newStyleSheetValue.Opacity is { } opacity) _opacity.SetStyleValue(opacity);
            if (newStyleSheetValue.Direction is { } direction) _direction.SetStyleValue(direction);
            if (newStyleSheetValue.FlexDirection is { } flexDirection) _flexDirection.SetStyleValue(flexDirection);
            if (newStyleSheetValue.JustifyContent is { } justifyContent) _justifyContent.SetStyleValue(justifyContent);
            if (newStyleSheetValue.AlignItems is { } alignItems) _alignItems.SetStyleValue(alignItems);
            if (newStyleSheetValue.AlignSelf is { } alignSelf) _alignSelf.SetStyleValue(alignSelf);
            if (newStyleSheetValue.AlignContent is { } alignContent) _alignContent.SetStyleValue(alignContent);
            if (newStyleSheetValue.Position is { } position) _position.SetStyleValue(position);
            if (newStyleSheetValue.FlexWrap is { } flexWrap) _flexWrap.SetStyleValue(flexWrap);
            if (newStyleSheetValue.Overflow is { } overflow) _overflow.SetStyleValue(overflow);
            if (newStyleSheetValue.Display is { } display) _display.SetStyleValue(display);
            if (newStyleSheetValue.Flex is { } flex) _flex.SetStyleValue(flex);
            if (newStyleSheetValue.FlexGrow is { } flexGrow) _flexGrow.SetStyleValue(flexGrow);
            if (newStyleSheetValue.FlexShrink is { } flexShrink) _flexShrink.SetStyleValue(flexShrink);
            if (newStyleSheetValue.FlexBasis is { } flexBasis) _flexBasis.SetStyleValue(flexBasis);
            if (newStyleSheetValue.Left is { } left) _left.SetStyleValue(left);
            if (newStyleSheetValue.Top is { } top) _top.SetStyleValue(top);
            if (newStyleSheetValue.Right is { } right) _right.SetStyleValue(right);
            if (newStyleSheetValue.Bottom is { } bottom) _bottom.SetStyleValue(bottom);
            if (newStyleSheetValue.Margin is { } margin) { _marginTop.SetStyleValue(margin.Top); _marginBottom.SetStyleValue(margin.Bottom); _marginLeft.SetStyleValue(margin.Left); _marginRight.SetStyleValue(margin.Right); }
            if (newStyleSheetValue.MarginTop is { } marginTop) _marginTop.SetStyleValue(marginTop);
            if (newStyleSheetValue.MarginBottom is { } marginBottom) _marginBottom.SetStyleValue(marginBottom);
            if (newStyleSheetValue.MarginLeft is { } marginLeft) _marginLeft.SetStyleValue(marginLeft);
            if (newStyleSheetValue.MarginRight is { } marginRight) _marginRight.SetStyleValue(marginRight);
            if (newStyleSheetValue.Padding is { } padding) { _paddingTop.SetStyleValue(padding.Top); _paddingBottom.SetStyleValue(padding.Bottom); _paddingLeft.SetStyleValue(padding.Left); _paddingRight.SetStyleValue(padding.Right); }
            if (newStyleSheetValue.PaddingTop is { } paddingTop) _paddingTop.SetStyleValue(paddingTop);
            if (newStyleSheetValue.PaddingBottom is { } paddingBottom) _paddingBottom.SetStyleValue(paddingBottom);
            if (newStyleSheetValue.PaddingLeft is { } paddingLeft) _paddingLeft.SetStyleValue(paddingLeft);
            if (newStyleSheetValue.PaddingRight is { } paddingRight) _paddingRight.SetStyleValue(paddingRight);
            if (newStyleSheetValue.Border is { } border) { _borderTop.SetStyleValue(border.Top); _borderBottom.SetStyleValue(border.Bottom); _borderLeft.SetStyleValue(border.Left); _borderRight.SetStyleValue(border.Right); }
            if (newStyleSheetValue.BorderTop is { } borderTop) _borderTop.SetStyleValue(borderTop);
            if (newStyleSheetValue.BorderBottom is { } borderBottom) _borderBottom.SetStyleValue(borderBottom);
            if (newStyleSheetValue.BorderLeft is { } borderLeft) _borderLeft.SetStyleValue(borderLeft);
            if (newStyleSheetValue.BorderRight is { } borderRight) _borderRight.SetStyleValue(borderRight);
            if (newStyleSheetValue.Gap is { } gap) { _gapColumn.SetStyleValue(gap); _gapRow.SetStyleValue(gap); }
            if (newStyleSheetValue.GapColumn is { } gapColumn) _gapColumn.SetStyleValue(gapColumn);
            if (newStyleSheetValue.GapRow is { } gapRow) _gapRow.SetStyleValue(gapRow);
            if (newStyleSheetValue.BoxSizing is { } boxSizing) _boxSizing.SetStyleValue(boxSizing);
            if (newStyleSheetValue.Width is { } width) _width.SetStyleValue(width);
            if (newStyleSheetValue.Height is { } height) _height.SetStyleValue(height);
            if (newStyleSheetValue.MinWidth is { } minWidth) _minWidth.SetStyleValue(minWidth);
            if (newStyleSheetValue.MinHeight is { } minHeight) _minHeight.SetStyleValue(minHeight);
            if (newStyleSheetValue.MaxWidth is { } maxWidth) _maxWidth.SetStyleValue(maxWidth);
            if (newStyleSheetValue.MaxHeight is { } maxHeight) _maxHeight.SetStyleValue(maxHeight);
            if (newStyleSheetValue.AspectRatio is { } aspectRatio) _aspectRatio.SetStyleValue(aspectRatio);
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

    internal Property<float> _opacity;
    internal Property<Direction> _direction;
    internal Property<FlexDirection> _flexDirection;
    internal Property<Justify> _justifyContent;
    internal Property<Align> _alignItems;
    internal Property<Align> _alignSelf;
    internal Property<Align> _alignContent;
    internal Property<Position> _position;
    internal Property<Wrap> _flexWrap;
    internal Property<Overflow> _overflow;
    internal Property<Display> _display;
    internal Property<BoxSizing> _boxSizing;
    internal Property<float?> _flex;
    internal Property<float?> _flexGrow;
    internal Property<float?> _flexShrink;
    internal Property<MeasurementFlexBasis> _flexBasis;
    internal Property<MeasurementMarginPosition> _left;
    internal Property<MeasurementMarginPosition> _top;
    internal Property<MeasurementMarginPosition> _right;
    internal Property<MeasurementMarginPosition> _bottom;
    internal Property<MeasurementMarginPosition> _marginTop;
    internal Property<MeasurementMarginPosition> _marginBottom;
    internal Property<MeasurementMarginPosition> _marginLeft;
    internal Property<MeasurementMarginPosition> _marginRight;
    internal Property<MeasurementPadding> _paddingTop;
    internal Property<MeasurementPadding> _paddingBottom;
    internal Property<MeasurementPadding> _paddingLeft;
    internal Property<MeasurementPadding> _paddingRight;
    internal Property<Pixels?> _borderTop;
    internal Property<Pixels?> _borderBottom;
    internal Property<Pixels?> _borderLeft;
    internal Property<Pixels?> _borderRight;
    internal Property<MeasurementGap> _gapColumn;
    internal Property<MeasurementGap> _gapRow;
    internal Property<MeasurementWidthHeight> _width;
    internal Property<MeasurementWidthHeight> _height;
    internal Property<MeasurementWidthHeight> _minWidth;
    internal Property<MeasurementWidthHeight> _minHeight;
    internal Property<MeasurementWidthHeight> _maxWidth;
    internal Property<MeasurementWidthHeight> _maxHeight;
    internal Property<Pixels?> _aspectRatio;

    /// <summary>
    /// CSS: opacity - Sets the transparency level (0.0 = fully transparent, 1.0 = fully opaque)
    /// </summary>
    public float Opacity
    {
        get => _opacity.ComputedValue;
        set => _opacity.SetOverrideValue(value);
    }

    // https://css-tricks.com/snippets/css/a-guide-to-flexbox/
    /// <summary>
    /// CSS: direction - Establishes the main-axis (ltr/rtl/inherit)
    /// </summary>
    public Direction Direction
    {
        get => _direction.ComputedValue;
        set => _direction.SetOverrideValue(value);
    }

    /// <summary>
    /// CSS: flex-direction - Establishes the main-axis (row/column/row-reverse/column-reverse)
    /// </summary>
    public FlexDirection FlexDirection
    {
        get => _flexDirection.ComputedValue;
        set => _flexDirection.SetOverrideValue(value);
    }

    /// <summary>
    /// CSS: justify-content - Defines alignment along the main axis
    /// </summary>
    public Justify JustifyContent
    {
        get => _justifyContent.ComputedValue;
        set => _justifyContent.SetOverrideValue(value);
    }

    /// <summary>
    /// CSS: align-items - Defines default alignment for all children along the cross axis
    /// </summary>
    public Align AlignItems
    {
        get => _alignItems.ComputedValue;
        set => _alignItems.SetOverrideValue(value);
    }

    /// <summary>
    /// CSS: align-self - Allows a child to override the default cross-axis alignment
    /// </summary>
    public Align AlignSelf
    {
        get => _alignSelf.ComputedValue;
        set => _alignSelf.SetOverrideValue(value);
    }

    /// <summary>
    /// CSS: align-content - Aligns flex container's lines when there is extra space in the cross-axis
    /// </summary>
    public Align AlignContent
    {
        get => _alignContent.ComputedValue;
        set => _alignContent.SetOverrideValue(value);
    }

    /// <summary>
    /// CSS: position - Sets how an element is positioned (static/relative/absolute/fixed)
    /// </summary>
    public Position Position
    {
        get => _position.ComputedValue;
        set => _position.SetOverrideValue(value);
    }

    /// <summary>
    /// CSS: flex-wrap - Controls whether flex items wrap onto multiple lines (nowrap/wrap/wrap-reverse)
    /// </summary>
    public Wrap FlexWrap
    {
        get => _flexWrap.ComputedValue;
        set => _flexWrap.SetOverrideValue(value);
    }

    /// <summary>
    /// CSS: overflow - Controls what happens to content that is too big to fit (visible/hidden/scroll)
    /// </summary>
    public Overflow Overflow
    {
        get => _overflow.ComputedValue;
        set => _overflow.SetOverrideValue(value);
    }

    /// <summary>
    /// CSS: display - Defines the display type of the element (flex/none/block)
    /// </summary>
    public Display Display
    {
        get => _display.ComputedValue;
        set => _display.SetOverrideValue(value);
    }

    /// <summary>
    /// CSS: flex - Shorthand for flex-grow, flex-shrink, and flex-basis combined
    /// </summary>
    public float? Flex
    {
        get => _flex.ComputedValue;
        set => _flex.SetOverrideValue(value);
    }

    /// <summary>
    /// CSS: flex-grow - Defines the ability for a flex item to grow if necessary
    /// </summary>
    public float? FlexGrow
    {
        get => _flexGrow.ComputedValue;
        set => _flexGrow.SetOverrideValue(value);
    }

    /// <summary>
    /// CSS: flex-shrink - Defines the ability for a flex item to shrink if necessary
    /// </summary>
    public float? FlexShrink
    {
        get => _flexShrink.ComputedValue;
        set => _flexShrink.SetOverrideValue(value);
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
    public MeasurementFlexBasis FlexBasis    {        get => _flexBasis.ComputedValue;        set => _flexBasis.SetOverrideValue(value);    }
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
    public MeasurementMarginPosition Left    {        get => _left.ComputedValue;        set => _left.SetOverrideValue(value);    }
    /// <summary>
    /// CSS: top - Specifies the top position of a positioned element
    /// </summary>
    public MeasurementMarginPosition Top    {        get => _top.ComputedValue;        set => _top.SetOverrideValue(value);    }
    /// <summary>
    /// CSS: right - Specifies the right position of a positioned element
    /// </summary>
    public MeasurementMarginPosition Right    {        get => _right.ComputedValue;        set => _right.SetOverrideValue(value);    }
    /// <summary>
    /// CSS: bottom - Specifies the bottom position of a positioned element
    /// </summary>
    public MeasurementMarginPosition Bottom    {        get => _bottom.ComputedValue;        set => _bottom.SetOverrideValue(value);    }
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
            _marginLeft.SetOverrideValue(value.Left);
            _marginRight.SetOverrideValue(value.Right);
            _marginTop.SetOverrideValue(value.Top);
            _marginBottom.SetOverrideValue(value.Bottom);
        }
    }

    /// <summary>
    /// CSS: margin-top - Sets the top margin space outside the element
    /// </summary>
    public MeasurementMarginPosition MarginTop    {        get => _marginTop.ComputedValue;        set => _marginTop.SetOverrideValue(value);    }
    /// <summary>
    /// CSS: margin-bottom - Sets the bottom margin space outside the element
    /// </summary>
    public MeasurementMarginPosition MarginBottom    {        get => _marginBottom.ComputedValue;        set => _marginBottom.SetOverrideValue(value);    }
    /// <summary>
    /// CSS: margin-left - Sets the left margin space outside the element
    /// </summary>
    public MeasurementMarginPosition MarginLeft    {        get => _marginLeft.ComputedValue;        set => _marginLeft.SetOverrideValue(value);    }
    /// <summary>
    /// CSS: margin-right - Sets the right margin space outside the element
    /// </summary>
    public MeasurementMarginPosition MarginRight    {        get => _marginRight.ComputedValue;        set => _marginRight.SetOverrideValue(value);    }
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
            _paddingLeft.SetOverrideValue(value.Left);
            _paddingRight.SetOverrideValue(value.Right);
            _paddingTop.SetOverrideValue(value.Top);
            _paddingBottom.SetOverrideValue(value.Bottom);
        }
    }

    /// <summary>
    /// CSS: padding-top - Sets the top padding space inside the element
    /// </summary>
    public MeasurementPadding PaddingTop    {        get => _paddingTop.ComputedValue;        set => _paddingTop.SetOverrideValue(value);    }
    /// <summary>
    /// CSS: padding-bottom - Sets the bottom padding space inside the element
    /// </summary>
    public MeasurementPadding PaddingBottom    {        get => _paddingBottom.ComputedValue;        set => _paddingBottom.SetOverrideValue(value);    }
    /// <summary>
    /// CSS: padding-left - Sets the left padding space inside the element
    /// </summary>
    public MeasurementPadding PaddingLeft    {        get => _paddingLeft.ComputedValue;        set => _paddingLeft.SetOverrideValue(value);    }
    /// <summary>
    /// CSS: padding-right - Sets the right padding space inside the element
    /// </summary>
    public MeasurementPadding PaddingRight    {        get => _paddingRight.ComputedValue;        set => _paddingRight.SetOverrideValue(value);    }
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
            _borderLeft.SetOverrideValue(value.Left);
            _borderRight.SetOverrideValue(value.Right);
            _borderTop.SetOverrideValue(value.Top);
            _borderBottom.SetOverrideValue(value.Bottom);
        }
    }

    /// <summary>
    /// CSS: border-top-width - Sets the width of the top border
    /// </summary>
    public Pixels? BorderTop    {        get => _borderTop.ComputedValue;        set => _borderTop.SetOverrideValue(value);    }
    /// <summary>
    /// CSS: border-bottom-width - Sets the width of the bottom border
    /// </summary>
    public Pixels? BorderBottom    {        get => _borderBottom.ComputedValue;        set => _borderBottom.SetOverrideValue(value);    }
    /// <summary>
    /// CSS: border-left-width - Sets the width of the left border
    /// </summary>
    public Pixels? BorderLeft    {        get => _borderLeft.ComputedValue;        set => _borderLeft.SetOverrideValue(value);    }
    /// <summary>
    /// CSS: border-right-width - Sets the width of the right border
    /// </summary>
    public Pixels? BorderRight    {        get => _borderRight.ComputedValue;        set => _borderRight.SetOverrideValue(value);    }
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
            _gapColumn.SetOverrideValue(value);
            _gapRow.SetOverrideValue(value);
        }
    }

    /// <summary>
    /// CSS: column-gap - Sets the gap between columns in a flex container
    /// </summary>
    public MeasurementGap GapColumn    {        get => _gapColumn.ComputedValue;        set => _gapColumn.SetOverrideValue(value);    }
    /// <summary>
    /// CSS: row-gap - Sets the gap between rows in a flex container
    /// </summary>
    public MeasurementGap GapRow    {        get => _gapRow.ComputedValue;        set => _gapRow.SetOverrideValue(value);    }
    /// <summary>
    /// CSS: box-sizing - Defines how width/height calculations include padding/border (content-box/border-box)
    /// </summary>
    public BoxSizing BoxSizing    {        get => _boxSizing.ComputedValue;        set => _boxSizing.SetOverrideValue(value);    }
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
    public MeasurementWidthHeight Width    {        get => _width.ComputedValue;        set => _width.SetOverrideValue(value);    }
    /// <summary>
    /// CSS: height - Sets the height of the element
    /// </summary>
    public MeasurementWidthHeight Height    {        get => _height.ComputedValue;        set => _height.SetOverrideValue(value);    }
    /// <summary>
    /// CSS: min-width - Sets the minimum width of the element
    /// </summary>
    public MeasurementWidthHeight MinWidth    {        get => _minWidth.ComputedValue;        set => _minWidth.SetOverrideValue(value);    }
    /// <summary>
    /// CSS: min-height - Sets the minimum height of the element
    /// </summary>
    public MeasurementWidthHeight MinHeight    {        get => _minHeight.ComputedValue;        set => _minHeight.SetOverrideValue(value);    }
    /// <summary>
    /// CSS: max-width - Sets the maximum width of the element
    /// </summary>
    public MeasurementWidthHeight MaxWidth    {        get => _maxWidth.ComputedValue;        set => _maxWidth.SetOverrideValue(value);    }
    /// <summary>
    /// CSS: max-height - Sets the maximum height of the element
    /// </summary>
    public MeasurementWidthHeight MaxHeight    {        get => _maxHeight.ComputedValue;        set => _maxHeight.SetOverrideValue(value);    }
    /// <summary>
    /// CSS: aspect-ratio - Sets the preferred aspect ratio for the element (width / height)
    /// </summary>
    public Pixels? AspectRatio    {        get => _aspectRatio.ComputedValue;        set => _aspectRatio.SetOverrideValue(value);    }
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
            // Re-trigger all size-related onChanged handlers so they re-scale with new G.Scale
#pragma warning disable CA2245
            NodeInternal.Width = _width.ComputedValue.Scale(G.Scale);
            NodeInternal.Height = _height.ComputedValue.Scale(G.Scale);
            NodeInternal.MinWidth = _minWidth.ComputedValue.Scale(G.Scale);
            NodeInternal.MinHeight = _minHeight.ComputedValue.Scale(G.Scale);
            NodeInternal.MaxWidth = _maxWidth.ComputedValue.Scale(G.Scale);
            NodeInternal.MaxHeight = _maxHeight.ComputedValue.Scale(G.Scale);
            NodeInternal.MarginTop = _marginTop.ComputedValue.Scale(G.Scale);
            NodeInternal.MarginBottom = _marginBottom.ComputedValue.Scale(G.Scale);
            NodeInternal.MarginLeft = _marginLeft.ComputedValue.Scale(G.Scale);
            NodeInternal.MarginRight = _marginRight.ComputedValue.Scale(G.Scale);
            NodeInternal.PaddingTop = _paddingTop.ComputedValue.Scale(G.Scale);
            NodeInternal.PaddingBottom = _paddingBottom.ComputedValue.Scale(G.Scale);
            NodeInternal.PaddingLeft = _paddingLeft.ComputedValue.Scale(G.Scale);
            NodeInternal.PaddingRight = _paddingRight.ComputedValue.Scale(G.Scale);
            NodeInternal.BorderTop = _borderTop.ComputedValue?.Value * G.Scale ?? YG.YGUndefined;
            NodeInternal.BorderBottom = _borderBottom.ComputedValue?.Value * G.Scale ?? YG.YGUndefined;
            NodeInternal.BorderLeft = _borderLeft.ComputedValue?.Value * G.Scale ?? YG.YGUndefined;
            NodeInternal.BorderRight = _borderRight.ComputedValue?.Value * G.Scale ?? YG.YGUndefined;
            NodeInternal.GapColumn = _gapColumn.ComputedValue;
            NodeInternal.GapRow = _gapRow.ComputedValue;
            NodeInternal.FlexBasis = _flexBasis.ComputedValue.Scale(G.Scale);
            NodeInternal.Left = _left.ComputedValue.Scale(G.Scale);
            NodeInternal.Top = _top.ComputedValue.Scale(G.Scale);
            NodeInternal.Right = _right.ComputedValue.Scale(G.Scale);
            NodeInternal.Bottom = _bottom.ComputedValue.Scale(G.Scale);
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