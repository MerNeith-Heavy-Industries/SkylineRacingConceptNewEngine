using ClaySharp;
using NFMWorld.ClayDom;

namespace NFMWorld.Library.Test;

[TestClass]
public class ClayTextElementCssTests
{
    [TestMethod]
    public void Color_Named_SetsTextColor()
    {
        var t = new ClayTextElement();
        t.SetProperty("color", "red");
        Assert.AreEqual(255f, t.TextColor.R);
        Assert.AreEqual(0f, t.TextColor.G);
        Assert.AreEqual(0f, t.TextColor.B);
    }

    [TestMethod]
    public void Color_Hex_SetsTextColor()
    {
        var t = new ClayTextElement();
        t.SetProperty("color", "#00ff00");
        Assert.AreEqual(0f, t.TextColor.R);
        Assert.AreEqual(255f, t.TextColor.G);
        Assert.AreEqual(0f, t.TextColor.B);
    }

    [TestMethod]
    public void FontSize_SetsFontSize()
    {
        var t = new ClayTextElement();
        t.SetProperty("font-size", "16px");
        Assert.AreEqual((ushort)16, t.FontSize);
    }

    [TestMethod]
    public void LetterSpacing_SetsLetterSpacing()
    {
        var t = new ClayTextElement();
        t.SetProperty("letter-spacing", "2px");
        Assert.AreEqual((ushort)2, t.LetterSpacing);
    }

    [TestMethod]
    public void LineHeight_SetsLineHeight()
    {
        var t = new ClayTextElement();
        t.SetProperty("line-height", "24px");
        Assert.AreEqual((ushort)24, t.LineHeight);
    }

    [TestMethod]
    public void FontId_SetsFontId()
    {
        var t = new ClayTextElement();
        t.SetProperty("font-id", "3");
        Assert.AreEqual((ushort)3, t.FontId);
    }

    [TestMethod]
    public void TextAlign_Center()
    {
        var t = new ClayTextElement();
        t.SetProperty("text-align", "center");
        Assert.AreEqual(Clay.TextAlignment.Center, t.TextAlignment);
    }

    [TestMethod]
    public void TextAlign_Left_And_Right()
    {
        var t = new ClayTextElement();
        t.SetProperty("text-align", "left");
        Assert.AreEqual(Clay.TextAlignment.Left, t.TextAlignment);
        t.SetProperty("text-align", "right");
        Assert.AreEqual(Clay.TextAlignment.Right, t.TextAlignment);
    }

    [TestMethod]
    public void WhiteSpace_Nowrap_SetsWrapNone()
    {
        var t = new ClayTextElement();
        t.SetProperty("white-space", "nowrap");
        Assert.AreEqual(Clay.TextElementConfigWrapMode.None, t.WrapMode);
    }

    [TestMethod]
    public void WhiteSpace_Pre_SetsWrapNewlines()
    {
        var t = new ClayTextElement();
        t.SetProperty("white-space", "pre");
        Assert.AreEqual(Clay.TextElementConfigWrapMode.Newlines, t.WrapMode);
    }

    [TestMethod]
    public void WhiteSpace_Normal_SetsWrapWords()
    {
        var t = new ClayTextElement();
        t.SetProperty("white-space", "normal");
        Assert.AreEqual(Clay.TextElementConfigWrapMode.Words, t.WrapMode);
    }

    [TestMethod]
    public void UnknownProperty_Throws()
    {
        var t = new ClayTextElement();
        Assert.Throws<ArgumentException>(() => t.SetProperty("width", "10px"));
    }

    [TestMethod]
    public void TextAlign_Justify_Throws()
    {
        var t = new ClayTextElement();
        Assert.Throws<ArgumentException>(() => t.SetProperty("text-align", "justify"));
    }
}
