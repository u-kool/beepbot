namespace TwitchIrcMinimal.Gui;

public class VolumeForm : Form
{
    private readonly TextBox _volumeBox;
    private readonly TrackBar _slider;
    private readonly Button _okButton;
    private readonly Button _closeButton;
    private bool _updating;

    public int VolumeResult { get; private set; }

    private static readonly Color Bg = Color.FromArgb(22, 30, 62);
    private static readonly Color Surface = Color.FromArgb(35, 45, 80);
    private static readonly Color Txt = Color.FromArgb(220, 225, 240);
    private static readonly Color Accent = Color.FromArgb(232, 47, 77);
    private static readonly Color Hover = Color.FromArgb(115, 23, 38);

    public VolumeForm(int currentVolume)
    {
        Text = "beepbot";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(280, 210);
        TopMost = true;
        ShowInTaskbar = false;
        BackColor = Bg;

        _closeButton = new Button
        {
            Text = "x",
            Location = new Point(245, 8),
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
            DialogResult = DialogResult.Cancel;
            Close();
        };

        var title = new Label
        {
            Text = "Громкость",
            Location = new Point(0, 20),
            Size = new Size(280, 36),
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Accent,
            BackColor = Bg,
            Font = new Font("Segoe UI", 18f, FontStyle.Bold),
        };

        _volumeBox = new TextBox
        {
            Location = new Point(40, 75),
            Size = new Size(200, 32),
            BackColor = Surface,
            ForeColor = Txt,
            BorderStyle = BorderStyle.FixedSingle,
            Font = new Font("Segoe UI", 14f),
            TextAlign = HorizontalAlignment.Center,
            MaxLength = 3,
            Text = currentVolume.ToString(),
        };
        _volumeBox.KeyPress += (_, e) =>
        {
            if (!char.IsDigit(e.KeyChar) && e.KeyChar != '\b')
                e.Handled = true;
        };
        _volumeBox.TextChanged += (_, _) =>
        {
            if (_updating) return;
            if (int.TryParse(_volumeBox.Text, out int v))
                _slider.Value = Math.Clamp(v, 0, 200);
        };

        _slider = new TrackBar
        {
            Minimum = 0,
            Maximum = 200,
            Value = currentVolume,
            TickFrequency = 10,
            SmallChange = 10,
            LargeChange = 20,
            Location = new Point(20, 112),
            Size = new Size(240, 45),
            BackColor = Bg,
            ForeColor = Accent,
        };
        _slider.ValueChanged += (_, _) =>
        {
            if (_updating) return;
            _updating = true;
            _volumeBox.Text = _slider.Value.ToString();
            _updating = false;
        };

        _okButton = new Button
        {
            Text = "OK",
            Location = new Point(40, 160),
            Size = new Size(200, 32),
            BackColor = Accent,
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            Cursor = Cursors.Hand,
        };
        _okButton.FlatAppearance.BorderSize = 0;
        _okButton.MouseEnter += (_, _) => _okButton.BackColor = Hover;
        _okButton.MouseLeave += (_, _) => _okButton.BackColor = Accent;
        _okButton.Click += (_, _) =>
        {
            if (int.TryParse(_volumeBox.Text, out int vol) && vol >= 0 && vol <= 200)
            {
                VolumeResult = vol;
                DialogResult = DialogResult.OK;
                Close();
            }
            else
            {
                MessageBox.Show("Введите число от 0 до 200.", "beepbot", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        };

        Controls.AddRange(new Control[]
        {
            _closeButton, title, _volumeBox, _slider, _okButton
        });

        AcceptButton = _okButton;

        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
        };

        _volumeBox.SelectAll();
        _volumeBox.Focus();
    }
}
