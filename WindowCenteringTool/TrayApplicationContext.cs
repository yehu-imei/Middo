using Microsoft.Win32;

namespace WindowCenteringTool;

/// <summary>
/// 托盘应用上下文。
/// </summary>
internal class TrayApplicationContext : ApplicationContext
{
    private readonly NotifyIcon _notifyIcon;
    // 隐藏窗口负责接收 WM_HOTKEY，托盘程序本身不显示主窗口。
    private readonly HiddenForm _hiddenForm;
    private HotkeyConfig _config;
    private SettingsWindow? _settingsWindow;
    // 记录当前是否已注册，避免重复注册同一个 ID。
    private bool _hotkeyRegistered;

    public TrayApplicationContext(bool showSettingsOnStartup)
    {
        // 启动时读取上次保存的快捷键和自启状态。
        _config = HotkeyConfig.Load();

        _hiddenForm = new HiddenForm();
        _hiddenForm.HotKeyPressed += OnHotKeyPressed;

        RegisterHotkey();

        var contextMenu = new ContextMenuStrip();

        // 右键菜单只保留设置和退出，保持托盘工具简单。
        var settingsMenuItem = new ToolStripMenuItem("设置");
        settingsMenuItem.Click += OnOpenSettings;
        contextMenu.Items.Add(settingsMenuItem);

        contextMenu.Items.Add(new ToolStripSeparator());

        var exitMenuItem = new ToolStripMenuItem("退出");
        exitMenuItem.Click += (_, _) => ExitApplication();
        contextMenu.Items.Add(exitMenuItem);

        _notifyIcon = new NotifyIcon
        {
            Icon = Icon.ExtractAssociatedIcon(Application.ExecutablePath)!,
            Text = "WindowCenteringTool - " + HotkeyConfig.FormatHotkey(_config.Modifiers, _config.Key),
            ContextMenuStrip = contextMenu,
            Visible = true
        };
        // 双击托盘图标等同于点“设置”。
        _notifyIcon.DoubleClick += OnOpenSettings;

        if (showSettingsOnStartup)
        {
            // BeginInvoke 等消息循环启动后再弹设置窗口，避免构造阶段显示窗口。
            _hiddenForm.BeginInvoke(new Action(OpenSettings));
        }
    }

    private void RegisterHotkey()
    {
        // 捕获按键期间会临时注销；正常状态保持注册。
        if (_hotkeyRegistered)
            return;

        _hotkeyRegistered = NativeMethods.RegisterHotKey(
            _hiddenForm.Handle,
            NativeMethods.HOTKEY_ID,
            _config.Modifiers,
            _config.Key);
    }

    private void UnregisterHotkey()
    {
        if (!_hotkeyRegistered)
            return;

        // 注销失败也按未注册处理，避免状态卡住。
        NativeMethods.UnregisterHotKey(_hiddenForm.Handle, NativeMethods.HOTKEY_ID);
        _hotkeyRegistered = false;
    }

    private void OnOpenSettings(object? sender, EventArgs e)
    {
        OpenSettings();
    }

    private void OpenSettings()
    {
        // 设置窗口只允许一个；重复打开时把已有窗口带到前台。
        if (_settingsWindow != null && !_settingsWindow.IsDisposed)
        {
            if (_settingsWindow.WindowState == FormWindowState.Minimized)
                _settingsWindow.WindowState = FormWindowState.Normal;

            _settingsWindow.Activate();
            _settingsWindow.BringToFront();
            return;
        }

        uint modifiers = _config.Modifiers;
        uint key = _config.Key;
        bool autoStart = _config.AutoStart;

        try
        {
            using var window = new SettingsWindow(modifiers, key, autoStart);
            _settingsWindow = window;
            // 捕获当前快捷键本身时，需要临时释放全局热键。
            window.CaptureStarted += (_, _) => UnregisterHotkey();
            window.CaptureEnded += (_, _) => RegisterHotkey();
            // 保存不关闭窗口，因此通过事件即时写配置。
            window.SettingsSaved += (_, _) => SaveSettings(window);
            window.ShowDialog(_hiddenForm);
        }
        finally
        {
            _settingsWindow = null;
            if (!_hotkeyRegistered)
                RegisterHotkey();
        }
    }

    private void SaveSettings(SettingsWindow window)
    {
        // 保存新热键前先注销旧热键，防止同一个 ID 重复注册。
        UnregisterHotkey();
        try
        {
            _config.Modifiers = window.Modifiers;
            _config.Key = window.Key;
            _config.AutoStart = window.AutoStart;
            _config.Save();

            SetAutoStart(_config.AutoStart);

            // 托盘悬停文本同步显示当前快捷键。
            _notifyIcon.Text = "WindowCenteringTool - " +
                HotkeyConfig.FormatHotkey(_config.Modifiers, _config.Key);
        }
        finally
        {
            if (!_hotkeyRegistered)
            {
                RegisterHotkey();
            }
        }
    }

    private static void SetAutoStart(bool enable)
    {
        // 写当前用户 Run 项，不需要管理员权限。
        using var key = Registry.CurrentUser.OpenSubKey(
            @"Software\Microsoft\Windows\CurrentVersion\Run", writable: true);
        if (key == null) return;

        if (enable)
            // 自启时使用 --tray，避免每次开机都弹设置窗口。
            key.SetValue("WindowCenteringTool", $"\"{Application.ExecutablePath}\" --tray");
        else
            key.DeleteValue("WindowCenteringTool", throwOnMissingValue: false);
    }

    private void OnHotKeyPressed(object? sender, EventArgs e)
    {
        // 收到 WM_HOTKEY 后居中当前前台窗口。
        WindowCenteringService.CenterActiveWindow();
    }

    private void ExitApplication()
    {
        // 退出前释放系统资源和热键。
        UnregisterHotkey();
        _notifyIcon.Visible = false;
        _notifyIcon.Dispose();
        _hiddenForm.Close();
        _hiddenForm.Dispose();
        ExitThread();
    }
}

/// <summary>
/// 隐藏窗口，接收 Windows 消息（WM_HOTKEY）。
/// </summary>
internal class HiddenForm : Form
{
    public event EventHandler? HotKeyPressed;

    protected override void WndProc(ref Message m)
    {
        // RegisterHotKey 触发后，Windows 会把 WM_HOTKEY 发到这个隐藏窗口。
        if (m.Msg == NativeMethods.WM_HOTKEY && m.WParam.ToInt32() == NativeMethods.HOTKEY_ID)
        {
            HotKeyPressed?.Invoke(this, EventArgs.Empty);
        }
        base.WndProc(ref m);
    }
}
