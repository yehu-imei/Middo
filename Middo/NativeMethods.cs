using System.Runtime.InteropServices;

namespace Middo;

// 集中声明本工具直接使用的 Win32 API。
internal static class NativeMethods
{
    // 注册/注销全局快捷键，WM_HOTKEY 会发到指定窗口句柄。
    [DllImport("user32.dll")]
    public static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);

    [DllImport("user32.dll")]
    public static extern bool UnregisterHotKey(IntPtr hWnd, int id);

    // 获取当前前台窗口，用于“选中哪个窗口就居中哪个窗口”。
    [DllImport("user32.dll")]
    public static extern IntPtr GetForegroundWindow();

    // 获取窗口矩形，计算窗口宽高。
    [DllImport("user32.dll")]
    public static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    // 找到窗口所在的显示器，支持多显示器居中。
    [DllImport("user32.dll")]
    public static extern IntPtr MonitorFromWindow(IntPtr hWnd, uint dwFlags);

    // 读取显示器工作区，避开任务栏。
    [DllImport("user32.dll")]
    public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFO lpmi);

    // 移动窗口位置；本工具只改位置，不改大小和 Z 序。
    [DllImport("user32.dll")]
    public static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter,
        int X, int Y, int cx, int cy, uint uFlags);

    // 后台托盘状态下收缩进程工作集，降低任务管理器里看到的常驻占用。
    [DllImport("kernel32.dll")]
    public static extern IntPtr GetCurrentProcess();

    [DllImport("psapi.dll")]
    public static extern bool EmptyWorkingSet(IntPtr hProcess);

    public const uint MOD_ALT = 0x0001;
    public const uint MOD_CONTROL = 0x0002;
    public const uint MOD_SHIFT = 0x0004;
    public const uint MOD_WIN = 0x0008;

    public const int WM_HOTKEY = 0x0312;
    public const uint MONITOR_DEFAULTTONEAREST = 0x00000002;
    public const uint SWP_NOSIZE = 0x0001;
    public const uint SWP_NOZORDER = 0x0004;
    public const int HOTKEY_ID = 1;
}

[StructLayout(LayoutKind.Sequential)]
internal struct RECT
{
    public int Left;
    public int Top;
    public int Right;
    public int Bottom;
}

[StructLayout(LayoutKind.Sequential)]
internal struct MONITORINFO
{
    public int cbSize;
    public RECT rcMonitor;
    public RECT rcWork;
    public uint dwFlags;

    public static MONITORINFO Create()
    {
        return new MONITORINFO { cbSize = Marshal.SizeOf<MONITORINFO>() };
    }
}
