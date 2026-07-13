using NAudio.Wave;

namespace TwitchIrcMinimal;

public class StutterSampleProvider : ISampleProvider
{
    private readonly float[] _allSamples;
    private int _position;
    private readonly int _chunkLen;
    private readonly int _fullLen;
    private readonly int _totalLen;
    public WaveFormat WaveFormat { get; }

    public StutterSampleProvider(ISampleProvider source)
    {
        WaveFormat = source.WaveFormat;
        var list = new List<float>();
        var tmp = new float[WaveFormat.SampleRate * WaveFormat.Channels * 2];
        int read;
        while ((read = source.Read(tmp, 0, tmp.Length)) > 0)
            for (int i = 0; i < read; i++) list.Add(tmp[i]);
        _allSamples = list.ToArray();

        int channels = WaveFormat.Channels;
        _chunkLen = (int)(WaveFormat.SampleRate * channels * 0.140);
        if (_chunkLen > _allSamples.Length) _chunkLen = _allSamples.Length;

        _fullLen = _allSamples.Length;
        _totalLen = _chunkLen * 3 + _fullLen;
    }

    public int Read(float[] buffer, int offset, int count)
    {
        if (_position >= _totalLen) return 0;

        int samplesToRead = Math.Min(count, _totalLen - _position);
        for (int i = 0; i < samplesToRead; i++)
        {
            int pos = _position + i;
            if (pos < _chunkLen * 3)
            {
                buffer[offset + i] = _allSamples[pos % _chunkLen];
            }
            else
            {
                buffer[offset + i] = _allSamples[pos - _chunkLen * 3];
            }
        }
        _position += samplesToRead;
        return samplesToRead;
    }
}
