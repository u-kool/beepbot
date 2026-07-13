namespace TwitchIrcMinimal;

public class SoundParams
{
    public float Speed { get; set; } = 1.0f;
    public float CutStart { get; set; }
    public float CutEnd { get; set; }
    public bool Reverse { get; set; }
    public bool Bitcrush { get; set; }
    public bool Stutter { get; set; }
    public bool EarRape { get; set; }
    public bool Delay { get; set; }
    public bool Vibrato { get; set; }
    public bool Gacha { get; set; }

    public static SoundParams Parse(string? effects, bool erEnabled)
    {
        var p = new SoundParams();
        if (string.IsNullOrWhiteSpace(effects)) return p;
        foreach (var token in effects.Split('-', StringSplitOptions.RemoveEmptyEntries))
        {
            var t = token.Trim().ToLowerInvariant();
            if (t.StartsWith("sp") && int.TryParse(t[2..], out var sp))
                p.Speed = Math.Clamp(sp / 100f, 0.1f, 2.0f);
            else if (t.StartsWith("cs") && int.TryParse(t[2..], out var cs))
                p.CutStart = Math.Clamp(cs / 100f, 0f, 0.99f);
            else if (t.StartsWith("ce") && int.TryParse(t[2..], out var ce))
                p.CutEnd = Math.Clamp(ce / 100f, 0f, 0.99f);
            else if (t == "rs") p.Reverse = true;
            else if (t == "lq") p.Bitcrush = true;
            else if (t == "st") p.Stutter = true;
            else if (t == "er") p.EarRape = erEnabled;
            else if (t == "dl") p.Delay = true;
            else if (t == "vb") p.Vibrato = true;
            else if (t == "ga") p.Gacha = true;
        }
        return p;
    }
}
