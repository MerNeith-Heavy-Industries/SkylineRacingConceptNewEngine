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
        t.SetStyleProperty("color", "red");
        Assert.AreEqual(255f, t.TextColor.R);
        Assert.AreEqual(0f, t.TextColor.G);
        Assert.AreEqual(0f, t.TextColor.B);
    }

    [TestMethod]
    public void Color_Hex_SetsTextColor()
    {
        var t = new ClayTextElement();
        t.SetStyleProperty("color", "#00ff00");
        Assert.AreEqual(0f, t.TextColor.R);
        Assert.AreEqual(255f, t.TextColor.G);
        Assert.AreEqual(0f, t.TextColor.B);
    }

    [TestMethod]
    public void FontSize_SetsFontSize()
    {
        var t = new ClayTextElement();
        t.SetStyleProperty("font-size", "16px");
        Assert.AreEqual((ushort)16, t.FontSize);
    }

    [TestMethod]
    public void LetterSpacing_SetsLetterSpacing()
    {
        var t = new ClayTextElement();
        t.SetStyleProperty("letter-spacing", "2px");
        Assert.AreEqual((ushort)2, t.LetterSpacing);
    }

    [TestMethod]
    public void LineHeight_SetsLineHeight()
    {
        var t = new ClayTextElement();
        t.SetStyleProperty("line-height", "24px");
        Assert.AreEqual((ushort)24, t.LineHeight);
    }

    [TestMethod]
    public void FontId_SetsFontId()
    {
        var t = new ClayTextElement();
        t.SetStyleProperty("font-id", "3");
        Assert.AreEqual((ushort)3, t.FontId);
    }

    [TestMethod]
    public void TextAlign_Center()
    {
        var t = new ClayTextElement();
        t.SetStyleProperty("text-align", "center");
        Assert.AreEqual(Clay.TextAlignment.Center, t.TextAlignment);
    }

    [TestMethod]
    public void TextAlign_Left_And_Right()
    {
        var t = new ClayTextElement();
        t.SetStyleProperty("text-align", "left");
        Assert.AreEqual(Clay.TextAlignment.Left, t.TextAlignment);
        t.SetStyleProperty("text-align", "right");
        Assert.AreEqual(Clay.TextAlignment.Right, t.TextAlignment);
    }

    [TestMethod]
    public void WhiteSpace_Nowrap_SetsWrapNone()
    {
        var t = new ClayTextElement();
        t.SetStyleProperty("white-space", "nowrap");
        Assert.AreEqual(Clay.TextElementConfigWrapMode.None, t.WrapMode);
    }

    [TestMethod]
    public void WhiteSpace_Pre_SetsWrapNewlines()
    {
        var t = new ClayTextElement();
        t.SetStyleProperty("white-space", "pre");
        Assert.AreEqual(Clay.TextElementConfigWrapMode.Newlines, t.WrapMode);
    }

    [TestMethod]
    public void WhiteSpace_Normal_SetsWrapWords()
    {
        var t = new ClayTextElement();
        t.SetStyleProperty("white-space", "normal");
        Assert.AreEqual(Clay.TextElementConfigWrapMode.Words, t.WrapMode);
    }

    [TestMethod]
    public void UnknownProperty_Throws()
    {
        var t = new ClayTextElement();
        Assert.Throws<ArgumentException>(() => t.SetStyleProperty("width", "10px"));
    }

    [TestMethod]
    public void TextAlign_Justify_Throws()
    {
        var t = new ClayTextElement();
        Assert.Throws<ArgumentException>(() => t.SetStyleProperty("text-align", "justify"));
    }
}
