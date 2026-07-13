using NAudio.Wave;

namespace TwitchIrcMinimal;

public class BitcrushSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private int _counter;
    private float _holdL;
    private float _holdR;
    private const int SampleHoldInterval = 8;
    private const float BitDepthMultiplier = 64.0f;
    public WaveFormat WaveFormat => _source.WaveFormat;

    public BitcrushSampleProvider(ISampleProvider source) { _source = source; }

    public int Read(float[] buffer, int offset, int count)
    {
        int read = _source.Read(buffer, offset, count);
        int channels = WaveFormat.Channels;
        for (int i = offset; i < offset + read; i += channels)
        {
            if (_counter == 0 || _counter == SampleHoldInterval)
            {
                _holdL = buffer[i];
                _holdR = (channels >= 2 && i + 1 < offset + read) ? buffer[i + 1] : buffer[i];
                _counter = 0;
            }
            else
            {
                buffer[i] = _holdL;
                if (channels >= 2 && i + 1 < offset + read)
                    buffer[i + 1] = _holdR;
            }

            buffer[i] = BitCrash(buffer[i]);
            if (channels >= 2 && i + 1 < offset + read)
                buffer[i + 1] = BitCrash(buffer[i + 1]);

            _counter++;
        }
        return read;
    }

    private static float BitCrash(float sample)
    {
        return MathF.Round(sample * BitDepthMultiplier) / BitDepthMultiplier;
    }
}
