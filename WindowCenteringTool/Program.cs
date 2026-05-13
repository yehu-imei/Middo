namespace WindowCenteringTool;

internal static class Program
{
    [STAThread]
    static void Main(string[] args)
    {
        // 用命名互斥锁限制单实例，避免第二个进程抢占同一个全局热键。
        using var singleInstance = new Mutex(
            initiallyOwned: true,
            name: @"Local\WindowCenteringTool.SingleInstance",
            createdNew: out bool createdNew);

        // 已有实例运行时，当前进程直接退出，不弹额外提示。
        if (!createdNew)
        {
            return;
        }

        // 启用 Per-Monitor V2 DPI 感知，让设置窗口按当前显示器缩放。
        Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
        Application.EnableVisualStyles();
        ApplicationConfiguration.Initialize();

        // preview 只打开设置窗口，用于在虚拟机里快速调试 UI。
        bool previewMode = args.Any(arg => string.Equals(arg, "preview", StringComparison.OrdinalIgnoreCase));
        // --tray 用于开机自启：只进托盘，不主动弹出设置窗口。
        bool trayOnly = args.Any(arg => string.Equals(arg, "--tray", StringComparison.OrdinalIgnoreCase));

        if (previewMode)
        {
            var config = HotkeyConfig.Load();
            Application.Run(new SettingsWindow(config.Modifiers, config.Key, config.AutoStart));
            return;
        }

        // 正常运行托盘上下文；手动双击 exe 时默认打开设置窗口。
        Application.Run(new TrayApplicationContext(showSettingsOnStartup: !trayOnly));
    }
}
