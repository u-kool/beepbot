namespace TwitchIrcMinimal;

public class Bot
{
    public string Channel { get; }
    public int Volume { get; set; }
    public bool IsMuted { get; private set; }
    private bool _queueEnabled;
    public bool QueueEnabled
    {
        get => _queueEnabled;
        set { _queueEnabled = value; QueueChanged?.Invoke(); }
    }
    public event Action? QueueChanged;
    public bool ErEnabled { get; set; } = true;

    public AudioEngine Engine { get; }
    public Player Player { get; }
    public Dictionary<string, string> TtsLanguages { get; }
    private long _ttsCounter;

    public Bot(string channel, int volume, Dictionary<string, string> ttsLanguages)
    {
        Channel = channel;
        Volume = Math.Clamp(volume, 0, 200);
        TtsLanguages = ttsLanguages;
        Engine = new AudioEngine();
        Player = new Player(Engine);
        Engine.PlaybackFinished += () => Player.OnPlaybackFinished();
    }

    public void SetMuted(bool muted)
    {
        IsMuted = muted;
        if (muted)
        {
            Engine.Stop();
            Player.Stop();
        }
    }

    public string NextTtsKey() => $"tts_temp_{Interlocked.Increment(ref _ttsCounter)}";
}
