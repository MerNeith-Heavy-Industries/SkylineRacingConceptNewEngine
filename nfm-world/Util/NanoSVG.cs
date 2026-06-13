using System.Text;
using CommunityToolkit.HighPerformance;
using CommunityToolkit.HighPerformance.Buffers;
using NanoSvgSharp;
using NFMWorld.DriverInterface;
using NvgSharp;

// ReSharper disable InconsistentNaming

namespace NFMWorld.Util;

public sealed class NanoSVGImage : IImage
{
	private readonly NsvgImage _image;

	public int Height => (int)_image.Height;
	public int Width => (int)_image.Width;

	public NanoSVGImage(NsvgImage image)
	{
		_image = image;
	}

	public static NanoSVGImage FromStream(Stream stream, string units = "px", float dpi = 96.0f)
	{
		using var arr = new ArrayPoolBufferWriter<byte>();
		stream.CopyTo(arr.AsStream());
		using var arr1 = new ArrayPoolBufferWriter<char>();
		var count = Encoding.UTF8.GetCharCount(arr.WrittenSpan);
		if (!Encoding.UTF8.TryGetChars(arr.WrittenSpan, arr1.GetSpan(count), out int charsWritten))
		{
			throw new InvalidOperationException("Failed to parse SVG data: Invalid encoding");
		}
		return new NanoSVGImage(NsvgParser.Parse(arr1.WrittenSpan[..charsWritten], units, dpi));
	}

	public void Draw(NvgContext vg)
	{
		NsvgRenderer.Render(vg, _image);
	}
}