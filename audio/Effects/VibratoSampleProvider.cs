using NAudio.Wave;

namespace TwitchIrcMinimal;

public class VibratoSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private readonly float[] _circularBuffer;
    private const int BufferFrames = 1024;
    private const float Frequency = 8f;
    private const float Depth = 0.08f;
    private readonly int _channels;
    private readonly int _sampleRate;
    private int _writeCount;
    private float _pos;
    private int _sampleCount;
    private bool _sourceDone;

    public WaveFormat WaveFormat => _source.WaveFormat;

    public VibratoSampleProvider(ISampleProvider source)
    {
        _source = source;
        _channels = source.WaveFormat.Channels;
        _sampleRate = source.WaveFormat.SampleRate;
        _circularBuffer = new float[BufferFrames * _channels];
    }

    public int Read(float[] buffer, int offset, int count)
    {
        int framesRequested = count / _channels;
        int framesWritten = 0;

        while (framesWritten < framesRequested)
        {
            while (_writeCount <= (int)_pos + 1 && !_sourceDone)
            {
                var temp = new float[_channels];
                int read = _source.Read(temp, 0, _channels);
                if (read >= _channels)
                {
                    int idx = (_writeCount % BufferFrames) * _channels;
                    for (int ch = 0; ch < _channels; ch++)
                        _circularBuffer[idx + ch] = temp[ch];
                    _writeCount++;
                }
                else
                {
                    _sourceDone = true;
                }
            }

            if (_writeCount <= (int)_pos + 1 && _sourceDone)
                break;

            int index1 = (int)_pos;
            int index2 = index1 + 1;
            int wrap1 = ((index1 % BufferFrames) + BufferFrames) % BufferFrames;
            int wrap2 = ((index2 % BufferFrames) + BufferFrames) % BufferFrames;
            float frac = _pos - index1;

            for (int ch = 0; ch < _channels; ch++)
            {
                float s1 = _circularBuffer[wrap1 * _channels + ch];
                float s2 = _circularBuffer[wrap2 * _channels + ch];
                buffer[offset + framesWritten * _channels + ch] = s1 * (1f - frac) + s2 * frac;
            }

            float timeSec = (float)_sampleCount / _sampleRate;
            float step = MathF.Sin(2f * MathF.PI * Frequency * timeSec) * Depth + 1f;
            _pos += step;
            _sampleCount++;
            framesWritten++;
        }

        return framesWritten * _channels;
    }
}
