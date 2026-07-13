using NAudio.Wave;

namespace TwitchIrcMinimal;

public class SpeedSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private readonly float _speed;
    private float _fractionalCarry;
    public WaveFormat WaveFormat => _source.WaveFormat;

    public SpeedSampleProvider(ISampleProvider source, float speed)
    {
        _source = source;
        _speed = Math.Clamp(speed, 0.1f, 2.0f);
    }

    public int Read(float[] buffer, int offset, int count)
    {
        int channels = WaveFormat.Channels;
        int framesRequested = count / channels;

        int srcCapacity = (int)(framesRequested * _speed) + 2;
        var srcBuffer = new float[srcCapacity * channels];
        int srcRead = _source.Read(srcBuffer, 0, srcBuffer.Length);
        int srcFrames = srcRead / channels;

        int written = 0;
        float pos = _fractionalCarry;

        for (int frame = 0; frame < framesRequested; frame++)
        {
            int srcPos = (int)pos;
            float frac = pos - srcPos;

            if (srcPos + 1 >= srcFrames) break;

            for (int ch = 0; ch < channels; ch++)
            {
                float s0 = srcBuffer[srcPos * channels + ch];
                float s1 = srcBuffer[(srcPos + 1) * channels + ch];
                buffer[offset + written + ch] = s0 + (s1 - s0) * frac;
            }
            written += channels;
            pos += _speed;
        }

        _fractionalCarry = pos - (int)pos;
        return written;
    }
}
