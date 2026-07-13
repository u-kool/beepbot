namespace TwitchIrcMinimal;

public class PlayCommand
{
    public string SoundNames { get; set; } = "";
    public string Effects { get; set; } = "";
    public bool Mix { get; set; }
}

public class Player
{
    private readonly AudioEngine _engine;
    private readonly Queue<PlayCommand> _queue = new();
    private bool _playing;
    private readonly object _lock = new();
    private int _lastVolume = 100;

    public bool QueueEnabled { get; set; }

    public Player(AudioEngine engine) { _engine = engine; }

    public void Play(PlayCommand cmd, int volume)
    {
        if (QueueEnabled && _playing)
        {
            lock (_lock) { if (_queue.Count < 50) _queue.Enqueue(cmd); }
            return;
        }
        _lastVolume = volume;
        _playing = true;
        _engine.PlayAll(new[] { cmd }, volume);
    }

    public void PlayAll(List<PlayCommand> cmds, int volume)
    {
        if (cmds.Count == 0) return;
        _lastVolume = volume;
        if (QueueEnabled && _playing)
        {
            lock (_lock)
            {
                foreach (var cmd in cmds)
                    if (_queue.Count < 50) _queue.Enqueue(cmd);
            }
            return;
        }
        lock (_lock) _queue.Clear();
        _playing = true;
        // Play ALL commands as one seamless stream
        _engine.PlayAll(cmds, volume);
    }

    public void Stop()
    {
        lock (_lock) _queue.Clear();
        _playing = false;
        _engine.Stop();
    }

    public void Skip() { _engine.Stop(); }

    public void OnPlaybackFinished()
    {
        lock (_lock)
        {
            if (_queue.Count > 0)
            {
                var next = _queue.Dequeue();
                _engine.PlayAll(new[] { next }, _lastVolume);
            }
            else
            {
                _playing = false;
            }
        }
    }
}
