using System.Runtime.InteropServices;
using NAudio.Wave;

namespace TwitchIrcMinimal.Gui;

public class SoundBrowserForm : Form
{
    private readonly string _soundsPath;
    private WaveOutEvent? _currentOutput;
    private AudioFileReader? _currentReader;
    private string? _currentPlaying;
    private readonly List<string> _fileNames = new();
    private readonly HashSet<int> _selected = new();
    private readonly DarkScrollPanel _listPanel;
    private readonly Panel _dragOverlay;
    private readonly Button _closeButton;
    private readonly Button _infoButton;
    private readonly Button _deleteButton;
    private readonly ToolTip _toolTip = new();
    private ContextMenuStrip? _ctxMenu;
    private int _contextTargetIndex = -1;

    private static readonly Color Bg = Color.FromArgb(22, 30, 62);
    private static readonly Color Surface = Color.FromArgb(35, 45, 80);
    private static readonly Color Txt = Color.FromArgb(220, 225, 240);
    private static readonly Color Accent = Color.FromArgb(232, 47, 77);

    [DllImport("user32.dll")]
    private static extern int SendMessage(IntPtr hWnd, int Msg, int wParam, int lParam);
    [DllImport("user32.dll")]
    private static extern int ReleaseCapture();

    private const int WM_NCLBUTTONDOWN = 0xA1;
    private const int HT_CAPTION = 0x2;

    public SoundBrowserForm(string soundsPath)
    {
        _soundsPath = soundsPath ?? AppContext.BaseDirectory;
        var realPath = Path.Combine(_soundsPath, "sounds");
        if (Directory.Exists(realPath))
            _soundsPath = realPath;

        Text = "beepbot";
        FormBorderStyle = FormBorderStyle.None;
        StartPosition = FormStartPosition.CenterScreen;
        ClientSize = new Size(340, 460);
        TopMost = true;
        ShowInTaskbar = false;
        BackColor = Bg;
        AllowDrop = true;
        DoubleBuffered = true;
        KeyPreview = true;

        _closeButton = new Button
        {
            Text = "x",
            Dock = DockStyle.Right,
            Width = 44,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Txt,
            BackColor = Bg,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            Cursor = Cursors.Hand,
        };
        _closeButton.FlatAppearance.BorderSize = 0;
        _closeButton.Click += (_, _) => { StopPlayback(); Close(); };

        _infoButton = new Button
        {
            Text = "\u2139",
            Dock = DockStyle.Right,
            Width = 40,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Txt,
            BackColor = Bg,
            Font = new Font("Segoe UI", 11f),
            Cursor = Cursors.Help,
        };
        _infoButton.FlatAppearance.BorderSize = 0;
        _toolTip.SetToolTip(_infoButton, "Перетащите .wav/.mp3 файлы сюда");

        _deleteButton = new Button
        {
            Text = "\U0001F5D1",
            Dock = DockStyle.Right,
            Width = 40,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Accent,
            BackColor = Bg,
            Font = new Font("Segoe UI", 11f),
            Cursor = Cursors.Hand,
            Visible = false,
        };
        _deleteButton.FlatAppearance.BorderSize = 0;
        _deleteButton.Click += (_, _) => DeleteSelected();

        var buttonsBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 28,
            BackColor = Bg,
        };
        buttonsBar.Controls.Add(_deleteButton);
        buttonsBar.Controls.Add(_infoButton);
        buttonsBar.Controls.Add(_closeButton);

        var titleLabel = new Label
        {
            Text = "Звуки",
            Dock = DockStyle.Top,
            Height = 32,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Accent,
            BackColor = Bg,
            Font = new Font("Segoe UI", 15f, FontStyle.Bold),
        };

        var titleBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 72,
            BackColor = Bg,
        };

        Action startDrag = () =>
        {
            ReleaseCapture();
            SendMessage(Handle, WM_NCLBUTTONDOWN, HT_CAPTION, 0);
        };
        titleBar.MouseDown += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                if (_selected.Count > 0) ClearSelection();
                else startDrag();
            }
        };
        titleLabel.MouseDown += (_, e) =>
        {
            if (e.Button == MouseButtons.Left)
            {
                if (_selected.Count > 0) ClearSelection();
                else startDrag();
            }
        };

        titleBar.Controls.Add(titleLabel);
        titleBar.Controls.Add(buttonsBar);

        _listPanel = new DarkScrollPanel
        {
            Dock = DockStyle.Fill,
            BackColor = Surface,
        };
        _listPanel.ItemClicked += OnItemClick;
        _listPanel.ItemRightClicked += OnItemRightClick;

        _dragOverlay = new Panel
        {
            BackColor = Color.FromArgb(160, Bg.R, Bg.G, Bg.B),
            Visible = false,
            AllowDrop = true,
            Size = ClientSize,
            Location = Point.Empty,
        };
        var dragLabel = new Label
        {
            Text = "Перетащите .wav/.mp3 файлы сюда",
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleCenter,
            ForeColor = Txt,
            BackColor = Color.Transparent,
            Font = new Font("Segoe UI", 15f, FontStyle.Bold),
        };
        _dragOverlay.Controls.Add(dragLabel);
        _dragOverlay.DragEnter += (_, e) =>
        {
            e.Effect = e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop)
                ? DragDropEffects.Copy : DragDropEffects.None;
        };
        _dragOverlay.DragDrop += (_, e) => { _dragOverlay.Visible = false; HandleDrop(e); };
        _dragOverlay.DragLeave += (_, _) =>
        {
            if (!ClientRectangle.Contains(PointToClient(Cursor.Position)))
                _dragOverlay.Visible = false;
        };

        Controls.Add(_listPanel);
        Controls.Add(titleBar);
        Controls.Add(_dragOverlay);

        DragEnter += (_, e) =>
        {
            if (e.Data != null && e.Data.GetDataPresent(DataFormats.FileDrop))
            {
                e.Effect = DragDropEffects.Copy;
                _dragOverlay.Visible = true;
                _dragOverlay.BringToFront();
            }
            else e.Effect = DragDropEffects.None;
        };
        DragDrop += (_, e) => { _dragOverlay.Visible = false; HandleDrop(e); };
        DragLeave += (_, _) =>
        {
            if (!ClientRectangle.Contains(PointToClient(Cursor.Position)))
                _dragOverlay.Visible = false;
        };

        KeyDown += (_, e) =>
        {
            if (e.KeyCode == Keys.Escape) { StopPlayback(); Close(); }
            if (e.KeyCode == Keys.Delete && _selected.Count > 0) DeleteSelected();
        };

        BuildContextMenu();
        LoadSounds();
    }

    private void BuildContextMenu()
    {
        _ctxMenu = new ContextMenuStrip();
        var select = new ToolStripMenuItem("Выбрать");
        select.Click += (_, _) =>
        {
            if (_contextTargetIndex >= 0) ToggleSelect(_contextTargetIndex);
        };
        var rename = new ToolStripMenuItem("Переименовать");
        rename.Click += (_, _) => RenameSelected();
        var delete = new ToolStripMenuItem("Удалить");
        delete.Click += (_, _) =>
        {
            if (_contextTargetIndex >= 0 && !_selected.Contains(_contextTargetIndex))
                ToggleSelect(_contextTargetIndex);
            DeleteSelected();
        };
        _ctxMenu.Items.AddRange(new ToolStripItem[] { select, rename, delete });
        _ctxMenu.BackColor = Surface;
        _ctxMenu.ForeColor = Txt;
        _ctxMenu.Renderer = new DarkMenuRenderer();
    }

    private void LoadSounds()
    {
        _fileNames.Clear();
        _selected.Clear();
        _currentPlaying = null;
        _deleteButton.Visible = false;
        _contextTargetIndex = -1;

        try
        {
            if (!Directory.Exists(_soundsPath)) return;

            _fileNames.AddRange(
                Directory.GetFiles(_soundsPath, "*.*", SearchOption.TopDirectoryOnly)
                    .Where(f => f.EndsWith(".wav", StringComparison.OrdinalIgnoreCase) ||
                                f.EndsWith(".mp3", StringComparison.OrdinalIgnoreCase))
                    .Select(f => Path.GetFileName(f)!)
                    .OrderBy(f => f));
        }
        catch { }

        _listPanel.SetItemCount(_fileNames.Count);
        _listPanel.FileNames = _fileNames;
        _listPanel.SelectedIndices = _selected;
        _listPanel.CurrentPlaying = _currentPlaying;
    }

    private void OnItemClick(int index)
    {
        if (index < 0 || index >= _fileNames.Count) return;
        TogglePlay(_fileNames[index]);
        if (Control.ModifierKeys.HasFlag(Keys.Control) || _selected.Count > 0)
            ToggleSelect(index);
    }

    private void OnItemRightClick(int index, Point screenPos)
    {
        if (index < 0 || index >= _fileNames.Count) return;
        _contextTargetIndex = index;
        if (!_selected.Contains(index))
        {
            ClearSelection();
            ToggleSelect(index);
        }
        _ctxMenu?.Show(_listPanel, screenPos);
    }

    private void ToggleSelect(int index)
    {
        if (_selected.Contains(index)) _selected.Remove(index);
        else _selected.Add(index);
        _deleteButton.Visible = _selected.Count > 0;
        _listPanel.Invalidate();
    }

    private void ClearSelection()
    {
        _selected.Clear();
        _deleteButton.Visible = false;
        _listPanel.Invalidate();
    }

    private void TogglePlay(string fileName)
    {
        if (_currentPlaying == fileName) { StopPlayback(); return; }

        StopPlayback();
        var path = Path.Combine(_soundsPath, fileName);
        if (!File.Exists(path)) return;
        try
        {
            _currentReader = new AudioFileReader(path) { Volume = 0.5f };
            _currentOutput = new WaveOutEvent();
            _currentOutput.Init(_currentReader);
            _currentPlaying = fileName;
            _listPanel.CurrentPlaying = _currentPlaying;
            var self = _currentOutput;
            _listPanel.Invalidate();
            _currentOutput.Play();
            _currentOutput.PlaybackStopped += (_, _) =>
            {
                if (_currentOutput != self) return;
                if (InvokeRequired) BeginInvoke(StopPlayback);
                else StopPlayback();
            };
        }
        catch { }
    }

    private void StopPlayback()
    {
        try { _currentOutput?.Stop(); _currentOutput?.Dispose(); _currentReader?.Dispose(); }
        catch { }
        _currentOutput = null;
        _currentReader = null;
        _currentPlaying = null;
        _listPanel.CurrentPlaying = null;
        _listPanel.Invalidate();
    }

    private void HandleDrop(DragEventArgs e)
    {
        if (e.Data == null) return;
        var files = e.Data.GetData(DataFormats.FileDrop) as string[];
        if (files == null) return;
        foreach (var f in files)
        {
            if (string.IsNullOrEmpty(f)) continue;
            var ext = Path.GetExtension(f).ToLowerInvariant();
            if (ext is ".wav" or ".mp3")
            {
                var dest = Path.Combine(_soundsPath, Path.GetFileName(f));
                if (!File.Exists(dest)) File.Copy(f, dest);
            }
        }
        LoadSounds();
    }

    private void RenameSelected()
    {
        if (_contextTargetIndex < 0 || _contextTargetIndex >= _fileNames.Count) return;
        var oldName = _fileNames[_contextTargetIndex];
        var ext = Path.GetExtension(oldName);
        var nameWithoutExt = Path.GetFileNameWithoutExtension(oldName);
        var result = InputDialog.Show("Переименовать", "Новое имя:", nameWithoutExt);
        if (string.IsNullOrWhiteSpace(result) || result == nameWithoutExt) return;
        var oldPath = Path.Combine(_soundsPath, oldName);
        var newPath = Path.Combine(_soundsPath, result + ext);
        if (File.Exists(oldPath) && !File.Exists(newPath))
        {
            try { File.Move(oldPath, newPath); } catch { }
            LoadSounds();
        }
    }

    private void DeleteSelected()
    {
        if (_selected.Count == 0) return;
        var names = _selected.Where(i => i >= 0 && i < _fileNames.Count).Select(i => _fileNames[i]).ToList();
        if (MessageBox.Show($"Удалить {names.Count} файл(ов)?", "Удаление",
            MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes) return;
        foreach (var name in names)
        {
            if (_currentPlaying == name) StopPlayback();
            try { File.Delete(Path.Combine(_soundsPath, name)); } catch { }
        }
        LoadSounds();
    }

    protected override void OnResize(EventArgs e)
    {
        base.OnResize(e);
        if (_dragOverlay != null) _dragOverlay.Size = ClientSize;
        if (_listPanel != null) _listPanel.Invalidate();
    }

    protected override void OnFormClosing(FormClosingEventArgs e)
    {
        StopPlayback();
        _fileNames.Clear();
        _toolTip.Dispose();
        base.OnFormClosing(e);
    }
}

internal class DarkScrollPanel : Panel
{
    private int _scrollY;
    private int _itemCount;
    private int _hoverIndex = -1;
    private int _dragStartY;
    private int _dragStartScroll;

    public const int ITEM_H = 32;
    private const int SCROLLBAR_W = 5;
    private const int SCROLLBAR_MIN_H = 30;

    private static readonly Color TrackColor = Color.FromArgb(22, 30, 62);
    private static readonly Color ThumbColor = Color.FromArgb(90, 95, 130);
    private static readonly Color ThumbHoverColor = Color.FromArgb(130, 135, 170);
    private static readonly Color BgNormal = Color.FromArgb(35, 45, 80);
    private static readonly Color BgHover = Color.FromArgb(50, 60, 100);
    private static readonly Color BgPlaying = Color.FromArgb(232, 47, 77);
    private static readonly Color TextNormal = Color.FromArgb(220, 225, 240);
    private static readonly Color TextPlaying = Color.White;
    private static readonly Color AccentBar = Color.FromArgb(232, 47, 77);
    private static readonly Font ItemFont = new("Segoe UI", 9.5f);

    private const int BAR_W = 3;

    private bool _thumbHover;
    private bool _thumbDragging;

    public event Action<int>? ItemClicked;
    public event Action<int, Point>? ItemRightClicked;

    public List<string> FileNames { get; set; } = new();
    public HashSet<int> SelectedIndices { get; set; } = new();
    public string? CurrentPlaying { get; set; }

    public DarkScrollPanel()
    {
        SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint | ControlStyles.UserPaint, true);
    }

    public void SetItemCount(int count)
    {
        _itemCount = count;
        _scrollY = 0;
        _hoverIndex = -1;
        int totalH = count * ITEM_H;
        int maxScroll = Math.Max(0, totalH - ClientSize.Height);
        _scrollY = Math.Clamp(_scrollY, 0, maxScroll);
        Invalidate();
    }

    public void ScrollTo(int y)
    {
        int maxScroll = Math.Max(0, _itemCount * ITEM_H - ClientSize.Height);
        _scrollY = Math.Clamp(y, 0, maxScroll);
        Invalidate();
    }

    private int HitTest(Point p)
    {
        if (_itemCount == 0) return -1;
        int idx = (p.Y + _scrollY) / ITEM_H;
        if (idx < 0 || idx >= _itemCount) return -1;
        return idx;
    }

    protected override void OnPaint(PaintEventArgs e)
    {
        var g = e.Graphics;
        var client = ClientRectangle;
        int contentW = client.Width;
        bool needScroll = _itemCount * ITEM_H > client.Height;
        int sbW = needScroll ? SCROLLBAR_W : 0;
        int itemW = contentW - sbW;

        using var bg = new SolidBrush(BackColor);
        g.FillRectangle(bg, client);

        var format = new StringFormat
        {
            Trimming = StringTrimming.EllipsisCharacter,
            Alignment = StringAlignment.Near,
            LineAlignment = StringAlignment.Center,
            FormatFlags = StringFormatFlags.NoWrap,
        };

        using var bgNormalBrush = new SolidBrush(BgNormal);
        using var bgHoverBrush = new SolidBrush(BgHover);
        using var bgPlayingBrush = new SolidBrush(BgPlaying);
        using var textNormalBrush = new SolidBrush(TextNormal);
        using var textPlayingBrush = new SolidBrush(TextPlaying);
        using var barBrush = new SolidBrush(AccentBar);

        int first = Math.Max(0, _scrollY / ITEM_H);
        int last = Math.Min(_itemCount - 1, (int)Math.Ceiling((double)(client.Height + _scrollY) / ITEM_H));

        for (int i = first; i <= last; i++)
        {
            int y = i * ITEM_H - _scrollY;
            var rect = new Rectangle(0, y, itemW, ITEM_H);
            bool playing = FileNames.Count > i && FileNames[i] == CurrentPlaying;
            bool selected = SelectedIndices.Contains(i);
            bool hover = i == _hoverIndex;

            Color bg2;
            Brush textBrush;
            if (playing) { bg2 = BgPlaying; textBrush = textPlayingBrush; }
            else if (hover) { bg2 = BgHover; textBrush = textNormalBrush; }
            else { bg2 = BgNormal; textBrush = textNormalBrush; }

            using var itemBg = new SolidBrush(bg2);
            g.FillRectangle(itemBg, rect);

            if (selected)
                g.FillRectangle(barBrush, 0, y, BAR_W, ITEM_H);

            if (FileNames.Count > i)
            {
                int textX = selected ? BAR_W + 6 : 8;
                if (textX < itemW)
                {
                    var textRect = new Rectangle(textX, y, itemW - textX - 4, ITEM_H);
                    g.DrawString(FileNames[i], ItemFont, textBrush, textRect, format);
                }
            }
        }
        format.Dispose();

        if (needScroll)
        {
            int sbX = contentW - SCROLLBAR_W;
            int trackH = client.Height;
            float ratio = (float)trackH / (_itemCount * ITEM_H);
            int thumbH = Math.Max(SCROLLBAR_MIN_H, (int)(trackH * ratio));
            float scrollable = _itemCount * ITEM_H - trackH;
            float pos = scrollable > 0 ? (float)_scrollY / scrollable : 0;
            int thumbY = (int)(pos * (trackH - thumbH));

            using var trackBrush = new SolidBrush(TrackColor);
            g.FillRectangle(trackBrush, sbX, 0, SCROLLBAR_W, trackH);

            var thumbRect = new Rectangle(sbX + 1, thumbY, SCROLLBAR_W - 2, thumbH);
            Color tc = _thumbHover || _thumbDragging ? ThumbHoverColor : ThumbColor;
            using var thumbBrush = new SolidBrush(tc);
            g.FillRectangle(thumbBrush, thumbRect);
        }
    }

    private Rectangle GetThumbRect()
    {
        int clientH = ClientSize.Height;
        int totalH = _itemCount * ITEM_H;
        if (totalH <= clientH) return Rectangle.Empty;
        int sbX = ClientSize.Width - SCROLLBAR_W;
        float ratio = (float)clientH / totalH;
        int thumbH = Math.Max(SCROLLBAR_MIN_H, (int)(clientH * ratio));
        float scrollable = totalH - clientH;
        float pos = scrollable > 0 ? (float)_scrollY / scrollable : 0;
        int thumbY = (int)(pos * (clientH - thumbH));
        return new Rectangle(sbX, thumbY, SCROLLBAR_W, thumbH);
    }

    protected override void OnMouseDown(MouseEventArgs e)
    {
        if (e.Button == MouseButtons.Left)
        {
            if (GetThumbRect().Contains(e.Location))
            {
                _thumbDragging = true;
                _dragStartY = e.Y;
                _dragStartScroll = _scrollY;
                Cursor = Cursors.Hand;
                return;
            }

            int totalH = _itemCount * ITEM_H;
            if (totalH > ClientSize.Height && e.X >= ClientSize.Width - SCROLLBAR_W)
            {
                int clientH = ClientSize.Height;
                float ratio = (float)clientH / totalH;
                int thumbH = Math.Max(SCROLLBAR_MIN_H, (int)(clientH * ratio));
                float clickRatio = (float)e.Y / clientH;
                ScrollTo((int)(clickRatio * (totalH - clientH)) - thumbH / 2);
                return;
            }
        }
        base.OnMouseDown(e);
    }

    protected override void OnMouseMove(MouseEventArgs e)
    {
        if (_thumbDragging)
        {
            int clientH = ClientSize.Height;
            int totalH = _itemCount * ITEM_H;
            float ratio = (float)clientH / totalH;
            int thumbH = Math.Max(SCROLLBAR_MIN_H, (int)(clientH * ratio));
            float trackH = clientH - thumbH;
            float scrollable = totalH - clientH;
            int dy = e.Y - _dragStartY;
            float scrollDelta = (trackH > 0) ? dy / trackH * scrollable : 0;
            ScrollTo(_dragStartScroll + (int)scrollDelta);
        }
        else
        {
            int totalH = _itemCount * ITEM_H;
            bool inScrollArea = totalH > ClientSize.Height && e.X >= ClientSize.Width - SCROLLBAR_W;

            bool wasThumbHover = _thumbHover;
            _thumbHover = GetThumbRect().Contains(e.Location);
            if (_thumbHover != wasThumbHover) Invalidate();

            if (!inScrollArea && e.Button == MouseButtons.None)
            {
                int newHover = HitTest(e.Location);
                if (newHover != _hoverIndex)
                {
                    _hoverIndex = newHover;
                    Invalidate();
                }
            }
        }
        base.OnMouseMove(e);
    }

    protected override void OnMouseUp(MouseEventArgs e)
    {
        if (_thumbDragging)
        {
            _thumbDragging = false;
            Cursor = Cursors.Default;
            Invalidate();
        }
        base.OnMouseUp(e);
    }

    protected override void OnMouseClick(MouseEventArgs e)
    {
        if (_thumbDragging) return;
        base.OnMouseClick(e);

        int totalH = _itemCount * ITEM_H;
        if (totalH > ClientSize.Height && e.X >= ClientSize.Width - SCROLLBAR_W) return;

        int idx = HitTest(e.Location);
        if (idx < 0) return;

        if (e.Button == MouseButtons.Left)
            ItemClicked?.Invoke(idx);
        else if (e.Button == MouseButtons.Right)
            ItemRightClicked?.Invoke(idx, e.Location);
    }

    protected override void OnMouseLeave(EventArgs e)
    {
        _thumbHover = false;
        if (_hoverIndex != -1)
        {
            _hoverIndex = -1;
            Invalidate();
        }
        base.OnMouseLeave(e);
    }

    protected override void OnMouseWheel(MouseEventArgs e)
    {
        int delta = -(e.Delta / SystemInformation.MouseWheelScrollDelta * SystemInformation.MouseWheelScrollLines);
        ScrollTo(_scrollY + delta * ITEM_H);
        base.OnMouseWheel(e);
    }

    protected override void OnSizeChanged(EventArgs e)
    {
        base.OnSizeChanged(e);
        int totalH = _itemCount * ITEM_H;
        int maxScroll = Math.Max(0, totalH - ClientSize.Height);
        _scrollY = Math.Clamp(_scrollY, 0, maxScroll);
        Invalidate();
    }
}

internal static class InputDialog
{
    private static readonly Color Bg = Color.FromArgb(22, 30, 62);
    private static readonly Color Surface = Color.FromArgb(35, 45, 80);
    private static readonly Color Txt = Color.FromArgb(220, 225, 240);
    private static readonly Color Accent = Color.FromArgb(232, 47, 77);
    private static readonly Color Muted = Color.FromArgb(69, 71, 90);

    public static string? Show(string title, string prompt, string defaultValue = "")
    {
        using var f = new Form
        {
            Text = title,
            ControlBox = false,
            FormBorderStyle = FormBorderStyle.None,
            StartPosition = FormStartPosition.CenterParent,
            ClientSize = new Size(300, 100),
            TopMost = true,
            ShowInTaskbar = false,
            BackColor = Bg,
        };

        var okBtn = new Button
        {
            Text = "✔",
            Dock = DockStyle.Right,
            Width = 44,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Txt,
            BackColor = Bg,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            DialogResult = DialogResult.OK,
            FlatAppearance = { BorderSize = 0, BorderColor = Bg },
        };

        var cancelBtn = new Button
        {
            Text = "x",
            Dock = DockStyle.Right,
            Width = 44,
            FlatStyle = FlatStyle.Flat,
            ForeColor = Txt,
            BackColor = Bg,
            Font = new Font("Segoe UI", 11f, FontStyle.Bold),
            Cursor = Cursors.Hand,
            DialogResult = DialogResult.Cancel,
            FlatAppearance = { BorderSize = 0, BorderColor = Bg },
        };

        var buttonsBar = new Panel
        {
            Dock = DockStyle.Top,
            Height = 28,
            BackColor = Bg,
        };
        buttonsBar.Controls.Add(okBtn);
        buttonsBar.Controls.Add(cancelBtn);

        var textBox = new TextBox
        {
            Location = new Point(8, 40),
            Size = new Size(284, 24),
            Text = defaultValue,
            BackColor = Surface,
            ForeColor = Txt,
            BorderStyle = BorderStyle.FixedSingle,
        };

        f.Controls.Add(textBox);
        f.Controls.Add(buttonsBar);
        f.AcceptButton = okBtn;
        f.CancelButton = cancelBtn;

        return f.ShowDialog() == DialogResult.OK ? textBox.Text : null;
    }
}

internal class DarkMenuRenderer : ToolStripProfessionalRenderer
{
    private static readonly Color Surface = Color.FromArgb(35, 45, 80);
    private static readonly Color Hover = Color.FromArgb(60, 70, 110);
    private static readonly Color Muted = Color.FromArgb(69, 71, 90);

    public DarkMenuRenderer() { RoundedEdges = false; }

    protected override void OnRenderMenuItemBackground(ToolStripItemRenderEventArgs e)
    {
        var color = e.Item.Selected ? Hover : Surface;
        e.Graphics.FillRectangle(new SolidBrush(color), new Rectangle(Point.Empty, e.Item.Size));
    }

    protected override void OnRenderToolStripBackground(ToolStripRenderEventArgs e)
    {
        e.Graphics.FillRectangle(new SolidBrush(Surface), e.AffectedBounds);
    }

    protected override void OnRenderToolStripBorder(ToolStripRenderEventArgs e)
    {
        e.Graphics.DrawRectangle(new Pen(Muted), 0, 0, e.AffectedBounds.Width - 1, e.AffectedBounds.Height - 1);
    }

    protected override void OnRenderSeparator(ToolStripSeparatorRenderEventArgs e)
    {
        e.Graphics.FillRectangle(new SolidBrush(Muted), e.Item.ContentRectangle);
    }

    protected override void OnRenderImageMargin(ToolStripRenderEventArgs e)
    {
        e.Graphics.FillRectangle(new SolidBrush(Surface), e.AffectedBounds);
    }
}
