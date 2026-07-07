using System.Numerics;
using Microsoft.Xna.Framework;

namespace NFMWorld.DriverInterface;

public interface IGraphics
{
    Vector2 Viewport { get; }

    // Length proportional to radius of a cubic bezier handle for 90deg arcs
    private const float NVG_KAPPA90 = 0.5522847493f;
    
    IImage LoadImage(string file);
    IImage LoadImage(ReadOnlyMemory<byte> file);

    void SetLinearGradient(int x, int y, int width, int height, Color[] colors, float[]? colorPos);
    void SetColor(Color c);

    void FillPolygon(ReadOnlySpan<int> x, ReadOnlySpan<int> y, int n)
    {
        if (n < 1) return;
        BeginPath();
        MoveTo(x[0], y[0]);
        for (int i = 1; i < n; i++)
        {
            LineTo(x[i], y[i]);
        }
        ClosePath();
        Fill();
    }

    void DrawPolygon(ReadOnlySpan<int> x, ReadOnlySpan<int> y, int n)
    {
        if (n < 1) return;
        BeginPath();
        MoveTo(x[0], y[0]);
        for (int i = 1; i < n; i++)
        {
            LineTo(x[i], y[i]);
        }
        ClosePath();
        Stroke();
    }

    void FillRect(int x, int y, int width, int height)
    {
        BeginPath();
        Rect(x, y, width, height);
        Fill();
    }

    void Rect(int x, int y, int width, int height)
    {
        MoveTo(x, y);
        LineTo(x, y + height);
        LineTo(x + width, y + height);
        LineTo(x + width, y);
        ClosePath();
    }

    void FillRoundedRect(int x, int y, int width, int height, float radTopLeft, float radTopRight,
        float radBottomRight, float radBottomLeft)
    {
        BeginPath();
        RoundedRectVarying(x, y, width, height, radTopLeft, radTopRight, radBottomRight, radBottomLeft);
        Fill();
    }

    private void RoundedRectVarying(int x, int y, int width, int height, float radTopLeft, float radTopRight,
        float radBottomRight, float radBottomLeft)
    {
        if (radTopLeft < 0.1f && radTopRight < 0.1f && radBottomRight < 0.1f && radBottomLeft < 0.1f)
        {
            Rect(x, y, width, height);
        }
        else
        {
            var halfw = Math.Abs(width) * 0.5f;
            var halfh = Math.Abs(height) * 0.5f;
            var rxBL = Math.Min(radBottomLeft, halfw) * Math.Sign(width);
            var ryBL = Math.Min(radBottomLeft, halfh) * Math.Sign(height);
            var rxBR = Math.Min(radBottomRight, halfw) * Math.Sign(width);
            var ryBR = Math.Min(radBottomRight, halfh) * Math.Sign(height);
            var rxTR = Math.Min(radTopRight, halfw) * Math.Sign(width);
            var ryTR = Math.Min(radTopRight, halfh) * Math.Sign(height);
            var rxTL = Math.Min(radTopLeft, halfw) * Math.Sign(width);
            var ryTL = Math.Min(radTopLeft, halfh) * Math.Sign(height);

            MoveTo(x, y + ryTL);
            LineTo(x, y + height - ryBL);
            BezierTo(x, y + height - ryBL * (1 - NVG_KAPPA90), x + rxBL * (1 - NVG_KAPPA90), y + height, x + rxBL, y + height);
            LineTo(x + width - rxBR, y + height);
            BezierTo(x + width - rxBR * (1 - NVG_KAPPA90), y + height, x + width, y + height - ryBR * (1 - NVG_KAPPA90), x + width, y + height - ryBR);
            LineTo(x + width, y + ryTR);
            BezierTo(x + width, y + ryTR * (1 - NVG_KAPPA90), x + width - rxTR * (1 - NVG_KAPPA90), y, x + width - rxTR, y);
            LineTo(x + rxTL, y);
            BezierTo(x + rxTL * (1 - NVG_KAPPA90), y, x, y + ryTL * (1 - NVG_KAPPA90), x, y + ryTL);
            ClosePath();
        }
    }

    void DrawLine(int x1, int y1, int x2, int y2)
    {
        BeginPath();
        MoveTo(x1, y1);
        LineTo(x2, y2);
        Stroke();
    }
    float Alpha { set; }
    float Scale { get; set; }
    void DrawImage(IImage image, int x, int y);
    void SetFont(Font font);
    IFontMetrics GetFontMetrics();
    IFontMetrics GetFontMetrics(Font font);
    void DrawString(ReadOnlySpan<char> text, int x, int y);
    void DrawStringAligned(ReadOnlySpan<char> text, int x, int y, int areaWidth, int areaHeight, TextHorizontalAlignment hAlign = TextHorizontalAlignment.Left, TextVerticalAlignment vAlign = TextVerticalAlignment.Top);
    void DrawStringStrokeAligned(ReadOnlySpan<char> text, int x, int y, int areaWidth, int areaHeight, TextHorizontalAlignment hAlign = TextHorizontalAlignment.Left, TextVerticalAlignment vAlign = TextVerticalAlignment.Top, int effectAmount = 1);
    
    void DrawStringAligned(ReadOnlySpan<char> text, int areaWidth, int areaHeight, TextHorizontalAlignment hAlign = TextHorizontalAlignment.Left, TextVerticalAlignment vAlign = TextVerticalAlignment.Top) 
        => DrawStringAligned(text, 0, 0, areaWidth, areaHeight, hAlign, vAlign);

    void DrawStringStrokeAligned(ReadOnlySpan<char> text, int areaWidth, int areaHeight, TextHorizontalAlignment hAlign = TextHorizontalAlignment.Left, TextVerticalAlignment vAlign = TextVerticalAlignment.Top, int effectAmount = 1)
        => DrawStringStrokeAligned(text, 0, 0, areaWidth, areaHeight, hAlign, vAlign, effectAmount);

    void DrawStringStroke(ReadOnlySpan<char> text, int x, int y, int effectAmount = 1)
    {
    }

    void FillOval(int x, int y, int width, int height)
    {
        BeginPath();
        Ellipse(x + width / 2f, y + height / 2f, width / 2f, height / 2f);
        Fill();
    }
    
    void Ellipse(float cx, float cy, float rx, float ry)
    {
        MoveTo(cx - rx, cy);
        BezierTo(cx - rx, cy + ry * NVG_KAPPA90, cx - rx * NVG_KAPPA90, cy + ry, cx, cy + ry);
        BezierTo(cx + rx * NVG_KAPPA90, cy + ry, cx + rx, cy + ry * NVG_KAPPA90, cx + rx, cy);
        BezierTo(cx + rx, cy - ry * NVG_KAPPA90, cx + rx * NVG_KAPPA90, cy - ry, cx, cy - ry);
        BezierTo(cx - rx * NVG_KAPPA90, cy - ry, cx - rx, cy - ry * NVG_KAPPA90, cx - rx, cy);
        ClosePath();
    }

    void DrawOval(int x, int y, int width, int height)
    {
        BeginPath();
        Ellipse(x + width / 2f, y + height / 2f, width / 2f, height / 2f);
        Stroke();
    }
    
    void DrawRoundedRect(int x, int y, int width, int height, float radTopLeft, float radTopRight,
        float radBottomRight, float radBottomLeft)
    {
        BeginPath();
        RoundedRectVarying(x, y, width, height, radTopLeft, radTopRight, radBottomRight, radBottomLeft);
        Stroke();
    }

    void DrawRect(int x1, int y1, int width, int height)
    {
        BeginPath();
        Rect(x1, y1, width, height);
        Stroke();
    }

    void DrawImage(IImage image, int x, int y, int width, int height);

    void SetAntialiasing(bool useAntialias)
    {
        // empty
    }

    void SetStrokeWidth(float width = 1f)
    {
        // empty
    }

    public void BeginPath();

    public void MoveTo(float x, float y);

    public void LineTo(float x, float y);

    public void BezierTo(float c1x, float c1y, float c2x, float c2y, float x, float y);

    public void ClosePath();

    public void MarkHole();

    public void Stroke();

    public void Fill();
}