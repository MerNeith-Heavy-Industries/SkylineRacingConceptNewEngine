using System.Buffers;
using Microsoft.Xna.Framework.Audio;

namespace NFMWorld.Audio;

/// <summary>
/// Result of decoding a tracker module.
/// </summary>
public readonly struct DecodeResult(byte[] pcmData, int sampleRate, AudioChannels channels, bool pooled) : IDisposable
{
    public readonly byte[] PcmData = pcmData;
    public readonly int SampleRate = sampleRate;
    public readonly AudioChannels Channels = channels;

    public void Dispose()
    {
        if (pooled)
        {
            ArrayPool<byte>.Shared.Return(PcmData);
        }
    }
}