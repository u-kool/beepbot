using NAudio.Wave;

namespace TwitchIrcMinimal;

public class DelaySampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private readonly float[] _lBuffer;
    private readonly float[] _rBuffer;
    private int _counter;
    private int _tailSamples;
    private bool _sourceDone;
    private const int DelayBufferSize = 8820;
    private const int TailSamplesMax = 44100;
    private const float DryGain = 0.85f;
    private const float WetGain = 0.4f;
    public WaveFormat WaveFormat => _source.WaveFormat;

    public DelaySampleProvider(ISampleProvider source)
    {
        _source = source;
        _lBuffer = new float[DelayBufferSize];
        _rBuffer = new float[DelayBufferSize];
        _tailSamples = TailSamplesMax;
    }

    public int Read(float[] buffer, int offset, int count)
    {
        int channels = WaveFormat.Channels;
        int framesRequested = count / channels;

        int read = _source.Read(buffer, offset, count);
        int framesRead = read / channels;
        bool sourceFinished = framesRead < framesRequested;
        if (sourceFinished && !_sourceDone)
        {
            _sourceDone = true;
        }

        int totalFrames = framesRead;
        if (_sourceDone && _tailSamples > 0)
        {
            totalFrames = framesRequested;
        }

        int written = 0;
        for (int frame = 0; frame < totalFrames; frame++)
        {
            float inputL, inputR;
            if (frame < framesRead)
            {
                inputL = buffer[offset + frame * channels];
                inputR = (channels >= 2) ? buffer[offset + frame * channels + 1] : inputL;
            }
            else
            {
                inputL = 0f;
                inputR = 0f;
            }

            float echoL = _lBuffer[_counter];
            float echoR = _rBuffer[_counter];

            float outputL = inputL * DryGain + echoL * WetGain;
            float outputR = inputR * DryGain + echoR * WetGain;

            _rBuffer[_counter] = outputL;
            _lBuffer[_counter] = outputR;

            _counter++;
            if (_counter >= DelayBufferSize)
                _counter = 0;

            if (frame < framesRead)
            {
                buffer[offset + frame * channels] = outputL;
                if (channels >= 2)
                    buffer[offset + frame * channels + 1] = outputR;
            }
            else
            {
                buffer[offset + frame * channels] = outputL;
                if (channels >= 2)
                    buffer[offset + frame * channels + 1] = outputR;
                _tailSamples--;
                if (_tailSamples == 0)
                {
                    written = (frame + 1) * channels;
                    return written;
                }
            }
            written = (frame + 1) * channels;
        }

        if (_sourceDone && _tailSamples <= 0)
            return written;

        return written;
    }
}
