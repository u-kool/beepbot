namespace TwitchIrcMinimal;

public class EnvConfig
{
    public string Channel { get; set; } = "";
    public int Volume { get; set; } = 100;
    public string? DeviceId { get; set; }
    public string TranslateLang { get; set; } = "ru";
    public string SoundConnect { get; set; } = "connect.wav";
    public string SoundConnected { get; set; } = "!m ru-sp150-st success ru-lq-sp150-st коннект ru-sp150-dl {CHANNEL}";
    public string SoundLeave { get; set; } = "leave.wav";
    public string SoundError { get; set; } = "nepravilno.wav";
    private readonly string _envPath;

    public EnvConfig(string? envPath = null)
    {
        _envPath = envPath ?? Path.Combine(AppContext.BaseDirectory, "config.env");
    }

    public void Load()
    {
        if (!File.Exists(_envPath))
            throw new FileNotFoundException($"Config file not found: {_envPath}");
        var env = DotNetEnv.Env.Load(_envPath).ToDictionary(x => x.Key, x => x.Value ?? "");
        Channel = env.TryGetValue("CHANNEL", out var ch) ? ch : "";
        Volume = env.TryGetValue("VOLUME", out var volStr) && int.TryParse(volStr, out var v) ? v : 100;
        Volume = Math.Clamp(Volume, 0, 200);
        DeviceId = env.TryGetValue("DEVICE_ID", out var did) && !string.IsNullOrWhiteSpace(did) ? did : null;
        TranslateLang = env.TryGetValue("TRANSLATE_LANG", out var tl) && !string.IsNullOrWhiteSpace(tl) ? tl : "ru";
        SoundConnect = env.TryGetValue("SOUND_CONNECT", out var sc) && !string.IsNullOrWhiteSpace(sc) ? sc : "connect.wav";
        SoundConnected = env.TryGetValue("SOUND_CONNECTED", out var scd) && !string.IsNullOrWhiteSpace(scd) ? scd : "!m ru-sp150-st success ru-lq-sp150-st коннект ru-sp150-dl {CHANNEL}";
        SoundLeave = env.TryGetValue("SOUND_LEAVE", out var sl) && !string.IsNullOrWhiteSpace(sl) ? sl : "leave.wav";
        SoundError = env.TryGetValue("SOUND_ERROR", out var se) && !string.IsNullOrWhiteSpace(se) ? se : "nepravilno.wav";
    }

    public void SaveChannel(string channel)
    {
        Channel = channel;
        var lines = File.Exists(_envPath) ? File.ReadAllLines(_envPath).ToList() : new List<string>();
        bool found = false;
        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].StartsWith("CHANNEL="))
            {
                lines[i] = $"CHANNEL={channel}";
                found = true;
                break;
            }
        }
        if (!found) lines.Add($"CHANNEL={channel}");
        File.WriteAllLines(_envPath, lines);
    }

    public void SaveVolume(int volume)
    {
        Volume = Math.Clamp(volume, 0, 200);
        var lines = File.Exists(_envPath) ? File.ReadAllLines(_envPath).ToList() : new List<string>();
        bool found = false;
        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].StartsWith("VOLUME="))
            {
                lines[i] = $"VOLUME={Volume}";
                found = true;
                break;
            }
        }
        if (!found) lines.Add($"VOLUME={Volume}");
        File.WriteAllLines(_envPath, lines);
    }

    public void SaveDevice(string? deviceId)
    {
        DeviceId = deviceId;
        var lines = File.Exists(_envPath) ? File.ReadAllLines(_envPath).ToList() : new List<string>();
        bool found = false;
        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].StartsWith("DEVICE_ID="))
            {
                lines[i] = $"DEVICE_ID={deviceId ?? ""}";
                found = true;
                break;
            }
        }
        if (!found) lines.Add($"DEVICE_ID={deviceId ?? ""}");
        File.WriteAllLines(_envPath, lines);
    }

    public void SaveTranslateLang(string lang)
    {
        TranslateLang = lang;
        var lines = File.Exists(_envPath) ? File.ReadAllLines(_envPath).ToList() : new List<string>();
        bool found = false;
        for (int i = 0; i < lines.Count; i++)
        {
            if (lines[i].StartsWith("TRANSLATE_LANG="))
            {
                lines[i] = $"TRANSLATE_LANG={lang}";
                found = true;
                break;
            }
        }
        if (!found) lines.Add($"TRANSLATE_LANG={lang}");
        File.WriteAllLines(_envPath, lines);
    }
}
