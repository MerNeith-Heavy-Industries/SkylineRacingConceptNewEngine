using Microsoft.Xna.Framework;

namespace NFMWorld.DriverInterface;

public sealed class DummyBackend : IBackend
{
    public Vector2 Viewport => new();
    public float Scale { get; set; } = 1;

    public IRadicalMusic LoadMusic(string file, double tempomul)
    {
        return new DummyMusic();
    }

    public IImage LoadImage(string file)
    {
        return new DummyImage();
    }
    
    public IImage LoadCachedImage(string file)
    {
        return new DummyImage();
    }

    public IImage LoadImage(ReadOnlySpan<byte> file)
    {
        return new DummyImage();
    }

    public void StopAllSounds()
    {
    }

    public ISoundClip GetSound(string filePath)
    {
        return new DummySoundClip();
    }

    public IGraphics Graphics { get; } = new DummyGraphics();

    public sealed class DummyGraphics : IGraphics
    {
        public void SetLinearGradient(int x, int y, int width, int height, Color[] colors, float[]? colorPos)
        {
        }

        public void SetColor(Color c)
        {
        }

        public void FillPolygon(ReadOnlySpan<int> x, ReadOnlySpan<int> y, int n)
        {
        }

        public void DrawPolygon(ReadOnlySpan<int> x, ReadOnlySpan<int> y, int n)
        {
        }

        public void FillRect(int x1, int y1, int width, int height)
        {
        }

        public void DrawLine(int x1, int y1, int x2, int y2)
        {
        }

        public float Alpha
        {
            set { }
        }

        public void DrawImage(IImage image, int x, int y)
        {
        }

        public void SetFont(Font font)
        {
        }

        public IFontMetrics GetFontMetrics()
        {
            return new DummyFontMetrics();
        }

        public IFontMetrics GetFontMetrics(Font font)
        {
            return new DummyFontMetrics();
        }

        public void DrawString(ReadOnlySpan<char> text, int x, int y)
        {
        }
        public void DrawStringAligned(ReadOnlySpan<char> text, int x, int y, int areaWidth, int areaHeight, TextHorizontalAlignment hAlign = TextHorizontalAlignment.Left, TextVerticalAlignment vAlign = TextVerticalAlignment.Top)
        {
        }

        public void DrawStringStrokeAligned(ReadOnlySpan<char> text, int x, int y, int areaWidth, int areaHeight, TextHorizontalAlignment hAlign = TextHorizontalAlignment.Left, TextVerticalAlignment vAlign = TextVerticalAlignment.Top, int effectAmount = 1)
        {
        }

        public void FillOval(int p0, int p1, int p2, int p3)
        {
        }

        public void FillRoundRect(int x, int y, int wid, int hei, int arcWid, int arcHei)
        {
        }

        public void DrawRoundRect(int x, int y, int wid, int hei, int arcWid, int arcHei)
        {
        }

        public void DrawRect(int x1, int y1, int width, int height)
        {
        }

        public void DrawImage(IImage image, int x, int y, int width, int height)
        {
        }
    }

    public void SetAllVolumes(float vol)
    {
    }
}

file sealed class DummyMusic : IRadicalMusic
{
    public void SetPaused(bool p0)
    {
    }

    public void Unload()
    {
    }

    public void Play()
    {
    }

    public void SetVolume(float vol)
    {
    }

    public float GetVolume()
    {
        return 1f;
    }

    public void SetFreqMultiplier(double multiplier)
    {
    }
}

file sealed class DummyFontMetrics : IFontMetrics
{
    public Vector2 MeasureText(ReadOnlySpan<char> text)
    {
        return Vector2.Zero;
    }

    public float LineHeight => 0;
}

file sealed class DummySoundClip : ISoundClip
{
    public void Play()
    {
    }

    public void Checkopen()
    {
    }

    public void Loop()
    {
    }

    public void Stop()
    {
    }
}

file sealed class DummyImage : IImage
{
    public int Height => 0;
    public int Width => 0;
}