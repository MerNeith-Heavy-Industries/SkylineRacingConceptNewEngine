using nfm_world_library.Lua;

namespace NFMWorld.Reactor;

[LuaVisible]
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
    public static Yoga.YGDirection ToYogaDirection(this Direction d) => (Yoga.YGDirection)d;
    public static Direction ToNfmDirection(this Yoga.YGDirection d) => (Direction)d;
    public static Yoga.YGFlexDirection ToYogaFlexDirection(this FlexDirection d) => (Yoga.YGFlexDirection)d;
    public static FlexDirection ToNfmFlexDirection(this Yoga.YGFlexDirection d) => (FlexDirection)d;
    public static Yoga.YGJustify ToYogaJustify(this Justify j) => (Yoga.YGJustify)j;
    public static Justify ToNfmJustify(this Yoga.YGJustify j) => (Justify)j;
    public static Yoga.YGAlign ToYogaAlign(this Align a) => (Yoga.YGAlign)a;
    public static Align ToNfmAlign(this Yoga.YGAlign a) => (Align)a;
    public static Yoga.YGPositionType ToYogaPositionType(this Position p) => (Yoga.YGPositionType)p;
    public static Position ToNfmPositionType(this Yoga.YGPositionType p) => (Position)p;
    public static Yoga.YGWrap ToYogaWrap(this Wrap w) => (Yoga.YGWrap)w;
    public static Wrap ToNfmWrap(this Yoga.YGWrap w) => (Wrap)w;
    public static Yoga.YGOverflow ToYogaOverflow(this Overflow o) => (Yoga.YGOverflow)o;
    public static Overflow ToNfmOverflow(this Yoga.YGOverflow o) => (Overflow)o;
    public static Yoga.YGDisplay ToYogaDisplay(this Display d) => (Yoga.YGDisplay)d;
    public static Display ToNfmDisplay(this Yoga.YGDisplay d) => (Display)d;
    public static Yoga.YGBoxSizing ToYogaBoxSizing(this BoxSizing b) => (Yoga.YGBoxSizing)b;
    public static BoxSizing ToNfmBoxSizing(this Yoga.YGBoxSizing b) => (BoxSizing)b;
    public static Yoga.YGNodeType ToYogaNodeType(this NodeType n) => (Yoga.YGNodeType)n;
    public static NodeType ToNfmNodeType(this Yoga.YGNodeType n) => (NodeType)n;
}