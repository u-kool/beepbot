using System.Drawing;
using System.Drawing.Drawing2D;

namespace TwitchIrcMinimal.Gui;

public class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _trayIcon;
    private readonly ContextMenuStrip _menu;
    private readonly ToolStripMenuItem _statusItem;
    private readonly ToolStripMenuItem _changeNickItem;
    private readonly ToolStripMenuItem _queueItem;

    private readonly EnvConfig _config;
    private readonly TwitchIrcClient _irc;
    private readonly Bot _bot;

    private string _currentChannel = "";

    public TrayApplicationContext()
    {
        _config = new EnvConfig();
        try { _config.Load(); }
        catch { }

        var ttsLanguages = TtsLanguages.Create();
        _bot = new Bot("", _config.Volume, ttsLanguages);
        var basePath = AppContext.BaseDirectory;
        _bot.Engine.LoadSounds(Path.Combine(basePath, "sounds"));

        _irc = new TwitchIrcClient();
        _irc.Log += msg => Log.Info($"IRC: {msg}");
        _irc.OnMessage += HandleMessage;

        Log.Info($"Sounds loaded: {_bot.Engine.GetSoundNames().Count}");
        Log.Info($"Volume: {_config.Volume}");

        _statusItem = new ToolStripMenuItem("Status: disconnected") { Enabled = false };
        _changeNickItem = new ToolStripMenuItem("Change Nick...") { };
        var exitItem = new ToolStripMenuItem("Exit");

        _changeNickItem.Click += (_, _) => ShowNickForm();
        exitItem.Click += (_, _) => Exit();

        _queueItem = new ToolStripMenuItem("Очередь: OFF");
        _queueItem.Click += (_, _) =>
        {
            _bot.Player.QueueEnabled = !_bot.Player.QueueEnabled;
            _bot.QueueEnabled = _bot.Player.QueueEnabled;
        };
        _bot.QueueChanged += () =>
        {
            _queueItem.Text = _bot.QueueEnabled ? "Очередь: ON" : "Очередь: OFF";
        };

        _menu = new ContextMenuStrip();
        _menu.Items.Add(_statusItem);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(_changeNickItem);
        _menu.Items.Add(_queueItem);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(exitItem);

        _trayIcon = new NotifyIcon
        {
            Text = "beepbot",
            Icon = CreateTrayIcon(),
            ContextMenuStrip = _menu,
            Visible = true,
        };
        _trayIcon.DoubleClick += (_, _) => ShowNickForm();

        Application.ApplicationExit += (_, _) => _trayIcon.Visible = false;

        if (!string.IsNullOrWhiteSpace(_config.Channel))
        {
            _currentChannel = _config.Channel;
            Log.Info($"Channel from config: {_currentChannel}");
            _ = ConnectAsync(_currentChannel);
        }
        else
        {
            Log.Info("No channel in config, showing nick form");
            ShowNickForm();
        }
    }

    private void ShowNickForm()
    {
        using var form = new NickForm(_currentChannel);
        if (form.ShowDialog() == DialogResult.OK && form.Result != null)
        {
            _config.SaveChannel(form.Result);
            _currentChannel = form.Result;
            _ = ConnectAsync(_currentChannel);
        }
    }

    private async Task ConnectAsync(string channel)
    {
        _statusItem.Text = $"Status: connecting to #{channel}...";
        _trayIcon.Text = $"beepbot - #{channel}";
        Log.Info($"Connecting to #{channel}...");

        try
        {
            await _irc.ConnectAsync(channel);
            _statusItem.Text = $"Status: #{channel}";
            Log.Info($"Connected to #{channel}");
        }
        catch (Exception ex)
        {
            _statusItem.Text = $"Status: error - {ex.Message}";
            _trayIcon.Text = "beepbot - error";
            Log.Error($"Connect failed: {ex.Message}");
        }
    }

    private void HandleMessage(string user, string text, string displayName, bool isBroadcaster, bool isMod)
    {
        var badgePrefix = isBroadcaster ? "[STREAMER] " :
                          isMod ? "[MOD] " : "";

        Log.Info($"{badgePrefix}{displayName}: {text}");

        Handlers.HandleMessage(user, text, isBroadcaster, isMod, _bot, _config);
    }

    private async void Exit()
    {
        await _irc.DisconnectAsync();
        _bot.Engine.Stop();
        Application.ExitThread();
    }

    private static Icon CreateTrayIcon()
    {
        var bmp = new Bitmap(32, 32);
        using var g = Graphics.FromImage(bmp);
        g.SmoothingMode = SmoothingMode.AntiAlias;
        g.Clear(Color.Transparent);

        using var brush = new SolidBrush(Color.FromArgb(145, 70, 255));
        g.FillEllipse(brush, 2, 2, 28, 28);

        using var font = new Font("Consolas", 14f, FontStyle.Bold);
        using var sf = new StringFormat { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center };
        g.DrawString("b", font, Brushes.White, new RectangleF(0, 0, 32, 32), sf);

        return Icon.FromHandle(bmp.GetHicon());
    }
}
