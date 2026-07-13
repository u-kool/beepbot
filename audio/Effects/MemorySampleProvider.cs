using NAudio.Wave;

namespace TwitchIrcMinimal;

public class MemorySampleProvider : ISampleProvider
{
    private readonly float[] _samples;
    private int _position;
    public WaveFormat WaveFormat { get; }
    public int TotalSamples => _samples.Length;
    public int TotalFrames => _samples.Length / WaveFormat.Channels;

    public MemorySampleProvider(float[] samples, WaveFormat waveFormat)
    {
        _samples = samples;
        WaveFormat = waveFormat;
    }

    public int Read(float[] buffer, int offset, int count)
    {
        int available = _samples.Length - _position;
        int toRead = Math.Min(count, available);
        Array.Copy(_samples, _position, buffer, offset, toRead);
        _position += toRead;
        return toRead;
    }
}
