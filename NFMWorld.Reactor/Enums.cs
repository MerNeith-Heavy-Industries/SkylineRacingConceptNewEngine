namespace WorldXaml.UI.Yoga;

public enum Direction
{
    Inherit,
    Ltr,
    Rtl,
}

public enum FlexDirection
{
    Column,
    ColumnReverse,
    Row,
    RowReverse,
}

public enum Justify
{
    FlexStart,
    Center,
    FlexEnd,
    SpaceBetween,
    SpaceAround,
    SpaceEvenly,
}

public enum Align
{
    Auto,
    FlexStart,
    Center,
    FlexEnd,
    Stretch,
    Baseline,
    SpaceBetween,
    SpaceAround,
    SpaceEvenly,
}

public enum Position
{
    Static,
    Relative,
    Absolute,
}

public enum Wrap
{
    NoWrap,
    Wrap,
    WrapReverse,
}

public enum Overflow
{
    Visible,
    Hidden,
    Scroll,
}

public enum Display
{
    Flex,
    None,
    Contents,
}

public enum BoxSizing
{
    BorderBox,
    ContentBox,
}
public enum NodeType
{
    Default,
    Text,
}

public enum Visibility
{
    Hidden,
    Visible
}

// Implicit conversions between these enums and Yoga-CS enums
public static class Conversions
{
    public static global::Yoga.YGDirection ToYogaDirection(this Direction d) => (global::Yoga.YGDirection)d;
    public static Direction ToNfmDirection(this global::Yoga.YGDirection d) => (Direction)d;
    public static global::Yoga.YGFlexDirection ToYogaFlexDirection(this FlexDirection d) => (global::Yoga.YGFlexDirection)d;
    public static FlexDirection ToNfmFlexDirection(this global::Yoga.YGFlexDirection d) => (FlexDirection)d;
    public static global::Yoga.YGJustify ToYogaJustify(this Justify j) => (global::Yoga.YGJustify)j;
    public static Justify ToNfmJustify(this global::Yoga.YGJustify j) => (Justify)j;
    public static global::Yoga.YGAlign ToYogaAlign(this Align a) => (global::Yoga.YGAlign)a;
    public static Align ToNfmAlign(this global::Yoga.YGAlign a) => (Align)a;
    public static global::Yoga.YGPositionType ToYogaPositionType(this Position p) => (global::Yoga.YGPositionType)p;
    public static Position ToNfmPositionType(this global::Yoga.YGPositionType p) => (Position)p;
    public static global::Yoga.YGWrap ToYogaWrap(this Wrap w) => (global::Yoga.YGWrap)w;
    public static Wrap ToNfmWrap(this global::Yoga.YGWrap w) => (Wrap)w;
    public static global::Yoga.YGOverflow ToYogaOverflow(this Overflow o) => (global::Yoga.YGOverflow)o;
    public static Overflow ToNfmOverflow(this global::Yoga.YGOverflow o) => (Overflow)o;
    public static global::Yoga.YGDisplay ToYogaDisplay(this Display d) => (global::Yoga.YGDisplay)d;
    public static Display ToNfmDisplay(this global::Yoga.YGDisplay d) => (Display)d;
    public static global::Yoga.YGBoxSizing ToYogaBoxSizing(this BoxSizing b) => (global::Yoga.YGBoxSizing)b;
    public static BoxSizing ToNfmBoxSizing(this global::Yoga.YGBoxSizing b) => (BoxSizing)b;
    public static global::Yoga.YGNodeType ToYogaNodeType(this NodeType n) => (global::Yoga.YGNodeType)n;
    public static NodeType ToNfmNodeType(this global::Yoga.YGNodeType n) => (NodeType)n;
}