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
        e.SetProperty("Width", "120px");
        Assert.AreEqual(Clay.SizingType.Fixed, e.Layout.Sizing.Width.Type);
        Assert.AreEqual(120f, e.Layout.Sizing.Width.MinMax.Min);
    }

    [TestMethod]
    public void Width_Percent_SetsPercentSizing()
    {
        var e = new ClayElement();
        e.SetProperty("Width", "50%");
        Assert.AreEqual(Clay.SizingType.Percent, e.Layout.Sizing.Width.Type);
        Assert.AreEqual(0.5f, e.Layout.Sizing.Width.Percent, 0.0001f);
    }

    [TestMethod]
    public void Width_Auto_SetsFitSizing()
    {
        var e = new ClayElement();
        e.SetProperty("Width", "auto");
        Assert.AreEqual(Clay.SizingType.Fit, e.Layout.Sizing.Width.Type);
    }

    [TestMethod]
    public void Width_Grow_SetsGrowSizing()
    {
        var e = new ClayElement();
        e.SetProperty("Width", "grow");
        Assert.AreEqual(Clay.SizingType.Grow, e.Layout.Sizing.Width.Type);
    }

    [TestMethod]
    public void MinMaxWidth_SetsMinMax()
    {
        var e = new ClayElement();
        e.SetProperty("Min-Width", "10px");
        e.SetProperty("max-Width", "200");
        Assert.AreEqual(10f, e.Layout.Sizing.Width.MinMax.Min);
        Assert.AreEqual(200f, e.Layout.Sizing.Width.MinMax.Max);
    }

    [TestMethod]
    public void Padding_FourValues_SetsAllSides()
    {
        var e = new ClayElement();
        e.SetProperty("Padding", "1px 2px 3px 4px");
        Assert.AreEqual((ushort)1, e.Layout.Padding.Top);
        Assert.AreEqual((ushort)2, e.Layout.Padding.Right);
        Assert.AreEqual((ushort)3, e.Layout.Padding.Bottom);
        Assert.AreEqual((ushort)4, e.Layout.Padding.Left);
    }

    [TestMethod]
    public void Padding_TwoValues_VerticalHorizontal()
    {
        var e = new ClayElement();
        e.SetProperty("Padding", "5px 10px");
        Assert.AreEqual((ushort)5, e.Layout.Padding.Top);
        Assert.AreEqual((ushort)10, e.Layout.Padding.Right);
        Assert.AreEqual((ushort)5, e.Layout.Padding.Bottom);
        Assert.AreEqual((ushort)10, e.Layout.Padding.Left);
    }

    [TestMethod]
    public void Padding_Longhand_SetsSingleSide()
    {
        var e = new ClayElement();
        e.SetProperty("Padding-left", "7px");
        Assert.AreEqual((ushort)7, e.Layout.Padding.Left);
    }

    [TestMethod]
    public void Gap_SetsChildGap()
    {
        var e = new ClayElement();
        e.SetProperty("gap", "8px");
        Assert.AreEqual((ushort)8, e.Layout.ChildGap);
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
        Assert.AreEqual(Clay.LayoutDirection.LeftToRight, e.Layout.LayoutDirection);
        e.SetProperty("flex-direction", "column");
        Assert.AreEqual(Clay.LayoutDirection.TopToBottom, e.Layout.LayoutDirection);
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
        Assert.AreEqual(Clay.LayoutAlignmentY.Center, e.Layout.ChildAlignment.Y);
    }

    [TestMethod]
    public void JustifyContent_Column_SetsYAxis()
    {
        var e = new ClayElement();
        e.SetProperty("flex-direction", "column");
        e.SetProperty("justify-content", "flex-end");
        Assert.AreEqual(Clay.LayoutAlignmentY.Bottom, e.Layout.ChildAlignment.Y);
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
        e.SetProperty("background-Color", "#ff8000");
        Assert.AreEqual(255f, e.BackgroundColor.R);
        Assert.AreEqual(128f, e.BackgroundColor.G);
        Assert.AreEqual(0f, e.BackgroundColor.B);
        Assert.AreEqual(255f, e.BackgroundColor.A);
    }

    [TestMethod]
    public void BackgroundColor_Hex4_WithAlpha()
    {
        var e = new ClayElement();
        e.SetProperty("background-Color", "#f00f");
        Assert.AreEqual(255f, e.BackgroundColor.R);
        Assert.AreEqual(0f, e.BackgroundColor.G);
        Assert.AreEqual(0f, e.BackgroundColor.B);
        Assert.AreEqual(255f, e.BackgroundColor.A);
    }

    [TestMethod]
    public void BackgroundColor_Rgb()
    {
        var e = new ClayElement();
        e.SetProperty("background-Color", "rgb(10, 20, 30)");
        Assert.AreEqual(10f, e.BackgroundColor.R);
        Assert.AreEqual(20f, e.BackgroundColor.G);
        Assert.AreEqual(30f, e.BackgroundColor.B);
        Assert.AreEqual(255f, e.BackgroundColor.A);
    }

    [TestMethod]
    public void BackgroundColor_Rgba_AlphaFraction()
    {
        var e = new ClayElement();
        e.SetProperty("background-Color", "rgba(255, 0, 0, 0.5)");
        Assert.AreEqual(255f, e.BackgroundColor.R);
        Assert.AreEqual(0f, e.BackgroundColor.G);
        Assert.AreEqual(0f, e.BackgroundColor.B);
        Assert.AreEqual(127.5f, e.BackgroundColor.A, 0.01f);
    }

    [TestMethod]
    public void BackgroundColor_Named()
    {
        var e = new ClayElement();
        e.SetProperty("background-Color", "red");
        Assert.AreEqual(255f, e.BackgroundColor.R);
        Assert.AreEqual(0f, e.BackgroundColor.G);
        Assert.AreEqual(0f, e.BackgroundColor.B);
    }

    [TestMethod]
    public void BackgroundColor_Unknown_Throws()
    {
        var e = new ClayElement();
        Assert.Throws<ArgumentException>(() => e.SetProperty("background-Color", "notaColor"));
    }

    [TestMethod]
    public void Border_Shorthand_SetsWidthAndColor()
    {
        var e = new ClayElement();
        e.SetProperty("border", "2px solid #ff0000");
        Assert.AreEqual((ushort)2, e.Border.Width.Top);
        Assert.AreEqual(255f, e.Border.Color.R);
        Assert.AreEqual(0f, e.Border.Color.G);
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
        Assert.AreEqual(1f, e.CornerRadius.TopLeft);
        Assert.AreEqual(2f, e.CornerRadius.TopRight);
        Assert.AreEqual(3f, e.CornerRadius.BottomRight);
        Assert.AreEqual(4f, e.CornerRadius.BottomLeft);
    }

    [TestMethod]
    public void BorderTopLeftRadius_SetsCorner()
    {
        var e = new ClayElement();
        e.SetProperty("border-top-left-radius", "9px");
        Assert.AreEqual(9f, e.CornerRadius.TopLeft);
    }

    [TestMethod]
    public void Position_Absolute_SetsAttachToParent()
    {
        var e = new ClayElement();
        e.SetProperty("position", "absolute");
        Assert.AreEqual(Clay.FloatingAttachToElement.Parent, e.Floating.AttachTo);
    }

    [TestMethod]
    public void Position_Fixed_SetsAttachToRoot()
    {
        var e = new ClayElement();
        e.SetProperty("position", "fixed");
        Assert.AreEqual(Clay.FloatingAttachToElement.Root, e.Floating.AttachTo);
    }

    [TestMethod]
    public void Position_Static_DisablesFloating()
    {
        var e = new ClayElement();
        e.SetProperty("position", "static");
        Assert.AreEqual(Clay.FloatingAttachToElement.None, e.Floating.AttachTo);
    }

    [TestMethod]
    public void LeftTop_SetOffset_And_EnableFloating()
    {
        var e = new ClayElement();
        e.SetProperty("left", "10px");
        e.SetProperty("top", "5px");
        Assert.AreEqual(Clay.FloatingAttachToElement.Parent, e.Floating.AttachTo);
        Assert.AreEqual(10f, e.Floating.Offset.X);
        Assert.AreEqual(5f, e.Floating.Offset.Y);
    }

    [TestMethod]
    public void RightBottom_NegateOffset()
    {
        var e = new ClayElement();
        e.SetProperty("right", "10px");
        e.SetProperty("bottom", "5px");
        Assert.AreEqual(-10f, e.Floating.Offset.X);
        Assert.AreEqual(-5f, e.Floating.Offset.Y);
    }

    [TestMethod]
    public void ZIndex_SetsFloatingZIndex()
    {
        var e = new ClayElement();
        e.SetProperty("z-index", "42");
        Assert.AreEqual((short)42, e.Floating.ZIndex);
    }

    [TestMethod]
    public void AspectRatio_SetsRatio()
    {
        var e = new ClayElement();
        e.SetProperty("aspect-ratio", "16/9");
        Assert.AreEqual(16f / 9f, e.AspectRatio.AspectRatio, 0.0001f);
    }

    [TestMethod]
    public void Overflow_Hidden_ClipsBoth()
    {
        var e = new ClayElement();
        e.SetProperty("overflow", "hidden");
        Assert.IsTrue(e.Clip.Horizontal);
        Assert.IsTrue(e.Clip.Vertical);
    }

    [TestMethod]
    public void OverflowX_Hidden_ClipsHorizontalOnly()
    {
        var e = new ClayElement();
        e.SetProperty("overflow-x", "hidden");
        Assert.IsTrue(e.Clip.Horizontal);
        Assert.IsFalse(e.Clip.Vertical);
    }

    [TestMethod]
    public void TransitionDuration_Suffixes()
    {
        var e = new ClayElement();
        e.SetProperty("transition-duration", "300ms");
        Assert.AreEqual(0.3f, e.Transition.Duration, 0.0001f);
        e.SetProperty("transition-duration", "0.5s");
        Assert.AreEqual(0.5f, e.Transition.Duration, 0.0001f);
    }

    [TestMethod]
    public void TransitionProperty_WidthFlag()
    {
        var e = new ClayElement();
        e.SetProperty("transition-property", "Width");
        Assert.IsTrue(e.Transition.Properties.HasFlag(Clay.TransitionProperty.Width));
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
        Assert.AreEqual(Clay.SizingType.Fixed, e.Layout.Sizing.Width.Type);
    }
}
