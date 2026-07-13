using NAudio.Wave;

namespace TwitchIrcMinimal;

public static class EffectChain
{
    public static ISampleProvider Build(ISampleProvider source, SoundParams p, bool erEnabled = true)
    {
        ISampleProvider current = source;

        if (p.CutStart > 0f || p.CutEnd > 0f)
        {
            int channels = current.WaveFormat.Channels;
            int totalFrames;
            if (current is MemorySampleProvider mem)
                totalFrames = mem.TotalFrames;
            else
                totalFrames = CountFrames(current);

            int startFrame = (int)(totalFrames * p.CutStart);
            int endFrame = (int)(totalFrames * (1f - p.CutEnd));
            if (startFrame < endFrame)
                current = new CutSampleProvider(current, startFrame * channels, (endFrame - startFrame) * channels);
        }

        if (p.Reverse) current = new ReverseSampleProvider(current);
        if (p.Bitcrush) current = new BitcrushSampleProvider(current);
        if (p.Stutter) current = new StutterSampleProvider(current);
        if (p.EarRape) current = new EarRapeSampleProvider(current);
        if (p.Delay) current = new DelaySampleProvider(current);
        if (p.Vibrato) current = new VibratoSampleProvider(current);
        if (Math.Abs(p.Speed - 1.0f) > 0.01f) current = new SpeedSampleProvider(current, p.Speed);
        if (p.Gacha) current = new GachaSampleProvider(current, erEnabled);
        return current;
    }

    private static int CountFrames(ISampleProvider source)
    {
        int channels = source.WaveFormat.Channels;
        var tmp = new float[channels * 4096];
        long totalSamples = 0;
        int read;
        while ((read = source.Read(tmp, 0, tmp.Length)) > 0)
            totalSamples += read;
        return (int)(totalSamples / channels);
    }
}

internal class CutSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    private int _skipped;
    private readonly int _startSample;
    private readonly int _count;
    private int _read;
    public WaveFormat WaveFormat => _source.WaveFormat;

    public CutSampleProvider(ISampleProvider source, int startSample, int count)
    {
        _source = source;
        _startSample = startSample;
        _count = count;
    }

    public int Read(float[] buffer, int offset, int count)
    {
        while (_skipped < _startSample)
        {
            var skip = new float[Math.Min(4096, _startSample - _skipped)];
            int s = _source.Read(skip, 0, skip.Length);
            if (s == 0) return 0;
            _skipped += s;
        }
        if (_read >= _count) return 0;
        int toRead = Math.Min(count, _count - _read);
        int read = _source.Read(buffer, offset, toRead);
        _read += read;
        return read;
    }
}
