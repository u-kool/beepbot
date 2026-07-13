namespace TwitchIrcMinimal.Gui;

public class NickForm : Form
{
    private readonly TextBox _textBox;
    private readonly Button _okButton;
    private readonly Button _cancelButton;
    private readonly Label _label;

    public string? Result { get; private set; }

    public NickForm(string? currentChannel = null)
    {
        Text = "beepbot";
        FormBorderStyle = FormBorderStyle.FixedToolWindow;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(320, 110);
        TopMost = true;
        ShowInTaskbar = false;
        KeyPreview = true;

        _label = new Label
        {
            Text = "Twitch channel:",
            Location = new Point(12, 15),
            AutoSize = true,
            Font = new Font("Segoe UI", 9f),
        };

        _textBox = new TextBox
        {
            Location = new Point(12, 40),
            Size = new Size(296, 23),
            Font = new Font("Segoe UI", 9.5f),
            Text = currentChannel ?? "",
        };

        _okButton = new Button
        {
            Text = "OK",
            Location = new Point(168, 72),
            Size = new Size(70, 28),
            DialogResult = DialogResult.OK,
            Font = new Font("Segoe UI", 9f),
        };

        _cancelButton = new Button
        {
            Text = "Cancel",
            Location = new Point(248, 72),
            Size = new Size(70, 28),
            DialogResult = DialogResult.Cancel,
            Font = new Font("Segoe UI", 9f),
        };

        Controls.Add(_label);
        Controls.Add(_textBox);
        Controls.Add(_okButton);
        Controls.Add(_cancelButton);

        AcceptButton = _okButton;
        CancelButton = _cancelButton;

        _okButton.Click += (_, _) =>
        {
            var text = _textBox.Text.Trim().TrimStart('#');
            if (string.IsNullOrWhiteSpace(text))
            {
                MessageBox.Show("Enter a channel name.", "beepbot", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
            Result = text.ToLowerInvariant();
            DialogResult = DialogResult.OK;
            Close();
        };

        _cancelButton.Click += (_, _) =>
        {
            DialogResult = DialogResult.Cancel;
            Close();
        };

        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape)
            {
                DialogResult = DialogResult.Cancel;
                Close();
            }
        };

        _textBox.SelectAll();
        _textBox.Focus();
    }
}
