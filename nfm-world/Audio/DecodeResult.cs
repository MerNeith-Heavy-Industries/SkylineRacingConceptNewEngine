using System.Buffers;
using Microsoft.Xna.Framework.Audio;

namespace NFMWorld.Audio;

/// <summary>
/// Result of decoding a tracker module.
/// </summary>
public readonly struct DecodeResult(ArraySegment<byte> pcmData, int sampleRate, AudioChannels channels, bool pooled) : IDisposable
{
    public readonly ArraySegment<byte> PcmData = pcmData;
    public readonly int SampleRate = sampleRate;
    public readonly AudioChannels Channels = channels;

    public void Dispose()
    {
        if (pooled && PcmData.Array is {} arr)
        {
            ArrayPool<byte>.Shared.Return(arr);
        }
    }
}