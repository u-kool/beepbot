using NAudio.Wave;

namespace TwitchIrcMinimal.Gui;

public class SettingsForm : Form
{
    private readonly TextBox _channelBox;
    private readonly Button _connectButton;
    private readonly Button _closeButton;
    private readonly string _placeholder = "Введите ник...";
    private readonly EnvConfig _config;

    public string? ChannelResult { get; private set; }

    private static readonly Color Bg = Color.FromArgb(22, 30, 62);
    private static readonly Color Surface = Color.FromArgb(35, 45, 80);
    private static readonly Color Txt = Color.FromArgb(220, 225, 240);
    private static readonly Color Accent = Color.FromArgb(232, 47, 77);
    private static readonly Color Hover = Color.FromArgb(115, 23, 38);

    public SettingsForm(string? currentChannel, int _, EnvConfig config)
    {
        _config = config;
        Text = "beepbot";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(340, 180);
        TopMost = true;
        ShowInTaskbar = false;
        BackColor = Bg;

        _closeButton = new Button
        {
            Text = "x",
            Location = new Point(305, 8),
            Size = new Size(28, 28),
            BackColor = Bg,
            ForeColor = Txt,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            Cursor = Cursors.Hand,
        };
        _closeButton.FlatAppearance.BorderSize = 0;
        _closeButton.Click += (_, _) =>
        {
            PlaySound(_config.SoundLeave);
            DialogResult = DialogResult.Cancel;
            Close();
        };

        var title = new Label
        {
            Text = "beepbot",
            Location = new Point(0, 20),
            Size = new Size(340, 40),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Accent,
            BackColor = Bg,
            Font = new Font("Segoe UI", 22f, FontStyle.Bold),
        };

        _channelBox = new TextBox
        {
            Location = new Point(30, 80),
            Size = new Size(280, 32),
            BackColor = Surface,
            ForeColor = Txt,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 12f),
            TextAlign = HorizontalAlignment.Center,
        };

        if (!string.IsNullOrEmpty(currentChannel))
        {
            _channelBox.Text = currentChannel;
        }
        else
        {
            ShowPlaceholder();
        }

        _channelBox.GotFocus += (_, _) =>
        {
            if (_channelBox.Text == _placeholder)
            {
                _channelBox.Text = "";
                _channelBox.ForeColor = Txt;
            }
        };

        _channelBox.LostFocus += (_, _) =>
        {
            if (string.IsNullOrWhiteSpace(_channelBox.Text))
                ShowPlaceholder();
        };

        _connectButton = new Button
        {
            Text = "Connect",
            Location = new Point(30, 125),
            Size = new Size(280, 36),
            BackColor = Accent,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            Cursor = Cursors.Hand,
        };
        _connectButton.FlatAppearance.BorderSize = 0;
        _connectButton.MouseEnter += (_, _) => _connectButton.BackColor = Hover;
        _connectButton.MouseLeave += (_, _) => _connectButton.BackColor = Accent;
        _connectButton.Click += (_, _) =>
        {
            var text = _channelBox.Text.Trim().TrimStart('#');
            if (string.IsNullOrWhiteSpace(text) || text == _placeholder)
            {
                MessageBox.Show("Введите имя канала.", "beepbot", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            PlaySound(_config.SoundConnect);
            ChannelResult = text.ToLowerInvariant();
            DialogResult = DialogResult.OK;
            Close();
        };

        Controls.AddRange(new Control[]
        {
            _closeButton, title, _channelBox, _connectButton
        });

        AcceptButton = _connectButton;

        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
        };

        _channelBox.Focus();
    }

    private void ShowPlaceholder()
    {
        _channelBox.Text = _placeholder;
        _channelBox.ForeColor = Color.FromArgb(120, 130, 160);
    }

    private void PlaySound(string fileName)
    {
        try
        {
            var path = Path.Combine(AppContext.BaseDirectory, "sounds", fileName);
            if (File.Exists(path))
            {
                var player = new AudioFileReader(path) { Volume = 0.5f };
                var output = new WaveOutEvent();
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
}
