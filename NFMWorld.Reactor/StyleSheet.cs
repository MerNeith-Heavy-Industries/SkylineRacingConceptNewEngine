using Microsoft.Xna.Framework;
using NFMWorld.DriverInterface;

namespace NFMWorld.Reactor;

public struct StyleSheet
{
    #region Node
    
    /// <summary>
    /// CSS: visibility - Controls whether the element is visible (visible/hidden/collapsed)
    /// </summary>
    public Visibility? Visibility { get; set; }
    
    /// <summary>
    /// CSS: opacity - Sets the transparency level (0.0 = fully transparent, 1.0 = fully opaque)
    /// </summary>
    public float? Opacity { get; set; }
    
    // https://css-tricks.com/snippets/css/a-guide-to-flexbox/
    /// <summary>
    /// CSS: direction - Establishes the main-axis (ltr/rtl/inherit)
    /// </summary>
    public Direction? Direction { get; set; }

    /// <summary>
    /// CSS: flex-direction - Establishes the main-axis (row/column/row-reverse/column-reverse)
    /// </summary>
    public FlexDirection? FlexDirection { get; set; }

    /// <summary>
    /// CSS: justify-content - Defines alignment along the main axis
    /// </summary>
    public Justify? JustifyContent { get; set; }

    /// <summary>
    /// CSS: align-items - Defines default alignment for all children along the cross axis
    /// </summary>
    public Align? AlignItems { get; set; }

    /// <summary>
    /// CSS: align-self - Allows a child to override the default cross-axis alignment
    /// </summary>
    public Align? AlignSelf { get; set; }

    /// <summary>
    /// CSS: align-content - Aligns flex container's lines when there is extra space in the cross-axis
    /// </summary>
    public Align? AlignContent { get; set; }

    /// <summary>
    /// CSS: position - Sets how an element is positioned (static/relative/absolute/fixed)
    /// </summary>
    public Position? Position { get; set; }

    /// <summary>
    /// CSS: flex-wrap - Controls whether flex items wrap onto multiple lines (nowrap/wrap/wrap-reverse)
    /// </summary>
    public Wrap? FlexWrap { get; set; }

    /// <summary>
    /// CSS: overflow - Controls what happens to content that is too big to fit (visible/hidden/scroll)
    /// </summary>
    public Overflow? Overflow { get; set; }

    /// <summary>
    /// CSS: display - Defines the display type of the element (flex/none/block)
    /// </summary>
    public Display? Display { get; set; }

    /// <summary>
    /// CSS: flex - Shorthand for flex-grow, flex-shrink, and flex-basis combined
    /// </summary>
    public float? Flex { get; set; }

    /// <summary>
    /// CSS: flex-grow - Defines the ability for a flex item to grow if necessary
    /// </summary>
    public float? FlexGrow { get; set; }

    /// <summary>
    /// CSS: flex-shrink - Defines the ability for a flex item to shrink if necessary
    /// </summary>
    public float? FlexShrink { get; set; }

    /// <summary>
    /// CSS: flex-basis - Defines the default size of an element before remaining space is distributed
    /// </summary>
    public Node.MeasurementFlexBasis? FlexBasis { get; set; }

    /// <summary>
    /// CSS: left - Specifies the left position of a positioned element
    /// </summary>
    public Node.MeasurementMarginPosition? Left { get; set; }

    /// <summary>
    /// CSS: top - Specifies the top position of a positioned element
    /// </summary>
    public Node.MeasurementMarginPosition? Top { get; set; }

    /// <summary>
    /// CSS: right - Specifies the right position of a positioned element
    /// </summary>
    public Node.MeasurementMarginPosition? Right { get; set; }

    /// <summary>
    /// CSS: bottom - Specifies the bottom position of a positioned element
    /// </summary>
    public Node.MeasurementMarginPosition? Bottom { get; set; }

    /// <summary>
    /// CSS: margin - Shorthand for setting all margin values (top, right, bottom, left)
    /// </summary>
    public Node.MeasurementMultiMargin? Margin { get; set; }

    /// <summary>
    /// CSS: margin-top - Sets the top margin space outside the element
    /// </summary>
    public Node.MeasurementMarginPosition? MarginTop { get; set; }

    /// <summary>
    /// CSS: margin-bottom - Sets the bottom margin space outside the element
    /// </summary>
    public Node.MeasurementMarginPosition? MarginBottom { get; set; }

    /// <summary>
    /// CSS: margin-left - Sets the left margin space outside the element
    /// </summary>
    public Node.MeasurementMarginPosition? MarginLeft { get; set; }
    
    /// <summary>
    /// CSS: margin-right - Sets the right margin space outside the element
    /// </summary>
    public Node.MeasurementMarginPosition? MarginRight { get; set; }

    /// <summary>
    /// CSS: padding - Shorthand for setting all padding values (top, right, bottom, left)
    /// </summary>
    public Node.MeasurementMultiPadding? Padding { get; set; }

    /// <summary>
    /// CSS: padding-top - Sets the top padding space inside the element
    /// </summary>
    public Node.MeasurementPadding? PaddingTop { get; set; }

    /// <summary>
    /// CSS: padding-bottom - Sets the bottom padding space inside the element
    /// </summary>
    public Node.MeasurementPadding? PaddingBottom { get; set; }

    /// <summary>
    /// CSS: padding-left - Sets the left padding space inside the element
    /// </summary>
    public Node.MeasurementPadding? PaddingLeft { get; set; }

    /// <summary>
    /// CSS: padding-right - Sets the right padding space inside the element
    /// </summary>
    public Node.MeasurementPadding? PaddingRight { get; set; }

    /// <summary>
    /// CSS: border - Shorthand for setting all border widths
    /// </summary>
    public Node.MeasurementMultiBorder? Border { get; set; }

    /// <summary>
    /// CSS: border-top-width - Sets the width of the top border
    /// </summary>
    public Node.Pixels? BorderTop { get; set; }

    /// <summary>
    /// CSS: border-bottom-width - Sets the width of the bottom border
    /// </summary>
    public Node.Pixels? BorderBottom { get; set; }

    /// <summary>
    /// CSS: border-left-width - Sets the width of the left border
    /// </summary>
    public Node.Pixels? BorderLeft { get; set; }

    /// <summary>
    /// CSS: border-right-width - Sets the width of the right border
    /// </summary>
    public Node.Pixels? BorderRight { get; set; }

    /// <summary>
    /// CSS: gap - Shorthand for setting row-gap and column-gap
    /// </summary>
    public Node.MeasurementGap? Gap { get; set; }

    /// <summary>
    /// CSS: column-gap - Sets the gap between columns in a flex container
    /// </summary>
    public Node.MeasurementGap? GapColumn { get; set; }

    /// <summary>
    /// CSS: row-gap - Sets the gap between rows in a flex container
    /// </summary>
    public Node.MeasurementGap? GapRow { get; set; }

    /// <summary>
    /// CSS: box-sizing - Defines how width/height calculations include padding/border (content-box/border-box)
    /// </summary>
    public BoxSizing? BoxSizing { get; set; }

    /// <summary>
    /// CSS: width - Sets the width of the element
    /// </summary>
    public Node.MeasurementWidthHeight? Width { get; set; }

    /// <summary>
    /// CSS: height - Sets the height of the element
    /// </summary>
    public Node.MeasurementWidthHeight? Height { get; set; }

    /// <summary>
    /// CSS: min-width - Sets the minimum width of the element
    /// </summary>
    public Node.MeasurementWidthHeight? MinWidth { get; set; }

    /// <summary>
    /// CSS: min-height - Sets the minimum height of the element
    /// </summary>
    public Node.MeasurementWidthHeight? MinHeight { get; set; }

    /// <summary>
    /// CSS: max-width - Sets the maximum width of the element
    /// </summary>
    public Node.MeasurementWidthHeight? MaxWidth { get; set; }

    /// <summary>
    /// CSS: max-height - Sets the maximum height of the element
    /// </summary>
    public Node.MeasurementWidthHeight? MaxHeight { get; set; }

    /// <summary>
    /// CSS: aspect-ratio - Sets the preferred aspect ratio for the element (width / height)
    /// </summary>
    public Node.Pixels? AspectRatio { get; set; }
    
    #endregion

    #region PaintedBox
    
    public Color? BorderColor { get; set; }
    public Color? BackgroundColor { get; set; }

    public float? BorderRadius { get; set; }
    public float? BorderTopLeftRadius { get; set; }
    public float? BorderTopRightRadius { get; set; }
    public float? BorderBottomLeftRadius { get; set; }
    public float? BorderBottomRightRadius { get; set; }

    #endregion

    #region TextRun
    
    /// <summary>
    /// Sets the background color of the text.
    /// </summary>
    public Color? Background { get; set; }

    /// <summary>
    /// Sets the fill color of the text. The default value is white.
    /// </summary>
    public Color? Foreground { get; set; }
    
    /// <summary>
    /// Sets the stroke color of the text. Or set to null to disable the stroke.
    /// </summary>
    public Color? Stroke { get; set; }

    /// <summary>
    /// Gets or sets the font family.
    /// </summary>
    public FontFamily? FontFamily { get; set; }

    /// <summary>
    /// Gets or sets the font size.
    /// </summary>
    public float? FontSize { get; set; }

    /// <summary>
    /// Gets or sets the font style.
    /// </summary>
    public FontStyle? FontStyle { get; set; }

    public BreakType? BreakType { get; set; }

    public OverflowBehavior? OverflowBehavior { get; set; }

    /// <summary>
    /// Sets the horizontal alignment of the text. The default value is <see cref="TextHorizontalAlignment.Left"/>.
    /// </summary>
    public TextHorizontalAlignment? HorizontalAlignment { get; set; }

    /// <summary>
    /// Sets the vertical alignment of the text. The default value is <see cref="TextVerticalAlignment.Top"/>.
    /// </summary>
    public TextVerticalAlignment? VerticalAlignment { get; set; }
    
    #endregion
    
    /*
    Visibility? Visibility
    float? Opacity
    Direction? Direction
    FlexDirection? FlexDirection
    Justify? JustifyContent
    Align? AlignItems
    Align? AlignSelf
    Align? AlignContent
    Position? Position
    Wrap? FlexWrap
    Overflow? Overflow
    Display? Display
    float? Flex
    float? FlexGrow
    float? FlexShrink
    Node.MeasurementFlexBasis? FlexBasis
    Node.MeasurementMarginPosition? Left
    Node.MeasurementMarginPosition? Top
    Node.MeasurementMarginPosition? Right
    Node.MeasurementMarginPosition? Bottom
    Node.MeasurementMultiMargin? Margin
    Node.MeasurementMarginPosition? MarginTop
    Node.MeasurementMarginPosition? MarginBottom
    Node.MeasurementMarginPosition? MarginLeft
    Node.MeasurementMarginPosition? MarginRight
    Node.MeasurementMultiPadding? Padding
    Node.MeasurementPadding? PaddingTop
    Node.MeasurementPadding? PaddingBottom
    Node.MeasurementPadding? PaddingLeft
    Node.MeasurementPadding? PaddingRight
    Node.MeasurementMultiBorder? Border
    Node.Pixels? BorderTop
    Node.Pixels? BorderBottom
    Node.Pixels? BorderLeft
    Node.Pixels? BorderRight
    Node.MeasurementGap? Gap
    Node.MeasurementGap? GapColumn
    Node.MeasurementGap? GapRow
    BoxSizing? BoxSizing
    Node.MeasurementWidthHeight? Width
    Node.MeasurementWidthHeight? Height
    Node.MeasurementWidthHeight? MinWidth
    Node.MeasurementWidthHeight? MinHeight
    Node.MeasurementWidthHeight? MaxWidth
    Node.MeasurementWidthHeight? MaxHeight
    Node.Pixels? AspectRatio
    Color? BorderColor
    Color? BackgroundColor
    float? BorderRadius
    float? BorderTopLeftRadius
    float? BorderTopRightRadius
    float? BorderBottomLeftRadius
    float? BorderBottomRightRadius
    Color? Background
    Color? Foreground
    Color? Stroke
    FontFamily? FontFamily
    float? FontSize
    FontStyle? FontStyle
    BreakType? BreakType
    OverflowBehavior? OverflowBehavior
    TextHorizontalAlignment? HorizontalAlignment
    TextVerticalAlignment? VerticalAlignment
    
     */

    public static implicit operator StyleSheet(ReadOnlySpan<StyleSheet> styleSheets) => StyleSheet.Merge(styleSheets);

    private static StyleSheet Merge(params ReadOnlySpan<StyleSheet> styleSheets)
    {
        var styleSheet = new StyleSheet();

        foreach (var sheet in styleSheets)
        {
            #region Node

            if (sheet.Visibility is {} visibility) styleSheet.Visibility = visibility;
            if (sheet.Opacity is {} opacity) styleSheet.Opacity = opacity;
            if (sheet.Direction is {} direction) styleSheet.Direction = direction;
            if (sheet.FlexDirection is {} flexDirection) styleSheet.FlexDirection = flexDirection;
            if (sheet.JustifyContent is {} justifyContent) styleSheet.JustifyContent = justifyContent;
            if (sheet.AlignItems is {} alignItems) styleSheet.AlignItems = alignItems;
            if (sheet.AlignSelf is {} alignSelf) styleSheet.AlignSelf = alignSelf;
            if (sheet.AlignContent is {} alignContent) styleSheet.AlignContent = alignContent;
            if (sheet.Position is {} position) styleSheet.Position = position;
            if (sheet.FlexWrap is {} flexWrap) styleSheet.FlexWrap = flexWrap;
            if (sheet.Overflow is {} overflow) styleSheet.Overflow = overflow;
            if (sheet.Display is {} display) styleSheet.Display = display;
            if (sheet.Flex is {} flex) styleSheet.Flex = flex;
            if (sheet.FlexGrow is {} flexGrow) styleSheet.FlexGrow = flexGrow;
            if (sheet.FlexShrink is {} flexShrink) styleSheet.FlexShrink = flexShrink;
            if (sheet.FlexBasis is {} flexBasis) styleSheet.FlexBasis = flexBasis;
            if (sheet.Left is {} left) styleSheet.Left = left;
            if (sheet.Top is {} top) styleSheet.Top = top;
            if (sheet.Right is {} right) styleSheet.Right = right;
            if (sheet.Bottom is {} bottom) styleSheet.Bottom = bottom;
            if (sheet.Margin is {} margin) styleSheet.Margin = margin;
            if (sheet.MarginTop is {} marginTop) styleSheet.MarginTop = marginTop;
            if (sheet.MarginBottom is {} marginBottom) styleSheet.MarginBottom = marginBottom;
            if (sheet.MarginLeft is {} marginLeft) styleSheet.MarginLeft = marginLeft;
            if (sheet.MarginRight is {} marginRight) styleSheet.MarginRight = marginRight;
            if (sheet.Padding is {} padding) styleSheet.Padding = padding;
            if (sheet.PaddingTop is {} paddingTop) styleSheet.PaddingTop = paddingTop;
            if (sheet.PaddingBottom is {} paddingBottom) styleSheet.PaddingBottom = paddingBottom;
            if (sheet.PaddingLeft is {} paddingLeft) styleSheet.PaddingLeft = paddingLeft;
            if (sheet.PaddingRight is {} paddingRight) styleSheet.PaddingRight = paddingRight;
            if (sheet.Border is {} border) styleSheet.Border = border;
            if (sheet.BorderTop is {} borderTop) styleSheet.BorderTop = borderTop;
            if (sheet.BorderBottom is {} borderBottom) styleSheet.BorderBottom = borderBottom;
            if (sheet.BorderLeft is {} borderLeft) styleSheet.BorderLeft = borderLeft;
            if (sheet.BorderRight is {} borderRight) styleSheet.BorderRight = borderRight;
            if (sheet.Gap is {} gap) styleSheet.Gap = gap;
            if (sheet.GapColumn is {} gapColumn) styleSheet.GapColumn = gapColumn;
            if (sheet.GapRow is {} gapRow) styleSheet.GapRow = gapRow;
            if (sheet.BoxSizing is {} boxSizing) styleSheet.BoxSizing = boxSizing;
            if (sheet.Width is {} width) styleSheet.Width = width;
            if (sheet.Height is {} height) styleSheet.Height = height;
            if (sheet.MinWidth is {} minWidth) styleSheet.MinWidth = minWidth;
            if (sheet.MinHeight is {} minHeight) styleSheet.MinHeight = minHeight;
            if (sheet.MaxWidth is {} maxWidth) styleSheet.MaxWidth = maxWidth;
            if (sheet.MaxHeight is {} maxHeight) styleSheet.MaxHeight = maxHeight;
            if (sheet.AspectRatio is {} aspectRatio) styleSheet.AspectRatio = aspectRatio;
            
            #endregion

            #region PaintedBox

            if (sheet.BorderColor is {} borderColor) styleSheet.BorderColor = borderColor;
            if (sheet.BackgroundColor is {} backgroundColor) styleSheet.BackgroundColor = backgroundColor;
            if (sheet.BorderRadius is {} borderRadius) styleSheet.BorderRadius = borderRadius;
            if (sheet.BorderTopLeftRadius is {} borderTopLeftRadius) styleSheet.BorderTopLeftRadius = borderTopLeftRadius;
            if (sheet.BorderTopRightRadius is {} borderTopRightRadius) styleSheet.BorderTopRightRadius = borderTopRightRadius;
            if (sheet.BorderBottomLeftRadius is {} borderBottomLeftRadius) styleSheet.BorderBottomLeftRadius = borderBottomLeftRadius;
            if (sheet.BorderBottomRightRadius is {} borderBottomRightRadius) styleSheet.BorderBottomRightRadius = borderBottomRightRadius;

            #endregion

            #region TextRun

            if (sheet.Background is {} background) styleSheet.Background = background;
            if (sheet.Foreground is {} foreground) styleSheet.Foreground = foreground;
            if (sheet.Stroke is {} stroke) styleSheet.Stroke = stroke;
            if (sheet.FontFamily is {} fontFamily) styleSheet.FontFamily = fontFamily;
            if (sheet.FontSize is {} fontSize) styleSheet.FontSize = fontSize;
            if (sheet.FontStyle is {} fontStyle) styleSheet.FontStyle = fontStyle;
            if (sheet.BreakType is {} breakType) styleSheet.BreakType = breakType;
            if (sheet.OverflowBehavior is {} overflowBehavior) styleSheet.OverflowBehavior = overflowBehavior;
            if (sheet.HorizontalAlignment is {} horizontalAlignment) styleSheet.HorizontalAlignment = horizontalAlignment;
            if (sheet.VerticalAlignment is {} verticalAlignment) styleSheet.VerticalAlignment = verticalAlignment;

            #endregion
        }

        return styleSheet;
    }
}