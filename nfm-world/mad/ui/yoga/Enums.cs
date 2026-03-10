namespace nfm_world.ui.yoga;

public enum YgDirection
{
    Inherit,
    Ltr,
    Rtl,
}

public enum YgFlexDirection
{
    Column,
    ColumnReverse,
    Row,
    RowReverse,
}

public enum YgJustify
{
    FlexStart,
    Center,
    FlexEnd,
    SpaceBetween,
    SpaceAround,
    SpaceEvenly,
}

public enum YgAlign
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

public enum YgPositionType
{
    Static,
    Relative,
    Absolute,
}

public enum YgWrap
{
    NoWrap,
    Wrap,
    WrapReverse,
}

public enum YgOverflow
{
    Visible,
    Hidden,
    Scroll,
}

public enum YgDisplay
{
    Flex,
    None,
    Contents,
}

public enum YgBoxSizing
{
    BorderBox,
    ContentBox,
}
public enum YgNodeType
{
    Default,
    Text,
}

public enum Visibility
{
    Hidden,
    Visible
}

public enum YgUnit
{
    Undefined,
    Point,
    Percent,
    Auto,
    MaxContent,
    FitContent,
    Stretch,
}

// Implicit conversions between these enums and Yoga-CS enums
public static class Conversions
{
    public static Yoga.YGDirection ToYogaDirection(this YgDirection d) => (Yoga.YGDirection)d;
    public static YgDirection ToNfmDirection(this Yoga.YGDirection d) => (YgDirection)d;
    public static Yoga.YGFlexDirection ToYogaFlexDirection(this YgFlexDirection d) => (Yoga.YGFlexDirection)d;
    public static YgFlexDirection ToNfmFlexDirection(this Yoga.YGFlexDirection d) => (YgFlexDirection)d;
    public static Yoga.YGJustify ToYogaJustify(this YgJustify j) => (Yoga.YGJustify)j;
    public static YgJustify ToNfmJustify(this Yoga.YGJustify j) => (YgJustify)j;
    public static Yoga.YGAlign ToYogaAlign(this YgAlign a) => (Yoga.YGAlign)a;
    public static YgAlign ToNfmAlign(this Yoga.YGAlign a) => (YgAlign)a;
    public static Yoga.YGPositionType ToYogaPositionType(this YgPositionType p) => (Yoga.YGPositionType)p;
    public static YgPositionType ToNfmPositionType(this Yoga.YGPositionType p) => (YgPositionType)p;
    public static Yoga.YGWrap ToYogaWrap(this YgWrap w) => (Yoga.YGWrap)w;
    public static YgWrap ToNfmWrap(this Yoga.YGWrap w) => (YgWrap)w;
    public static Yoga.YGOverflow ToYogaOverflow(this YgOverflow o) => (Yoga.YGOverflow)o;
    public static YgOverflow ToNfmOverflow(this Yoga.YGOverflow o) => (YgOverflow)o;
    public static Yoga.YGDisplay ToYogaDisplay(this YgDisplay d) => (Yoga.YGDisplay)d;
    public static YgDisplay ToNfmDisplay(this Yoga.YGDisplay d) => (YgDisplay)d;
    public static Yoga.YGBoxSizing ToYogaBoxSizing(this YgBoxSizing b) => (Yoga.YGBoxSizing)b;
    public static YgBoxSizing ToNfmBoxSizing(this Yoga.YGBoxSizing b) => (YgBoxSizing)b;
    public static Yoga.YGNodeType ToYogaNodeType(this YgNodeType n) => (Yoga.YGNodeType)n;
    public static YgNodeType ToNfmNodeType(this Yoga.YGNodeType n) => (YgNodeType)n;
    public static Yoga.YGUnit ToYogaUnit(this YgUnit u) => (Yoga.YGUnit)u;
    public static YgUnit ToNfmUnit(this Yoga.YGUnit u) => (YgUnit)u;
}