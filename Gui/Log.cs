namespace TwitchIrcMinimal.Gui;

public static class Log
{
    private static readonly object _lock = new();
    private static readonly string _logPath;

    static Log()
    {
        _logPath = Path.Combine(AppContext.BaseDirectory, "beepbot.log");
        try { File.WriteAllText(_logPath, $"=== beepbot started {DateTime.Now:yyyy-MM-dd HH:mm:ss} ===\n"); }
        catch { }
    }

    public static void Info(string msg) => Write("INFO", msg);
    public static void Error(string msg) => Write("ERROR", msg);

    private static void Write(string level, string msg)
    {
        var line = $"{DateTime.Now:HH:mm:ss} [{level}] {msg}";
        try
        {
            lock (_lock) File.AppendAllText(_logPath, line + "\n");
        }
        catch { }
    }
}
