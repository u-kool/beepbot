using NAudio.Wave;

namespace TwitchIrcMinimal;

public class EarRapeSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private const float ClipLimit = 0.05f;
    private const float VolumeMultiplier = 5.0f;
    public WaveFormat WaveFormat => _source.WaveFormat;

    public EarRapeSampleProvider(ISampleProvider source) { _source = source; }

    public int Read(float[] buffer, int offset, int count)
    {
        int read = _source.Read(buffer, offset, count);
        for (int i = offset; i < offset + read; i++)
        {
            if (buffer[i] < -ClipLimit) buffer[i] = -ClipLimit;
            if (buffer[i] > ClipLimit) buffer[i] = ClipLimit;
            buffer[i] *= VolumeMultiplier;
        }
        return read;
    }
}
