using System.Buffers;
using Collections.Pooled;
using SoundTouch;

namespace NFMWorld.Audio;

/// <summary>
/// Offline audio time-stretching using SoundTouch.
/// Changes tempo without affecting pitch.
/// </summary>
public static class TempoStretcher
{
    /// <summary>
    /// Process 16-bit PCM data through SoundTouch to change tempo.
    /// </summary>
    /// <param name="pcmData">Input 16-bit stereo or mono PCM samples.</param>
    /// <param name="sampleRate">Sample rate in Hz (e.g., 44100).</param>
    /// <param name="channels">1 for mono, 2 for stereo.</param>
    /// <param name="tempoRatio">Tempo multiplier. 1.0 = normal, >1 = faster, &lt;1 = slower.</param>
    /// <returns>Time-stretched 16-bit PCM data.</returns>
    public static byte[] Process(byte[] pcmData, int sampleRate, int channels, double tempoRatio)
    {
        if (Math.Abs(tempoRatio - 1.0) < 0.001)
            return pcmData; // No stretching needed

        // Configure SoundTouch
        var processor = new SoundTouchProcessor
        {
            SampleRate = sampleRate,
            Channels = channels,
            Tempo = tempoRatio
        };

        // Convert 16-bit PCM to float samples (SoundTouch works in float)
        var totalInt16Samples = pcmData.Length / 2;
        var floatInput = ArrayPool<float>.Shared.Rent(totalInt16Samples);
        try
        {
            for (int i = 0; i < totalInt16Samples; i++)
            {
                var int16Sample = (short)(pcmData[i * 2] | (pcmData[i * 2 + 1] << 8));
                floatInput[i] = int16Sample / (float)short.MaxValue;
            }

            // Feed all input samples
            var inputSpan = new ReadOnlySpan<float>(floatInput);
            processor.PutSamples(inputSpan, floatInput.Length);
        }
        finally
        {
            ArrayPool<float>.Shared.Return(floatInput);
        }

        // Flush remaining samples through the pipeline
        processor.Flush();

        // Collect output
        using var outputFloats = new PooledList<float>(floatInput.Length);
        var outputChunk = ArrayPool<float>.Shared.Rent(4096);
        try
        {
            int received;
            while ((received = processor.ReceiveSamples(outputChunk.AsSpan(), outputChunk.Length)) > 0)
            {
                outputFloats.AddRange(outputChunk.AsSpan(0, received));
            }

            // Continue receiving until empty (after Flush)
            while (processor.AvailableSamples > 0)
            {
                received = processor.ReceiveSamples(outputChunk.AsSpan(), outputChunk.Length);
                if (received > 0)
                    outputFloats.AddRange(outputChunk.AsSpan(0, received));
            }

            // Convert back to 16-bit PCM
            var resultFloats = outputFloats.ToArray();
            var resultPcm = new byte[resultFloats.Length * 2];
            for (int i = 0; i < resultFloats.Length; i++)
            {
                var sample = Math.Clamp(resultFloats[i], -1.0f, 1.0f);
                var int16Sample = (short)(sample * short.MaxValue);
                resultPcm[i * 2] = (byte)(int16Sample & 0xFF);
                resultPcm[i * 2 + 1] = (byte)((int16Sample >> 8) & 0xFF);
            }

            return resultPcm;
        }
        finally
        {
            ArrayPool<float>.Shared.Return(outputChunk);
        }
    }
}
