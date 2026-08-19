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
        Assert.AreEqual(255f, t.textColor.r);
        Assert.AreEqual(0f, t.textColor.g);
        Assert.AreEqual(0f, t.textColor.b);
    }

    [TestMethod]
    public void Color_Hex_SetsTextColor()
    {
        var t = new ClayTextElement();
        t.SetProperty("color", "#00ff00");
        Assert.AreEqual(0f, t.textColor.r);
        Assert.AreEqual(255f, t.textColor.g);
        Assert.AreEqual(0f, t.textColor.b);
    }

    [TestMethod]
    public void FontSize_SetsFontSize()
    {
        var t = new ClayTextElement();
        t.SetProperty("font-size", "16px");
        Assert.AreEqual((ushort)16, t.fontSize);
    }

    [TestMethod]
    public void LetterSpacing_SetsLetterSpacing()
    {
        var t = new ClayTextElement();
        t.SetProperty("letter-spacing", "2px");
        Assert.AreEqual((ushort)2, t.letterSpacing);
    }

    [TestMethod]
    public void LineHeight_SetsLineHeight()
    {
        var t = new ClayTextElement();
        t.SetProperty("line-height", "24px");
        Assert.AreEqual((ushort)24, t.lineHeight);
    }

    [TestMethod]
    public void FontId_SetsFontId()
    {
        var t = new ClayTextElement();
        t.SetProperty("font-id", "3");
        Assert.AreEqual((ushort)3, t.fontId);
    }

    [TestMethod]
    public void TextAlign_Center()
    {
        var t = new ClayTextElement();
        t.SetProperty("text-align", "center");
        Assert.AreEqual(Clay_TextAlignment.CLAY_TEXT_ALIGN_CENTER, t.textAlignment);
    }

    [TestMethod]
    public void TextAlign_Left_And_Right()
    {
        var t = new ClayTextElement();
        t.SetProperty("text-align", "left");
        Assert.AreEqual(Clay_TextAlignment.CLAY_TEXT_ALIGN_LEFT, t.textAlignment);
        t.SetProperty("text-align", "right");
        Assert.AreEqual(Clay_TextAlignment.CLAY_TEXT_ALIGN_RIGHT, t.textAlignment);
    }

    [TestMethod]
    public void WhiteSpace_Nowrap_SetsWrapNone()
    {
        var t = new ClayTextElement();
        t.SetProperty("white-space", "nowrap");
        Assert.AreEqual(Clay_TextElementConfigWrapMode.CLAY_TEXT_WRAP_NONE, t.wrapMode);
    }

    [TestMethod]
    public void WhiteSpace_Pre_SetsWrapNewlines()
    {
        var t = new ClayTextElement();
        t.SetProperty("white-space", "pre");
        Assert.AreEqual(Clay_TextElementConfigWrapMode.CLAY_TEXT_WRAP_NEWLINES, t.wrapMode);
    }

    [TestMethod]
    public void WhiteSpace_Normal_SetsWrapWords()
    {
        var t = new ClayTextElement();
        t.SetProperty("white-space", "normal");
        Assert.AreEqual(Clay_TextElementConfigWrapMode.CLAY_TEXT_WRAP_WORDS, t.wrapMode);
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
