namespace Middo;

internal partial class SettingsWindow : Form
{
    private uint _modifiers;
    private uint _key;
    // 进入捕获前先保存旧值，Esc 取消时恢复。
    private uint _preCaptureMods;
    private uint _preCaptureKey;
    private bool _capturing;

    private GroupBox _grpHotkey = null!;
    private Button _btnCapture = null!;
    private Label _lblCaptureHint = null!;
    private Label _lblCurrent = null!;
    private CheckBox _chkAutoStart = null!;
    private Button _btnSave = null!;
    private Button _btnReset = null!;
    private Button _btnCancel = null!;

    public event EventHandler? CaptureStarted;
    public event EventHandler? CaptureEnded;
    public event EventHandler? SettingsSaved;

    public uint Modifiers => _modifiers;
    public uint Key => _key;
    public bool AutoStart => _chkAutoStart.Checked;

    public SettingsWindow(uint modifiers, uint key, bool autoStart)
    {
        // 设置窗口持有一份待编辑值，只有点“保存”才通知外层写配置。
        _modifiers = modifiers;
        _key = key;

        Text = "设置";
        AutoScaleMode = AutoScaleMode.Dpi;
        FormBorderStyle = FormBorderStyle.Sizable;
        MaximizeBox = true;
        MinimizeBox = true;
        StartPosition = FormStartPosition.CenterScreen;
        ShowInTaskbar = true;
        // 捕获快捷键时让窗体优先收到键盘事件。
        KeyPreview = true;
        KeyDown += OnKeyCapture;

        BuildUI(autoStart);
    }

    private void BuildUI(bool autoStart)
    {
        // 这些尺寸是 100% DPI 下的逻辑单位，WinForms 会按 DPI 自动缩放。
        int pad = 12;
        int gap = 6;
        int buttonHeight = 28;
        int captureButtonWidth = 270;

        SuspendLayout();

        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 1,
            RowCount = 5,
            Padding = new Padding(pad),
        };
        // AutoSize 行根据内容撑开；Percent 行吸收用户手动放大的空白。
        root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, gap));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.Absolute, buttonHeight + 16));

        // ── 快捷键分组 ──
        _grpHotkey = new GroupBox
        {
            Text = "快捷键",
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = Padding.Empty,
            Padding = new Padding(12, 6, 12, 10),
        };

        var hotkeyLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 2,
            RowCount = 3,
            Margin = new Padding(0, 2, 0, 0),
            Padding = Padding.Empty,
        };
        hotkeyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.AutoSize));
        hotkeyLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        hotkeyLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        hotkeyLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        hotkeyLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        // 当前快捷键一行：标题固定宽度，右侧显示当前值。
        var lbl1 = new Label
        {
            Text = "当前快捷键：",
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(3, 0, 6, 8),
        };
        _lblCurrent = new Label
        {
            Text = HotkeyConfig.FormatHotkey(_modifiers, _key),
            AutoSize = true,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(0, 0, 3, 8),
        };

        _btnCapture = new Button
        {
            Text = "捕获按键",
            Size = new Size(captureButtonWidth, 30),
            Anchor = AnchorStyles.None,
            Margin = new Padding(0, 0, 0, 8),
        };
        _btnCapture.Click += OnStartCapture;

        // 提示保持单行显示，因此窗口最小宽度会按它的首选宽度计算。
        _lblCaptureHint = new Label
        {
            Text = "点击按钮后按下组合键",
            AutoSize = true,
            ForeColor = Color.Gray,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(3, 0, 3, 0),
        };

        hotkeyLayout.Controls.Add(lbl1, 0, 0);
        hotkeyLayout.Controls.Add(_lblCurrent, 1, 0);
        hotkeyLayout.Controls.Add(_btnCapture, 0, 1);
        hotkeyLayout.SetColumnSpan(_btnCapture, 2);
        hotkeyLayout.Controls.Add(_lblCaptureHint, 0, 2);
        hotkeyLayout.SetColumnSpan(_lblCaptureHint, 2);
        _grpHotkey.Controls.Add(hotkeyLayout);

        // ── 启动分组 ──
        var grpStartup = new GroupBox
        {
            Text = "启动",
            Dock = DockStyle.Fill,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            Margin = Padding.Empty,
            Padding = new Padding(12, 6, 12, 10),
        };
        // 启动项单独放进自适应表格，避免高 DPI 时贴到分组标题。
        _chkAutoStart = new CheckBox
        {
            Text = "开机自动启动",
            AutoSize = true,
            Checked = autoStart,
            Anchor = AnchorStyles.Left,
            Margin = new Padding(3, 2, 3, 0),
        };

        var startupLayout = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            AutoSizeMode = AutoSizeMode.GrowAndShrink,
            ColumnCount = 1,
            RowCount = 1,
            Margin = new Padding(0, 2, 0, 0),
            Padding = Padding.Empty,
        };
        startupLayout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        startupLayout.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        startupLayout.Controls.Add(_chkAutoStart, 0, 0);
        grpStartup.Controls.Add(startupLayout);

        // ── 底部按钮 ──
        var btnPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            ColumnCount = 3,
            RowCount = 1,
            Margin = Padding.Empty,
            Padding = new Padding(0, 8, 0, 0),
        };
        // 底部三个按钮等分宽度，最小宽度由文本首选宽度决定。
        btnPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333f));
        btnPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.333f));
        btnPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.334f));
        btnPanel.RowStyles.Add(new RowStyle(SizeType.Percent, 100));

        _btnReset = new Button { Text = "还原默认", Dock = DockStyle.Fill, Height = buttonHeight, Margin = new Padding(0, 0, 6, 0) };
        _btnSave = new Button { Text = "保存", Dock = DockStyle.Fill, Height = buttonHeight, Margin = new Padding(6, 0, 6, 0) };
        _btnCancel = new Button { Text = "取消", Dock = DockStyle.Fill, Height = buttonHeight, Margin = new Padding(6, 0, 0, 0) };
        _btnReset.Click += OnReset;
        _btnSave.Click += OnSave;
        _btnCancel.Click += (_, _) => Close();
        btnPanel.Controls.Add(_btnReset, 0, 0);
        btnPanel.Controls.Add(_btnSave, 1, 0);
        btnPanel.Controls.Add(_btnCancel, 2, 0);

        root.Controls.Add(_grpHotkey, 0, 0);
        root.Controls.Add(grpStartup, 0, 2);
        root.Controls.Add(btnPanel, 0, 4);
        Controls.Add(root);

        // 根据按钮文本和提示文本计算窗口最小宽度，避免缩小时换行或重叠。
        int minButtonWidth = Math.Max(_btnReset.GetPreferredSize(Size.Empty).Width,
            Math.Max(_btnSave.GetPreferredSize(Size.Empty).Width, _btnCancel.GetPreferredSize(Size.Empty).Width));
        minButtonWidth += 8;

        _btnReset.MinimumSize = new Size(minButtonWidth, buttonHeight);
        _btnSave.MinimumSize = new Size(minButtonWidth, buttonHeight);
        _btnCancel.MinimumSize = new Size(minButtonWidth, buttonHeight);

        int minClientWidth = Math.Max(400,
            Math.Max(pad * 2 + minButtonWidth * 3 + 36,
                pad * 2 + _lblCaptureHint.GetPreferredSize(Size.Empty).Width + 30));
        int minClientHeight = root.GetPreferredSize(new Size(minClientWidth, 0)).Height;
        ClientSize = new Size(minClientWidth, minClientHeight + 8);
        // 把内容区最小尺寸换算成包含标题栏/边框的窗体最小尺寸。
        MinimumSize = SizeFromClientSize(ClientSize);

        ResumeLayout(performLayout: true);
    }

    private void OnStartCapture(object? sender, EventArgs e)
    {
        // 捕获期间外层会临时注销全局热键，保证当前快捷键本身也能被捕获。
        _preCaptureMods = _modifiers;
        _preCaptureKey = _key;
        _capturing = true;
        _modifiers = 0;
        _key = 0;
        _btnCapture.Enabled = false;
        _btnCapture.Text = "正在捕获... 按 Esc 取消";
        _lblCaptureHint.Text = "请按下组合键";
        CaptureStarted?.Invoke(this, EventArgs.Empty);
        Focus();
    }

    private void OnKeyCapture(object? sender, KeyEventArgs e)
    {
        if (!_capturing) return;

        // Esc 不保存本次捕获，恢复进入捕获前的值。
        if (e.KeyCode == Keys.Escape)
        {
            _modifiers = _preCaptureMods;
            _key = _preCaptureKey;
            EndCapture();
            _btnCapture.Enabled = true;
            _btnCapture.Text = "捕获按键";
            _lblCaptureHint.Text = "已取消，恢复为上次设置";
            _lblCurrent.Text = HotkeyConfig.FormatHotkey(_modifiers, _key);
            e.Handled = true;
            return;
        }

        uint mods = 0;
        // KeyEventArgs 和 ModifierKeys 结合使用，补足 Win 键等捕获不稳定的情况。
        if (e.Control) mods |= NativeMethods.MOD_CONTROL;
        if (e.Alt) mods |= NativeMethods.MOD_ALT;
        if (e.Shift) mods |= NativeMethods.MOD_SHIFT;
        Keys mk = ModifierKeys;
        if ((mk & Keys.Control) != 0) mods |= NativeMethods.MOD_CONTROL;
        if ((mk & Keys.Alt) != 0) mods |= NativeMethods.MOD_ALT;
        if ((mk & Keys.Shift) != 0) mods |= NativeMethods.MOD_SHIFT;
        if ((mk & (Keys.LWin | Keys.RWin)) != 0) mods |= NativeMethods.MOD_WIN;
        _modifiers = mods;

        if (e.KeyCode is Keys.ControlKey or Keys.Menu or Keys.ShiftKey or Keys.LWin or Keys.RWin)
        {
            // 只按下修饰键时继续等待主键。
            _lblCaptureHint.Text = "当前: " + HotkeyConfig.FormatModifiers(mods) + "+?";
            return;
        }

        // 非修饰键到达后，一次完整快捷键捕获完成。
        _key = (uint)e.KeyValue;
        EndCapture();
        _btnCapture.Enabled = true;
        _btnCapture.Text = "捕获按键";
        _lblCaptureHint.Text = "已捕获: " + HotkeyConfig.FormatHotkey(_modifiers, _key);
        _lblCurrent.Text = HotkeyConfig.FormatHotkey(_modifiers, _key);
        e.Handled = true;
    }

    private void EndCapture()
    {
        if (!_capturing) return;

        _capturing = false;
        // 通知托盘上下文恢复全局热键。
        CaptureEnded?.Invoke(this, EventArgs.Empty);
    }

    protected override void OnFormClosed(FormClosedEventArgs e)
    {
        // 防止捕获中直接关闭窗口导致全局热键没有恢复。
        EndCapture();
        base.OnFormClosed(e);
    }

    private void OnReset(object? sender, EventArgs e)
    {
        // 只还原界面里的待保存值，真正写入要再点“保存”。
        var def = HotkeyConfig.Default();
        _modifiers = def.Modifiers;
        _key = def.Key;
        _lblCurrent.Text = HotkeyConfig.FormatHotkey(_modifiers, _key);
        _lblCaptureHint.Text = "已还原为默认快捷键";
    }

    private void OnSave(object? sender, EventArgs e)
    {
        // 必须同时有修饰键和主键，避免保存无效热键。
        if (_modifiers == 0 || _key == 0)
        {
            MessageBox.Show("请先捕获组合键再保存。", "提示",
                MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        // 设置窗口不关闭，由托盘上下文负责写配置和重新注册热键。
        SettingsSaved?.Invoke(this, EventArgs.Empty);
        _lblCaptureHint.Text = "已保存：" + HotkeyConfig.FormatHotkey(_modifiers, _key);
    }

}
