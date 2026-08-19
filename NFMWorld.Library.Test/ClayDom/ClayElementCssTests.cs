using ClaySharp;
using NFMWorld.ClayDom;

namespace NFMWorld.Library.Test;

[TestClass]
public class ClayElementCssTests
{
    [TestMethod]
    public void Width_Pixels_SetsFixedSizing()
    {
        var e = new ClayElement();
        e.SetProperty("width", "120px");
        Assert.AreEqual(Clay__SizingType.CLAY__SIZING_TYPE_FIXED, e.layout.sizing.width.type);
        Assert.AreEqual(120f, e.layout.sizing.width.minMax.min);
    }

    [TestMethod]
    public void Width_Percent_SetsPercentSizing()
    {
        var e = new ClayElement();
        e.SetProperty("width", "50%");
        Assert.AreEqual(Clay__SizingType.CLAY__SIZING_TYPE_PERCENT, e.layout.sizing.width.type);
        Assert.AreEqual(0.5f, e.layout.sizing.width.percent, 0.0001f);
    }

    [TestMethod]
    public void Width_Auto_SetsFitSizing()
    {
        var e = new ClayElement();
        e.SetProperty("width", "auto");
        Assert.AreEqual(Clay__SizingType.CLAY__SIZING_TYPE_FIT, e.layout.sizing.width.type);
    }

    [TestMethod]
    public void Width_Grow_SetsGrowSizing()
    {
        var e = new ClayElement();
        e.SetProperty("width", "grow");
        Assert.AreEqual(Clay__SizingType.CLAY__SIZING_TYPE_GROW, e.layout.sizing.width.type);
    }

    [TestMethod]
    public void MinMaxWidth_SetsMinMax()
    {
        var e = new ClayElement();
        e.SetProperty("min-width", "10px");
        e.SetProperty("max-width", "200");
        Assert.AreEqual(10f, e.layout.sizing.width.minMax.min);
        Assert.AreEqual(200f, e.layout.sizing.width.minMax.max);
    }

    [TestMethod]
    public void Padding_FourValues_SetsAllSides()
    {
        var e = new ClayElement();
        e.SetProperty("padding", "1px 2px 3px 4px");
        Assert.AreEqual((ushort)1, e.layout.padding.top);
        Assert.AreEqual((ushort)2, e.layout.padding.right);
        Assert.AreEqual((ushort)3, e.layout.padding.bottom);
        Assert.AreEqual((ushort)4, e.layout.padding.left);
    }

    [TestMethod]
    public void Padding_TwoValues_VerticalHorizontal()
    {
        var e = new ClayElement();
        e.SetProperty("padding", "5px 10px");
        Assert.AreEqual((ushort)5, e.layout.padding.top);
        Assert.AreEqual((ushort)10, e.layout.padding.right);
        Assert.AreEqual((ushort)5, e.layout.padding.bottom);
        Assert.AreEqual((ushort)10, e.layout.padding.left);
    }

    [TestMethod]
    public void Padding_Longhand_SetsSingleSide()
    {
        var e = new ClayElement();
        e.SetProperty("padding-left", "7px");
        Assert.AreEqual((ushort)7, e.layout.padding.left);
    }

    [TestMethod]
    public void Gap_SetsChildGap()
    {
        var e = new ClayElement();
        e.SetProperty("gap", "8px");
        Assert.AreEqual((ushort)8, e.layout.childGap);
    }

    [TestMethod]
    public void Margin_Throws()
    {
        var e = new ClayElement();
        Assert.Throws<ArgumentException>(() => e.SetProperty("margin", "10px"));
    }

    [TestMethod]
    public void FlexDirection_Row_And_Column()
    {
        var e = new ClayElement();
        e.SetProperty("flex-direction", "row");
        Assert.AreEqual(Clay_LayoutDirection.CLAY_LEFT_TO_RIGHT, e.layout.layoutDirection);
        e.SetProperty("flex-direction", "column");
        Assert.AreEqual(Clay_LayoutDirection.CLAY_TOP_TO_BOTTOM, e.layout.layoutDirection);
    }

    [TestMethod]
    public void FlexDirection_Reverse_Throws()
    {
        var e = new ClayElement();
        Assert.Throws<ArgumentException>(() => e.SetProperty("flex-direction", "row-reverse"));
    }

    [TestMethod]
    public void AlignItems_Row_SetsYAxis()
    {
        var e = new ClayElement();
        e.SetProperty("flex-direction", "row");
        e.SetProperty("align-items", "center");
        Assert.AreEqual(Clay_LayoutAlignmentY.CLAY_ALIGN_Y_CENTER, e.layout.childAlignment.y);
    }

    [TestMethod]
    public void JustifyContent_Column_SetsYAxis()
    {
        var e = new ClayElement();
        e.SetProperty("flex-direction", "column");
        e.SetProperty("justify-content", "flex-end");
        Assert.AreEqual(Clay_LayoutAlignmentY.CLAY_ALIGN_Y_BOTTOM, e.layout.childAlignment.y);
    }

    [TestMethod]
    public void JustifyContent_SpaceBetween_Throws()
    {
        var e = new ClayElement();
        Assert.Throws<ArgumentException>(() => e.SetProperty("justify-content", "space-between"));
    }

    [TestMethod]
    public void BackgroundColor_Hex6()
    {
        var e = new ClayElement();
        e.SetProperty("background-color", "#ff8000");
        Assert.AreEqual(255f, e.backgroundColor.r);
        Assert.AreEqual(128f, e.backgroundColor.g);
        Assert.AreEqual(0f, e.backgroundColor.b);
        Assert.AreEqual(255f, e.backgroundColor.a);
    }

    [TestMethod]
    public void BackgroundColor_Hex4_WithAlpha()
    {
        var e = new ClayElement();
        e.SetProperty("background-color", "#f00f");
        Assert.AreEqual(255f, e.backgroundColor.r);
        Assert.AreEqual(0f, e.backgroundColor.g);
        Assert.AreEqual(0f, e.backgroundColor.b);
        Assert.AreEqual(255f, e.backgroundColor.a);
    }

    [TestMethod]
    public void BackgroundColor_Rgb()
    {
        var e = new ClayElement();
        e.SetProperty("background-color", "rgb(10, 20, 30)");
        Assert.AreEqual(10f, e.backgroundColor.r);
        Assert.AreEqual(20f, e.backgroundColor.g);
        Assert.AreEqual(30f, e.backgroundColor.b);
        Assert.AreEqual(255f, e.backgroundColor.a);
    }

    [TestMethod]
    public void BackgroundColor_Rgba_AlphaFraction()
    {
        var e = new ClayElement();
        e.SetProperty("background-color", "rgba(255, 0, 0, 0.5)");
        Assert.AreEqual(255f, e.backgroundColor.r);
        Assert.AreEqual(0f, e.backgroundColor.g);
        Assert.AreEqual(0f, e.backgroundColor.b);
        Assert.AreEqual(127.5f, e.backgroundColor.a, 0.01f);
    }

    [TestMethod]
    public void BackgroundColor_Named()
    {
        var e = new ClayElement();
        e.SetProperty("background-color", "red");
        Assert.AreEqual(255f, e.backgroundColor.r);
        Assert.AreEqual(0f, e.backgroundColor.g);
        Assert.AreEqual(0f, e.backgroundColor.b);
    }

    [TestMethod]
    public void BackgroundColor_Unknown_Throws()
    {
        var e = new ClayElement();
        Assert.Throws<ArgumentException>(() => e.SetProperty("background-color", "notacolor"));
    }

    [TestMethod]
    public void Border_Shorthand_SetsWidthAndColor()
    {
        var e = new ClayElement();
        e.SetProperty("border", "2px solid #ff0000");
        Assert.AreEqual((ushort)2, e.border.width.top);
        Assert.AreEqual(255f, e.border.color.r);
        Assert.AreEqual(0f, e.border.color.g);
    }

    [TestMethod]
    public void BorderStyle_Dashed_Throws()
    {
        var e = new ClayElement();
        Assert.Throws<ArgumentException>(() => e.SetProperty("border-style", "dashed"));
    }

    [TestMethod]
    public void BorderStyle_Solid_DoesNotThrow()
    {
        var e = new ClayElement();
        e.SetProperty("border-style", "solid");
    }

    [TestMethod]
    public void BorderRadius_FourValues_SetsCorners()
    {
        var e = new ClayElement();
        e.SetProperty("border-radius", "1px 2px 3px 4px");
        Assert.AreEqual(1f, e.cornerRadius.topLeft);
        Assert.AreEqual(2f, e.cornerRadius.topRight);
        Assert.AreEqual(3f, e.cornerRadius.bottomRight);
        Assert.AreEqual(4f, e.cornerRadius.bottomLeft);
    }

    [TestMethod]
    public void BorderTopLeftRadius_SetsCorner()
    {
        var e = new ClayElement();
        e.SetProperty("border-top-left-radius", "9px");
        Assert.AreEqual(9f, e.cornerRadius.topLeft);
    }

    [TestMethod]
    public void Position_Absolute_SetsAttachToParent()
    {
        var e = new ClayElement();
        e.SetProperty("position", "absolute");
        Assert.AreEqual(Clay_FloatingAttachToElement.CLAY_ATTACH_TO_PARENT, e.floating.attachTo);
    }

    [TestMethod]
    public void Position_Fixed_SetsAttachToRoot()
    {
        var e = new ClayElement();
        e.SetProperty("position", "fixed");
        Assert.AreEqual(Clay_FloatingAttachToElement.CLAY_ATTACH_TO_ROOT, e.floating.attachTo);
    }

    [TestMethod]
    public void Position_Static_DisablesFloating()
    {
        var e = new ClayElement();
        e.SetProperty("position", "static");
        Assert.AreEqual(Clay_FloatingAttachToElement.CLAY_ATTACH_TO_NONE, e.floating.attachTo);
    }

    [TestMethod]
    public void LeftTop_SetOffset_And_EnableFloating()
    {
        var e = new ClayElement();
        e.SetProperty("left", "10px");
        e.SetProperty("top", "5px");
        Assert.AreEqual(Clay_FloatingAttachToElement.CLAY_ATTACH_TO_PARENT, e.floating.attachTo);
        Assert.AreEqual(10f, e.floating.offset.X);
        Assert.AreEqual(5f, e.floating.offset.Y);
    }

    [TestMethod]
    public void RightBottom_NegateOffset()
    {
        var e = new ClayElement();
        e.SetProperty("right", "10px");
        e.SetProperty("bottom", "5px");
        Assert.AreEqual(-10f, e.floating.offset.X);
        Assert.AreEqual(-5f, e.floating.offset.Y);
    }

    [TestMethod]
    public void ZIndex_SetsFloatingZIndex()
    {
        var e = new ClayElement();
        e.SetProperty("z-index", "42");
        Assert.AreEqual((short)42, e.floating.zIndex);
    }

    [TestMethod]
    public void AspectRatio_SetsRatio()
    {
        var e = new ClayElement();
        e.SetProperty("aspect-ratio", "16/9");
        Assert.AreEqual(16f / 9f, e.aspectRatio.aspectRatio, 0.0001f);
    }

    [TestMethod]
    public void Overflow_Hidden_ClipsBoth()
    {
        var e = new ClayElement();
        e.SetProperty("overflow", "hidden");
        Assert.IsTrue(e.clip.horizontal);
        Assert.IsTrue(e.clip.vertical);
    }

    [TestMethod]
    public void OverflowX_Hidden_ClipsHorizontalOnly()
    {
        var e = new ClayElement();
        e.SetProperty("overflow-x", "hidden");
        Assert.IsTrue(e.clip.horizontal);
        Assert.IsFalse(e.clip.vertical);
    }

    [TestMethod]
    public void TransitionDuration_Suffixes()
    {
        var e = new ClayElement();
        e.SetProperty("transition-duration", "300ms");
        Assert.AreEqual(0.3f, e.transition.duration, 0.0001f);
        e.SetProperty("transition-duration", "0.5s");
        Assert.AreEqual(0.5f, e.transition.duration, 0.0001f);
    }

    [TestMethod]
    public void TransitionProperty_WidthFlag()
    {
        var e = new ClayElement();
        e.SetProperty("transition-property", "width");
        Assert.IsTrue(e.transition.properties.HasFlag(Clay_TransitionProperty.CLAY_TRANSITION_PROPERTY_WIDTH));
    }

    [TestMethod]
    public void UnknownProperty_Throws()
    {
        var e = new ClayElement();
        Assert.Throws<ArgumentException>(() => e.SetProperty("not-a-real-property", "x"));
    }

    [TestMethod]
    public void PropertyName_CaseInsensitive()
    {
        var e = new ClayElement();
        e.SetProperty("WIDTH", "10px");
        Assert.AreEqual(Clay__SizingType.CLAY__SIZING_TYPE_FIXED, e.layout.sizing.width.type);
    }
}
