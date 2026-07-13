using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace TwitchIrcMinimal;

public class AudioEngine
{
    private readonly SoundLibrary _library = new();
    private readonly List<IWavePlayer> _outputs = new();
    private bool _erEnabled;
    private readonly object _lock = new();
    private string? _selectedDeviceId;

    public event Action? PlaybackFinished;

    public int LoadSounds(string dirPath)
    {
        LogDevices();
        return _library.LoadDirectory(dirPath);
    }

    public static List<(string id, string name)> GetOutputDevices()
    {
        var result = new List<(string, string)>();
        try
        {
            using var enumerator = new MMDeviceEnumerator();
            var devices = enumerator.EnumerateAudioEndPoints(DataFlow.Render, DeviceState.Active);
            foreach (var d in devices)
            {
                result.Add((d.ID, d.FriendlyName));
                d.Dispose();
            }
        }
        catch { }
        return result;
    }

    public void SetDevice(string? deviceId)
    {
        _selectedDeviceId = deviceId;
        Gui.Log.Info($"AudioEngine: device set to '{deviceId ?? "default"}'");
    }

    private MMDevice? GetDevice()
    {
        try
        {
            if (!string.IsNullOrEmpty(_selectedDeviceId))
            {
                using var enumerator = new MMDeviceEnumerator();
                return enumerator.GetDevice(_selectedDeviceId);
            }
        }
        catch (Exception ex)
        {
            Gui.Log.Error($"AudioEngine: device '{_selectedDeviceId}' not found: {ex.Message}");
        }
        return null;
    }

    private static void LogDevices()
    {
        var devices = GetOutputDevices();
        Gui.Log.Info($"AudioEngine: {devices.Count} output device(s)");
        foreach (var (id, name) in devices)
            Gui.Log.Info($"  {name} [{id}]");
    }

    public bool LoadFile(string name, string filePath)
    {
        try
        {
            using var reader = new AudioFileReader(filePath);
            var list = new List<float>();
            var buffer = new float[reader.WaveFormat.SampleRate * reader.WaveFormat.Channels];
            int read;
            while ((read = reader.Read(buffer, 0, buffer.Length)) > 0)
                for (int i = 0; i < read; i++) list.Add(buffer[i]);
            _library.AddTemp(name, list.ToArray());
            return true;
        }
        catch { return false; }
    }

    public void AddSamples(string name, float[] samples) => _library.AddTemp(name, samples);

    public void RemoveTemp(string name) => _library.RemoveTemp(name);

    public IReadOnlyCollection<string> GetSoundNames() => _library.Names;

    public WaveFormat GetDefaultFormat() => _library.Format ?? new WaveFormat(44100, 32, 2);

    public void Play(string soundNames, string effects, int volume)
    {
        PlayAll(new[] { new PlayCommand { SoundNames = soundNames, Effects = effects } }, volume);
    }

    public void PlayAll(IReadOnlyList<PlayCommand> cmds, int volume)
    {
        if (cmds.Count == 0) return;
        Task.Run(() =>
        {
            try
            {
                var format = GetDefaultFormat();
                int channels = format.Channels < 2 ? 2 : format.Channels;
                var stereoFormat = WaveFormat.CreateIeeeFloatWaveFormat(format.SampleRate, channels);

                // Process each command: apply its effects, then collect the result
                var allProcessed = new List<(float[] samples, bool mix)>();
                foreach (var cmd in cmds)
                {
                    var names = cmd.SoundNames.Split('+', StringSplitOptions.RemoveEmptyEntries);
                    // Mix sounds within one command (e.g. "car+dog")
                    int maxLen = 0;
                    var cmdSamples = new List<float[]>();
                    foreach (var name in names)
                    {
                        float[]? samples;
                        if (name.Equals("rand", StringComparison.OrdinalIgnoreCase))
                            samples = _library.GetRandomName() is { } rn ? _library.GetSoundSamples(rn) : null;
                        else
                            samples = _library.GetSoundSamples(name);
                        if (samples != null)
                        {
                            if (format.Channels < 2)
                            {
                                var stereo = new float[samples.Length * 2];
                                for (int i = 0; i < samples.Length; i++)
                                {
                                    stereo[i * 2] = samples[i];
                                    stereo[i * 2 + 1] = samples[i];
                                }
                                samples = stereo;
                            }
                            cmdSamples.Add(samples);
                            if (samples.Length > maxLen) maxLen = samples.Length;
                        }
                    }
                    if (cmdSamples.Count == 0) continue;

                    // Mix sounds within this command
                    var mixed = new float[maxLen];
                    foreach (var s in cmdSamples)
                        for (int i = 0; i < s.Length; i++)
                            mixed[i] += s[i];

                    // Apply THIS command's effects
                    ISampleProvider provider = new MemorySampleProvider(mixed, stereoFormat);
                    var soundParams = SoundParams.Parse(cmd.Effects, _erEnabled);
                    Gui.Log.Info($"AudioEngine: cmd '{cmd.SoundNames}' effects='{cmd.Effects}' speed={soundParams.Speed}");
                    provider = EffectChain.Build(provider, soundParams, _erEnabled);

                    // Read processed samples back
                    var processed = new List<float>();
                    var buf = new float[4096];
                    int read;
                    while ((read = provider.Read(buf, 0, buf.Length)) > 0)
                        for (int i = 0; i < read; i++) processed.Add(buf[i]);

                    allProcessed.Add((processed.ToArray(), cmd.Mix));
                }

                if (allProcessed.Count == 0)
                {
                    Gui.Log.Info($"AudioEngine: no samples");
                    PlaybackFinished?.Invoke();
                    return;
                }

                // Group consecutive commands: mixed ones overlap, others concatenate
                var final = new List<float[]>();
                int gi = 0;
                while (gi < allProcessed.Count)
                {
                    bool currentMix = allProcessed[gi].mix;
                    int start = gi;
                    while (gi < allProcessed.Count && allProcessed[gi].mix == currentMix) gi++;
                    int end = gi;

                    if (currentMix)
                    {
                        // Mix (overlap) all in this group
                        int maxLen = 0;
                        for (int k = start; k < end; k++)
                            if (allProcessed[k].samples.Length > maxLen)
                                maxLen = allProcessed[k].samples.Length;
                        var mixed = new float[maxLen];
                        for (int k = start; k < end; k++)
                        {
                            var s = allProcessed[k].samples;
                            for (int j = 0; j < s.Length; j++)
                                mixed[j] += s[j];
                        }
                        final.Add(mixed);
                    }
                    else
                    {
                        // Concatenate sequentially
                        for (int k = start; k < end; k++)
                            final.Add(allProcessed[k].samples);
                    }
                }

                // Build final buffer
                int totalLen = 0;
                foreach (var s in final) totalLen += s.Length;
                var concatenated = new float[totalLen];
                int pos = 0;
                foreach (var s in final)
                {
                    Array.Copy(s, 0, concatenated, pos, s.Length);
                    pos += s.Length;
                }

                Gui.Log.Info($"AudioEngine: {cmds.Count} cmd(s), {concatenated.Length} samples");

                // Volume only
                float vol = Math.Clamp(volume / 100f, 0f, 2f);
                ISampleProvider finalProvider = new VolumeSampleProvider(
                    new MemorySampleProvider(concatenated, stereoFormat)) { Volume = vol };

                var tempWav = Path.Combine(Path.GetTempPath(), $"beepbot_temp_{Guid.NewGuid():N}.wav");
                var outFormat = new WaveFormat(stereoFormat.SampleRate, 16, stereoFormat.Channels);
                using (var writer = new WaveFileWriter(tempWav, outFormat))
                {
                    var tmpBuf = new float[4096];
                    int read;
                    while ((read = finalProvider.Read(tmpBuf, 0, tmpBuf.Length)) > 0)
                    {
                        var byteBuf = new byte[read * 2];
                        for (int i = 0; i < read; i++)
                        {
                            float clamped = Math.Clamp(tmpBuf[i], -1f, 1f);
                            short pcm = (short)(clamped * short.MaxValue);
                            byteBuf[i * 2] = (byte)(pcm & 0xFF);
                            byteBuf[i * 2 + 1] = (byte)((pcm >> 8) & 0xFF);
                        }
                        writer.Write(byteBuf, 0, byteBuf.Length);
                    }
                }
                Gui.Log.Info($"AudioEngine: wrote temp wav {new FileInfo(tempWav).Length} bytes");

                var reader = new AudioFileReader(tempWav);
                var device = GetDevice();
                var wo = device != null
                    ? new WasapiOut(device, AudioClientShareMode.Shared, false, 0)
                    : new WasapiOut();
                lock (_lock) { _outputs.Add(wo); }
                wo.PlaybackStopped += (s, e) =>
                {
                    Gui.Log.Info("AudioEngine: PlaybackStopped");
                    reader.Dispose();
                    try { File.Delete(tempWav); } catch { }
                    lock (_lock) { _outputs.Remove(wo); }
                    wo.Dispose();
                    PlaybackFinished?.Invoke();
                };
                wo.Init(reader);
                wo.Play();
                Gui.Log.Info($"AudioEngine: started playing ({cmds.Count} cmd(s)) vol={vol}");
            }
            catch (Exception ex)
            {
                Gui.Log.Error($"AudioEngine.Play: {ex}");
                PlaybackFinished?.Invoke();
            }
        });
    }

    public void Stop()
    {
        List<IWavePlayer> snapshot;
        lock (_lock)
        {
            snapshot = new List<IWavePlayer>(_outputs);
            _outputs.Clear();
        }
        foreach (var wo in snapshot)
        {
            try { wo.Stop(); wo.Dispose(); } catch { }
        }
    }

    public void SetErEnabled(bool enabled) => _erEnabled = enabled;
}

internal class VolumeSampleProvider : ISampleProvider
{
    private readonly ISampleProvider _source;
    public float Volume { get; set; } = 1f;
    public WaveFormat WaveFormat => _source.WaveFormat;

    public VolumeSampleProvider(ISampleProvider source) { _source = source; }

    public int Read(float[] buffer, int offset, int count)
    {
        int read = _source.Read(buffer, offset, count);
        for (int i = offset; i < offset + read; i++)
            buffer[i] = Math.Clamp(buffer[i] * Volume, -1f, 1f);
        return read;
    }
}
