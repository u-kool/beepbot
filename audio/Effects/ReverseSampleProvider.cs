using NAudio.Wave;

namespace TwitchIrcMinimal;

public class ReverseSampleProvider : ISampleProvider
{
    private readonly float[] _buffer;
    private int _position;
    public WaveFormat WaveFormat { get; }

    public ReverseSampleProvider(ISampleProvider source)
    {
        WaveFormat = source.WaveFormat;
        var list = new List<float>();
        var tmp = new float[WaveFormat.SampleRate * WaveFormat.Channels * 2];
        int read;
        while ((read = source.Read(tmp, 0, tmp.Length)) > 0)
            for (int i = 0; i < read; i++) list.Add(tmp[i]);
        _buffer = list.ToArray();
        Array.Reverse(_buffer);
    }

    public int Read(float[] buffer, int offset, int count)
    {
        int available = _buffer.Length - _position;
        int toRead = Math.Min(count, available);
        Array.Copy(_buffer, _position, buffer, offset, toRead);
        _position += toRead;
        return toRead;
    }
}
