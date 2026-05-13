namespace WindowCenteringTool;

/// <summary>
/// 窗口居中核心逻辑。
/// </summary>
internal static class WindowCenteringService
{
    public static void CenterActiveWindow()
    {
        try
        {
            // 当前前台窗口就是用户此刻“选中”的窗口。
            IntPtr hWnd = NativeMethods.GetForegroundWindow();
            if (hWnd == IntPtr.Zero)
                return;

            // 读取窗口当前位置和大小；失败时静默跳过。
            if (!NativeMethods.GetWindowRect(hWnd, out RECT windowRect))
                return;

            // 以窗口所在显示器为准，避免多屏时居中到错误屏幕。
            IntPtr hMonitor = NativeMethods.MonitorFromWindow(hWnd,
                NativeMethods.MONITOR_DEFAULTTONEAREST);
            if (hMonitor == IntPtr.Zero)
                return;

            // 使用工作区而不是完整屏幕区域，避免压到任务栏。
            MONITORINFO monitorInfo = MONITORINFO.Create();
            if (!NativeMethods.GetMonitorInfo(hMonitor, ref monitorInfo))
                return;

            RECT workArea = monitorInfo.rcWork;

            int windowWidth = windowRect.Right - windowRect.Left;
            int windowHeight = windowRect.Bottom - windowRect.Top;
            int workWidth = workArea.Right - workArea.Left;
            int workHeight = workArea.Bottom - workArea.Top;

            // 居中坐标 = 工作区起点 + 剩余空间的一半。
            int newX = workArea.Left + (workWidth - windowWidth) / 2;
            int newY = workArea.Top + (workHeight - windowHeight) / 2;

            // 窗口比工作区大时，至少保证左上角不跑出工作区。
            if (newX < workArea.Left) newX = workArea.Left;
            if (newY < workArea.Top) newY = workArea.Top;

            // 只移动位置，不改变窗口大小和层级。
            NativeMethods.SetWindowPos(hWnd, IntPtr.Zero,
                newX, newY, 0, 0,
                NativeMethods.SWP_NOSIZE | NativeMethods.SWP_NOZORDER);
        }
        catch (Exception ex)
        {
            MessageBox.Show($"居中窗口失败：{ex.Message}",
                "WindowCenteringTool", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
    }
}
