using System.Collections.Concurrent;
using NAudio.Wave;

namespace TwitchIrcMinimal;

public class SoundLibrary
{
    private static readonly WaveFormat TargetFormat = WaveFormat.CreateIeeeFloatWaveFormat(44100, 2);

    private readonly ConcurrentDictionary<string, float[]> _sounds = new();
    private WaveFormat? _format;

    public int Count => _sounds.Count;
    public WaveFormat? Format => _format;
    public IReadOnlyCollection<string> Names => _sounds.Keys.ToList().AsReadOnly();

    public int LoadDirectory(string dirPath)
    {
        if (!Directory.Exists(dirPath)) return 0;
        int loaded = 0;
        foreach (var file in Directory.GetFiles(dirPath, "*.*", SearchOption.TopDirectoryOnly))
        {
            var ext = Path.GetExtension(file).ToLowerInvariant();
            if (ext is not (".wav" or ".mp3")) continue;
            var name = Path.GetFileNameWithoutExtension(file).ToLowerInvariant();
            try
            {
                var samples = LoadAudioFile(file);
                if (samples is not null) { _sounds[name] = samples; loaded++; }
            }
            catch { }
        }
        return loaded;
    }

    private float[]? LoadAudioFile(string filePath)
    {
        _format ??= TargetFormat;

        using var reader = new AudioFileReader(filePath);
        var source = reader.WaveFormat.SampleRate == TargetFormat.SampleRate &&
                     reader.WaveFormat.Channels == TargetFormat.Channels
            ? (ISampleProvider)reader
            : new MediaFoundationResampler(reader, TargetFormat) { ResamplerQuality = 60 }.ToSampleProvider();

        var list = new List<float>();
        var buffer = new float[TargetFormat.SampleRate * TargetFormat.Channels];
        int read;
        while ((read = source.Read(buffer, 0, buffer.Length)) > 0)
        {
            for (int i = 0; i < read; i++) list.Add(buffer[i]);
        }

        return list.ToArray();
    }

    public float[]? GetSoundSamples(string name)
    {
        _sounds.TryGetValue(name, out var samples);
        return samples;
    }

    public string? GetRandomName()
    {
        var keys = _sounds.Keys.ToList();
        return keys.Count == 0 ? null : keys[Random.Shared.Next(keys.Count)];
    }

    public void AddTemp(string name, float[] samples) => _sounds[name] = samples;

    public void RemoveTemp(string name) => _sounds.TryRemove(name, out _);
}
