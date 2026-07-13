using System.Diagnostics;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Runtime.InteropServices;

namespace TwitchIrcMinimal.Gui;

public class TrayForm : Form
{
    [DllImport("user32.dll")]
    private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    private const int HOTKEY_TRANSLATE = 1;
    private const int HOTKEY_MUTE = 2;
    private const int HOTKEY_SKIP = 3;
    private const int HOTKEY_STOP = 4;
    private const uint MOD_CONTROL = 0x0002;
    private const uint MOD_ALT = 0x0001;
    private const uint VK_T = 0x54;
    private const uint VK_M = 0x4D;
    private const int WM_HOTKEY = 0x0312;

    private readonly NotifyIcon _trayIcon;
    private readonly ContextMenuStrip _menu;
    private readonly ToolStripMenuItem _statusItem;
    private readonly ToolStripMenuItem _changeNickItem;
    private readonly ToolStripMenuItem _langMenu;
    private readonly ToolStripMenuItem _autoTranslateMenu;
    private readonly ToolStripMenuItem _muteItem;
    private readonly ToolStripMenuItem _erItem;
    private readonly ToolStripMenuItem _skipItem;
    private readonly ToolStripMenuItem _stopItem;
    private readonly ToolStripMenuItem _volMenu;
    private readonly ToolStripMenuItem _queueItem;

    private readonly EnvConfig _config;
    private readonly TwitchIrcClient _irc;
    private readonly Bot _bot;

    private string _currentChannel = "";

    private string _lastUser = "";
    private string _lastText = "";
    private string _lastDisplayName = "";
    private readonly HashSet<string> _autoTranslateUsers = new(StringComparer.OrdinalIgnoreCase);
    private readonly System.Collections.Concurrent.ConcurrentDictionary<string, string> _recentChatters = new(StringComparer.OrdinalIgnoreCase);
    private readonly Dictionary<string, ToolStripMenuItem> _langItems = new();

    private static readonly string[] LangCodes = { "ru", "en", "de", "fr", "es", "ja", "zh", "pt", "it", "pl", "uk", "tr", "ar", "ko" };
    private static readonly string[] LangNames = { "Русский", "English", "Deutsch", "Français", "Español", "日本語", "中文", "Português", "Italiano", "Polski", "Українська", "Türkçe", "العربية", "한국어" };

    public TrayForm()
    {
        Text = "beepbot";
        ShowInTaskbar = false;
        WindowState = FormWindowState.Minimized;
        Opacity = 0;
        Visible = false;

        _config = new EnvConfig();
        try { _config.Load(); }
        catch { }

        var ttsLanguages = TtsLanguages.Create();
        _bot = new Bot("", _config.Volume, ttsLanguages);
        var basePath = AppContext.BaseDirectory;
        _bot.Engine.LoadSounds(Path.Combine(basePath, "sounds"));

        if (!string.IsNullOrEmpty(_config.DeviceId))
            _bot.Engine.SetDevice(_config.DeviceId);

        _irc = new TwitchIrcClient();
        _irc.Log += msg => Log.Info($"IRC: {msg}");
        _irc.OnMessage += HandleMessage;

        Log.Info($"Sounds loaded: {_bot.Engine.GetSoundNames().Count}");
        Log.Info($"Volume: {_config.Volume}");

        _statusItem = new ToolStripMenuItem("Status: disconnected") { ToolTipText = "Нажмите для смены канала" };
        _statusItem.Click += (_, _) => ShowNickForm();
        _changeNickItem = new ToolStripMenuItem("Сменить ник...");
        var exitItem = new ToolStripMenuItem("Выход");

        _changeNickItem.Click += (_, _) => ShowNickForm();
        _changeNickItem.ToolTipText = "Изменить имя канала для подключения";
        var openSounds = new ToolStripMenuItem("Открыть папку sounds") { ToolTipText = "Открывает проводник со звуками" };
        openSounds.Click += (_, _) => ShowSoundBrowser();
        var openLog = new ToolStripMenuItem("Логи") { ToolTipText = "Открывает папку с логами бота" };
        openLog.Click += (_, _) => { try { Process.Start("explorer.exe", Path.Combine(AppContext.BaseDirectory, "beepbot.log")); } catch { } };
        exitItem.Click += (_, _) => Exit();

        _queueItem = new ToolStripMenuItem("Очередь: OFF") { ToolTipText = "Включает/выключает очередь воспроизведения звуков" };
        _queueItem.Click += (_, _) =>
        {
            _bot.Player.QueueEnabled = !_bot.Player.QueueEnabled;
            _bot.QueueEnabled = _bot.Player.QueueEnabled;
        };
        _bot.QueueChanged += () =>
        {
            if (InvokeRequired) BeginInvoke(() => _queueItem.Text = _bot.QueueEnabled ? "Очередь: ON" : "Очередь: OFF");
            else _queueItem.Text = _bot.QueueEnabled ? "Очередь: ON" : "Очередь: OFF";
        };

        var deviceMenu = BuildDeviceMenu();
        _langMenu = BuildLangMenu();
        _autoTranslateMenu = BuildAutoTranslateMenu();

        _muteItem = new ToolStripMenuItem("Звук: ON (Ctrl+M)") { ToolTipText = "Включает/выключает все звуки бота" };
        _muteItem.Click += (_, _) => ToggleMute();

        _erItem = new ToolStripMenuItem("Ear Safety: ON") { ToolTipText = "Включает/выключает защиту ушей (ограничение громкости)" };
        _erItem.Click += (_, _) => ToggleEr();

        _skipItem = new ToolStripMenuItem("Skip (Alt+M)") { ToolTipText = "Пропускает текущий звук в очереди" };
        _skipItem.Click += (_, _) => SkipCurrent();

        _stopItem = new ToolStripMenuItem("Stop (Ctrl+Alt+M)") { ToolTipText = "Полностью останавливает воспроизведение" };
        _stopItem.Click += (_, _) => StopPlayback();

        _volMenu = BuildVolMenu();

        var settingsMenu = new ToolStripMenuItem("Настройки") { ToolTipText = "Настройки бота" };
        settingsMenu.DropDownItems.Add(_changeNickItem);
        settingsMenu.DropDownItems.Add(deviceMenu);
        settingsMenu.DropDownItems.Add(_langMenu);
        settingsMenu.DropDownItems.Add(openSounds);
        settingsMenu.DropDownItems.Add(openLog);

        _menu = new ContextMenuStrip();
        _menu.Items.Add(_statusItem);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(_muteItem);
        _menu.Items.Add(_erItem);
        _menu.Items.Add(_skipItem);
        _menu.Items.Add(_stopItem);
        _menu.Items.Add(_volMenu);
        _menu.Items.Add(_queueItem);
        _menu.Items.Add(_autoTranslateMenu);
        _menu.Items.Add(settingsMenu);
        _menu.Items.Add(new ToolStripSeparator());
        _menu.Items.Add(exitItem);

        _trayIcon = new NotifyIcon
        {
            Text = "beepbot",
            Icon = CreateTrayIcon(),
            ContextMenuStrip = _menu,
            Visible = true,
        };
        _trayIcon.MouseClick += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                ShowSoundBrowser();
            }
            else if (e.Button == MouseButtons.Middle)
            {
                ShowNickForm();
            }
        };

        FormClosed += (_, _) =>
        {
            _trayIcon.Visible = false;
            UnregisterHotKey(Handle, HOTKEY_TRANSLATE);
            UnregisterHotKey(Handle, HOTKEY_MUTE);
            UnregisterHotKey(Handle, HOTKEY_SKIP);
            UnregisterHotKey(Handle, HOTKEY_STOP);
        };

        RegisterHotKey(Handle, HOTKEY_TRANSLATE, MOD_CONTROL, VK_T);
        RegisterHotKey(Handle, HOTKEY_MUTE, MOD_CONTROL, VK_M);
        RegisterHotKey(Handle, HOTKEY_SKIP, MOD_ALT, VK_M);
        RegisterHotKey(Handle, HOTKEY_STOP, MOD_CONTROL | MOD_ALT, VK_M);

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

    protected override void WndProc(ref Message m)
    {
        if (m.Msg == WM_HOTKEY)
        {
            int hotkeyId = m.WParam.ToInt32();
            if (hotkeyId == HOTKEY_TRANSLATE)
                TranslateLastMessage();
            else if (hotkeyId == HOTKEY_MUTE)
                ToggleMute();
            else if (hotkeyId == HOTKEY_SKIP)
                SkipCurrent();
            else if (hotkeyId == HOTKEY_STOP)
                StopPlayback();
        }
        base.WndProc(ref m);
    }

    private void SkipCurrent()
    {
        _bot.Player.Skip();
        Log.Info("[SKIP]");
    }

    private void StopPlayback()
    {
        _bot.Player.Stop();
        Log.Info("[STOP]");
    }

    private void ToggleMute()
    {
        _bot.SetMuted(!_bot.IsMuted);
        string text = _bot.IsMuted ? "Звук: OFF (Ctrl+M)" : "Звук: ON (Ctrl+M)";
        if (InvokeRequired) BeginInvoke(() => _muteItem.Text = text);
        else _muteItem.Text = text;
        Log.Info($"[MUTE] {(_bot.IsMuted ? "ON" : "OFF")}");
    }

    private void ToggleEr()
    {
        _bot.ErEnabled = !_bot.ErEnabled;
        _bot.Engine.SetErEnabled(_bot.ErEnabled);
        string text = _bot.ErEnabled ? "Ear Safety: ON" : "Ear Safety: OFF";
        if (InvokeRequired) BeginInvoke(() => _erItem.Text = text);
        else _erItem.Text = text;
        Log.Info($"[ER] {(_bot.ErEnabled ? "ON" : "OFF")}");
    }

    private ToolStripMenuItem BuildVolMenu()
    {
        var menu = new ToolStripMenuItem($"Громкость: {_bot.Volume}%") { ToolTipText = "Настройка громкости звуков бота (0-200%)" };
        for (int pct = 0; pct <= 100; pct += 10)
        {
            var p = pct;
            var item = new ToolStripMenuItem($"{p}%") { Checked = _bot.Volume == p, Tag = p };
            item.Click += (_, _) => SetVolume(p);
            menu.DropDownItems.Add(item);
        }
        menu.DropDownItems.Add(new ToolStripSeparator());
        var customItem = new ToolStripMenuItem("Своя...") { ToolTipText = "Введите значение от 0 до 200" };
        customItem.Click += (_, _) =>
        {
            var form = new VolumeForm(_bot.Volume);
            if (form.ShowDialog() == DialogResult.OK)
                SetVolume(form.VolumeResult);
        };
        menu.DropDownItems.Add(customItem);
        return menu;
    }

    private void SetVolume(int vol)
    {
        vol = Math.Clamp(vol, 0, 200);
        _bot.Volume = vol;
        _config.SaveVolume(vol);
        RefreshVolMenu();
        Log.Info($"[VOL] {vol}%");
    }

    private void RefreshVolMenu()
    {
        _volMenu.Text = $"Громкость: {_bot.Volume}%";
        foreach (var item in _volMenu.DropDownItems)
        {
            if (item is ToolStripMenuItem mi && mi.Tag is int pct)
                mi.Checked = pct == _bot.Volume;
        }
    }

    private void TranslateLastMessage()
    {
        if (string.IsNullOrEmpty(_lastText)) return;

        if (!string.IsNullOrEmpty(_lastUser))
        {
            bool alreadyAdded = _autoTranslateUsers.Contains(_lastUser);
            if (!alreadyAdded)
            {
                _autoTranslateUsers.Add(_lastUser);
                Log.Info($"[AUTO-TRANSLATE+] {_lastDisplayName} ({_lastUser})");
                if (InvokeRequired) BeginInvoke(RefreshAutoTranslateMenu);
                else RefreshAutoTranslateMenu();
            }
            else
            {
                Log.Info($"[AUTO-TRANSLATE] {_lastDisplayName} ({_lastUser}) уже в списке");
            }
        }

        var lang = _config.TranslateLang;
        Log.Info($"[TRANSLATE] {_lastDisplayName}: {_lastText} -> {lang}");
        _ = TranslateAndPlay(_lastText, lang);
    }

    private async Task TranslateAndPlay(string text, string lang)
    {
        try
        {
            var translated = await Translator.TranslateAsync(lang, text);
            if (!string.IsNullOrEmpty(translated))
            {
                Log.Info($"[TRANSLATE] -> {translated}");
                var ttsLang = TtsLanguages.Create().Values.FirstOrDefault(l => l.StartsWith(lang)) ?? $"{lang}-{lang.ToUpper()}";
                var key = _bot.NextTtsKey();
                var format = _bot.Engine.GetDefaultFormat();
                var samples = await GoogleTtsConverter.GetTtsAsync(ttsLang, translated, format);
                if (samples != null)
                {
                    _bot.Engine.AddSamples(key, samples);
                    var cmd = new PlayCommand { SoundNames = key };
                    _bot.Player.Play(cmd, _bot.Volume);
                }
            }
        }
        catch (Exception ex)
        {
            Log.Error($"[TRANSLATE] error: {ex}");
        }
    }

    private void HandleMessage(string user, string text, string displayName, bool isBroadcaster, bool isMod)
    {
        var badgePrefix = isBroadcaster ? "[STREAMER] " :
                          isMod ? "[MOD] " : "";

        Log.Info($"{badgePrefix}{displayName}: {text}");

        _lastUser = user;
        _lastText = text;
        _lastDisplayName = displayName;

        bool isNew = !_recentChatters.ContainsKey(user);
        if (isNew) _recentChatters[user] = displayName;

        if (_autoTranslateUsers.Contains(user) && !text.StartsWith('!'))
        {
            var lang = _config.TranslateLang;
            Log.Info($"[AUTO-TRANSLATE] {displayName}: {text} -> {lang}");
            _ = TranslateAndPlay(text, lang);
        }

        Handlers.HandleMessage(user, text, isBroadcaster, isMod, _bot, _config);
    }

    private void ShowNickForm()
    {
        var form = new SettingsForm(_currentChannel, _bot.Volume, _config);
        if (form.ShowDialog() == DialogResult.OK)
        {
            if (!string.IsNullOrEmpty(form.ChannelResult))
            {
                _config.SaveChannel(form.ChannelResult);
                _currentChannel = form.ChannelResult;
                _ = ConnectAsync(_currentChannel);
            }
        }
    }

    private void ShowSoundBrowser()
    {
        var soundsPath = Path.Combine(AppContext.BaseDirectory, "sounds");
        using var form = new SoundBrowserForm(soundsPath);
        form.ShowDialog();
    }

    private ToolStripMenuItem BuildDeviceMenu()
    {
        var devices = AudioEngine.GetOutputDevices();
        var menu = new ToolStripMenuItem("Устройство аудиовывода") { ToolTipText = "Выберите устройство для воспроизведения звуков" };
        var currentDeviceId = _config.DeviceId;

        var defaultItem = new ToolStripMenuItem("Default") { Checked = string.IsNullOrEmpty(currentDeviceId) };
        defaultItem.Click += (_, _) =>
        {
            _bot.Engine.SetDevice(null);
            _config.SaveDevice(null);
            RefreshDeviceMenu(menu);
        };
        menu.DropDownItems.Add(defaultItem);

        if (devices.Count > 0)
            menu.DropDownItems.Add(new ToolStripSeparator());

        foreach (var (id, name) in devices)
        {
            var item = new ToolStripMenuItem(name) { Checked = id == currentDeviceId, Tag = id };
            item.Click += (_, _) =>
            {
                _bot.Engine.SetDevice(id);
                _config.SaveDevice(id);
                RefreshDeviceMenu(menu);
            };
            menu.DropDownItems.Add(item);
        }

        return menu;
    }

    private void RefreshDeviceMenu(ToolStripMenuItem menu)
    {
        var currentDeviceId = _config.DeviceId;
        foreach (var item in menu.DropDownItems)
        {
            if (item is ToolStripMenuItem mi)
            {
                if (mi.Tag is string id)
                    mi.Checked = id == currentDeviceId;
                else
                    mi.Checked = string.IsNullOrEmpty(currentDeviceId);
            }
        }
    }

    private ToolStripMenuItem BuildLangMenu()
    {
        var menu = new ToolStripMenuItem("Язык перевода (Ctrl+T)") { ToolTipText = "Выберите язык для автоматического перевода сообщений" };
        for (int i = 0; i < LangCodes.Length; i++)
        {
            var code = LangCodes[i];
            var name = LangNames[i];
            var item = new ToolStripMenuItem(name) { Checked = code == _config.TranslateLang, Tag = code };
            item.Click += (_, _) =>
            {
                _config.SaveTranslateLang(code);
                RefreshLangMenu(menu);
            };
            _langItems[code] = item;
            menu.DropDownItems.Add(item);
        }
        return menu;
    }

    private void RefreshLangMenu(ToolStripMenuItem menu)
    {
        var current = _config.TranslateLang;
        foreach (var item in menu.DropDownItems)
        {
            if (item is ToolStripMenuItem mi && mi.Tag is string code)
                mi.Checked = code == current;
        }
    }

    private ToolStripMenuItem BuildAutoTranslateMenu()
    {
        var menu = new ToolStripMenuItem("Авто-перевод пользователей") { ToolTipText = "Выберите пользователей для автоматического перевода сообщений" };
        menu.DropDownItems.Add(new ToolStripMenuItem("(нет чаттеров)") { Enabled = false });
        return menu;
    }

    private void RefreshAutoTranslateMenu()
    {
        _autoTranslateMenu.DropDownItems.Clear();

        var snapshot = new Dictionary<string, string>(_recentChatters);

        if (snapshot.Count == 0)
        {
            _autoTranslateMenu.DropDownItems.Add(new ToolStripMenuItem("(нет чаттеров)") { Enabled = false });
            return;
        }

        foreach (var kvp in snapshot.Reverse())
        {
            var user = kvp.Key;
            var displayName = kvp.Value;
            var enabled = _autoTranslateUsers.Contains(user);
            var item = new ToolStripMenuItem(displayName) { Checked = enabled, Tag = user };
            item.Click += (_, _) =>
            {
                if (_autoTranslateUsers.Contains(user))
                {
                    _autoTranslateUsers.Remove(user);
                }
                else
                {
                    bool duplicate = false;
                    foreach (var existing in _autoTranslateUsers)
                    {
                        if (snapshot.TryGetValue(existing, out var existingName) &&
                            string.Equals(existingName, displayName, StringComparison.OrdinalIgnoreCase))
                        {
                            duplicate = true;
                            break;
                        }
                    }
                    if (!duplicate)
                        _autoTranslateUsers.Add(user);
                }
                RefreshAutoTranslateMenu();
            };
            _autoTranslateMenu.DropDownItems.Add(item);
        }
    }

    private async Task ConnectAsync(string channel)
    {
        _statusItem.Text = $"Status: connecting to {channel}...";
        _trayIcon.Text = $"beepbot - {channel}";
        Log.Info($"Connecting to {channel}...");

        try
        {
            await _irc.ConnectAsync(channel);
            _statusItem.Text = $"Status: {channel}";
            Log.Info($"Connected to #{channel}");
            PlayConnectedSound();
        }
        catch (Exception ex)
        {
            _statusItem.Text = $"Status: error - {ex.Message}";
            _trayIcon.Text = "beepbot - error";
            Log.Error($"Connect failed: {ex.Message}");
            PlayErrorSound();
            if (InvokeRequired) BeginInvoke(ShowNickForm);
            else ShowNickForm();
        }
    }

    private void PlayErrorSound()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "sounds", _config.SoundError);
            if (File.Exists(path))
            {
                var player = new NAudio.Wave.AudioFileReader(path) { Volume = 0.5f };
                var output = new NAudio.Wave.WaveOutEvent();
                output.Init(player);
                output.PlaybackStopped += (_, _) =>
                {
                    output.Dispose();
                    player.Dispose();
                };
                output.Play();
            }
        }
        catch { }
    }

    private void PlayConnectedSound()
    {
        var cmd = _config.SoundConnected;
        if (string.IsNullOrWhiteSpace(cmd)) return;

        if (cmd.EndsWith(".wav", StringComparison.OrdinalIgnoreCase))
        {
            try
            {
                var path = Path.Combine(AppContext.BaseDirectory, "sounds", cmd);
                if (File.Exists(path))
                {
                    var player = new NAudio.Wave.AudioFileReader(path) { Volume = 0.5f };
                    var output = new NAudio.Wave.WaveOutEvent();
                    output.Init(player);
                    output.PlaybackStopped += (_, _) => { output.Dispose(); player.Dispose(); };
                    output.Play();
                }
            }
            catch { }
        }
        else
        {
            cmd = cmd.Replace("{CHANNEL}", _currentChannel);
            Handlers.HandleMessage("system", cmd, true, false, _bot, _config);
        }
    }

    private async void Exit()
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "sounds", _config.SoundLeave);
            if (File.Exists(path))
            {
                var player = new NAudio.Wave.AudioFileReader(path) { Volume = 0.5f };
                var output = new NAudio.Wave.WaveOutEvent();
                output.Init(player);
                var tcs = new TaskCompletionSource();
                output.PlaybackStopped += (_, _) =>
                {
                    output.Dispose();
                    player.Dispose();
                    tcs.TrySetResult();
                };
                output.Play();
                await tcs.Task.WaitAsync(TimeSpan.FromSeconds(3));
            }
        }
        catch { }
        await _irc.DisconnectAsync();
        _bot.Engine.Stop();
        Close();
    }

    private static Icon CreateTrayIcon()
    {
        var asm = typeof(TrayForm).Assembly;
        using var stream = asm.GetManifestResourceStream("48.ico");
        if (stream != null) return new Icon(stream);

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
