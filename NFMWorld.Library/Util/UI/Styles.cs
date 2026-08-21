namespace NFMWorld.Reactor;

public struct Styles()
{
    /// <summary>
    /// CSS: visibility - Controls whether the element is visible (visible/hidden/collapsed)
    /// </summary>
    public Visibility Visibility = Visibility.Visible;

    /// <summary>
    /// CSS: opacity - Sets the transparency level (0.0 = fully transparent, 1.0 = fully opaque)
    /// </summary>
    public float Opacity = 1.0f;
    
    // https://css-tricks.com/snippets/css/a-guide-to-flexbox/
    /// <summary>
    /// CSS: direction - Establishes the main-axis (ltr/rtl/inherit)
    /// </summary>
    public Direction Direction = Direction.Ltr;

    /// <summary>
    /// CSS: flex-direction - Establishes the main-axis (row/column/row-reverse/column-reverse)
    /// </summary>
    public FlexDirection FlexDirection = FlexDirection.Row;

    /// <summary>
    /// CSS: justify-content - Defines alignment along the main axis
    /// </summary>
    public Justify JustifyContent = Justify.FlexStart;

    /// <summary>
    /// CSS: align-items - Defines default alignment for all children along the cross axis
    /// </summary>
    public Align AlignItems = Align.Auto;

    /// <summary>
    /// CSS: align-self - Allows a child to override the default cross-axis alignment
    /// </summary>
    public Align AlignSelf = Align.Auto;

    /// <summary>
    /// CSS: align-content - Aligns flex container's lines when there is extra space in the cross-axis
    /// </summary>
    public Align AlignContent = Align.Auto;

    /// <summary>
    /// CSS: position - Sets how an element is positioned (static/relative/absolute/fixed)
    /// </summary>
    public Position Position = Position.Static;

    /// <summary>
    /// CSS: flex-wrap - Controls whether flex items wrap onto multiple lines (nowrap/wrap/wrap-reverse)
    /// </summary>
    public Wrap FlexWrap = Wrap.NoWrap;

    /// <summary>
    /// CSS: overflow - Controls what happens to content that is too big to fit (visible/hidden/scroll)
    /// </summary>
    public Overflow Overflow = Overflow.Visible;

    /// <summary>
    /// CSS: display - Defines the display type of the element (flex/none/block)
    /// </summary>
    public Display Display = Display.Flex;

    /// <summary>
    /// CSS: box-sizing - Defines how width/height calculations include padding/border (content-box/border-box)
    /// </summary>
    public BoxSizing BoxSizing = BoxSizing.BorderBox;

    /// <summary>
    /// CSS: flex - Shorthand for flex-grow, flex-shrink, and flex-basis combined
    /// </summary>
    public float? Flex;
    
    /// <summary>
    /// CSS: flex-grow - Defines the ability for a flex item to grow if necessary
    /// </summary>
    public float? FlexGrow;
    
    /// <summary>
    /// CSS: flex-shrink - Defines the ability for a flex item to shrink if necessary
    /// </summary>
    public float? FlexShrink;

    /// <summary>
    /// CSS: flex-basis - Defines the default size of an element before remaining space is distributed
    /// </summary>
    public MeasurementFlexBasis FlexBasis = MeasurementFlexBasis.Undefined;
    
    /// <summary>
    /// CSS: left - Specifies the left position of a positioned element
    /// </summary>
    public MeasurementMarginPosition Left = MeasurementMarginPosition.Undefined;

    /// <summary>
    /// CSS: top - Specifies the top position of a positioned element
    /// </summary>
    public MeasurementMarginPosition Top = MeasurementMarginPosition.Undefined;

    /// <summary>
    /// CSS: right - Specifies the right position of a positioned element
    /// </summary>
    public MeasurementMarginPosition Right = MeasurementMarginPosition.Undefined;

    /// <summary>
    /// CSS: bottom - Specifies the bottom position of a positioned element
    /// </summary>
    public MeasurementMarginPosition Bottom = MeasurementMarginPosition.Undefined;

    /// <summary>
    /// CSS: margin-top - Sets the top margin space outside the element
    /// </summary>
    public MeasurementMarginPosition MarginTop = MeasurementMarginPosition.Undefined;

    /// <summary>
    /// CSS: margin-bottom - Sets the bottom margin space outside the element
    /// </summary>
    public MeasurementMarginPosition MarginBottom = MeasurementMarginPosition.Undefined;

    /// <summary>
    /// CSS: margin-left - Sets the left margin space outside the element
    /// </summary>
    public MeasurementMarginPosition MarginLeft = MeasurementMarginPosition.Undefined;

    /// <summary>
    /// CSS: margin-right - Sets the right margin space outside the element
    /// </summary>
    public MeasurementMarginPosition MarginRight = MeasurementMarginPosition.Undefined;

    /// <summary>
    /// CSS: padding-top - Sets the top padding space inside the element
    /// </summary>
    public MeasurementPadding PaddingTop = MeasurementPadding.Undefined;

    /// <summary>
    /// CSS: padding-bottom - Sets the bottom padding space inside the element
    /// </summary>
    public MeasurementPadding PaddingBottom = MeasurementPadding.Undefined;

    /// <summary>
    /// CSS: padding-left - Sets the left padding space inside the element
    /// </summary>
    public MeasurementPadding PaddingLeft = MeasurementPadding.Undefined;

    /// <summary>
    /// CSS: padding-right - Sets the right padding space inside the element
    /// </summary>
    public MeasurementPadding PaddingRight = MeasurementPadding.Undefined;

    /// <summary>
    /// CSS: border-top-width - Sets the width of the top border
    /// </summary>
    public Pixels? BorderTop;

    /// <summary>
    /// CSS: border-bottom-width - Sets the width of the bottom border
    /// </summary>
    public Pixels? BorderBottom;

    /// <summary>
    /// CSS: border-left-width - Sets the width of the left border
    /// </summary>
    public Pixels? BorderLeft;

    /// <summary>
    /// CSS: border-right-width - Sets the width of the right border
    /// </summary>
    public Pixels? BorderRight;
    
    /// <summary>
    /// CSS: column-gap - Sets the gap between columns in a flex container
    /// </summary>
    public MeasurementGap GapColumn = MeasurementGap.Undefined;

    /// <summary>
    /// CSS: row-gap - Sets the gap between rows in a flex container
    /// </summary>
    public MeasurementGap GapRow = MeasurementGap.Undefined;
    
    /// <summary>
    /// CSS: width - Sets the width of the element
    /// </summary>
    public MeasurementWidthHeight Width = MeasurementWidthHeight.Undefined;

    /// <summary>
    /// CSS: height - Sets the height of the element
    /// </summary>
    public MeasurementWidthHeight Height = MeasurementWidthHeight.Undefined;

    /// <summary>
    /// CSS: min-width - Sets the minimum width of the element
    /// </summary>
    public MeasurementWidthHeight MinWidth = MeasurementWidthHeight.Undefined;

    /// <summary>
    /// CSS: min-height - Sets the minimum height of the element
    /// </summary>
    public MeasurementWidthHeight MinHeight = MeasurementWidthHeight.Undefined;

    /// <summary>
    /// CSS: max-width - Sets the maximum width of the element
    /// </summary>
    public MeasurementWidthHeight MaxWidth = MeasurementWidthHeight.Undefined;

    /// <summary>
    /// CSS: max-height - Sets the maximum height of the element
    /// </summary>
    public MeasurementWidthHeight MaxHeight = MeasurementWidthHeight.Undefined;

    /// <summary>
    /// CSS: aspect-ratio - Sets the preferred aspect ratio for the element (width / height)
    /// </summary>
    public Pixels? AspectRatio;
    
    public Color? BorderColor = null;
    public Color? BackgroundColor = null;
    public float BorderTopLeftRadius = 0f;
    public float BorderTopRightRadius = 0f;
    public float BorderBottomLeftRadius = 0f;
    public float BorderBottomRightRadius = 0f;
}