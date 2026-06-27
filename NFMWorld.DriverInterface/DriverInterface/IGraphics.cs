using Microsoft.Xna.Framework;

namespace NFMWorld.DriverInterface;

public interface IGraphics
{
    void SetLinearGradient(int x, int y, int width, int height, Color[] colors, float[]? colorPos);
    void SetColor(Color c);
    void FillPolygon(ReadOnlySpan<int> x, ReadOnlySpan<int> y, int n);
    void DrawPolygon(ReadOnlySpan<int> x, ReadOnlySpan<int> y, int n);
    void FillRect(int x1, int y1, int width, int height);
    void FillRoundedRect(int x1, int y1, int width, int height, float radTopLeft, float radTopRight,
        float radBottomRight, float radBottomLeft)
    {
        FillRect(x1, y1, width, height);
    }
    void DrawLine(int x1, int y1, int x2, int y2);
    new float Alpha { set; }
    void DrawImage(IImage image, int x, int y);
    void SetFont(Font font);
    IFontMetrics GetFontMetrics();
    IFontMetrics GetFontMetrics(Font font);
    void DrawString(ReadOnlySpan<char> text, int x, int y);
    void DrawStringAligned(ReadOnlySpan<char> text, int x, int y, int areaWidth, int areaHeight, TextHorizontalAlignment hAlign, TextVerticalAlignment vAlign);
    void DrawStringStrokeAligned(ReadOnlySpan<char> text, int x, int y, int areaWidth, int areaHeight, TextHorizontalAlignment hAlign, TextVerticalAlignment vAlign, int effectAmount = 1);
    void DrawStringStroke(ReadOnlySpan<char> text, int x, int y, int effectAmount = 1)
    {
    }
    void FillOval(int x, int y, int width, int height);
    void DrawOval(int x, int y, int width, int height);
    void DrawRoundedRect(int x1, int y1, int width, int height, float radTopLeft, float radTopRight,
        float radBottomRight, float radBottomLeft)
    {
        DrawRect(x1, y1, width, height);
    }
    void DrawRect(int x1, int y1, int width, int height);
    void DrawImage(IImage image, int x, int y, int width, int height);

    void SetAntialiasing(bool useAntialias)
    {
        // empty
    }

    void SetStrokeWidth(float width = 1f)
    {
        // empty
    }
}