using System.Text.Json;

namespace WindowCenteringTool;

internal class HotkeyConfig
{
    // RegisterHotKey 使用的修饰键位标志。
    public uint Modifiers { get; set; }
    // Windows 虚拟键码，例如 0x43 表示 C。
    public uint Key { get; set; }
    // 是否写入 HKCU Run，实现当前用户开机自启。
    public bool AutoStart { get; set; }

    // 配置跟随 exe 放在同目录，便于绿色分发。
    private static string FilePath => Path.Combine(AppContext.BaseDirectory, "hotkey.json");

    public static HotkeyConfig Load()
    {
        try
        {
            // 配置不存在或内容非法时，统一回退到默认快捷键。
            if (File.Exists(FilePath))
            {
                string json = File.ReadAllText(FilePath);
                var config = JsonSerializer.Deserialize<HotkeyConfig>(json);
                if (config is { Modifiers: > 0, Key: > 0 })
                    return config;
            }
        }
        catch
        {
        }

        return Default();
    }

    public void Save()
    {
        // 当前配置体积很小，直接覆盖写入即可。
        string json = JsonSerializer.Serialize(this);
        File.WriteAllText(FilePath, json);
    }

    public static HotkeyConfig Default()
    {
        // 默认快捷键：Ctrl+Alt+C。
        return new HotkeyConfig
        {
            Modifiers = NativeMethods.MOD_CONTROL | NativeMethods.MOD_ALT,
            Key = 0x43
        };
    }

    public static string FormatModifiers(uint modifiers)
    {
        // 捕获过程中可能只有修饰键，单独格式化方便提示当前状态。
        var parts = GetModifierNames(modifiers);
        return parts.Count > 0 ? string.Join("+", parts) : "无";
    }

    public static string FormatHotkey(uint modifiers, uint key)
    {
        // 托盘提示、设置窗口显示、保存状态都使用同一套格式。
        var parts = GetModifierNames(modifiers);
        parts.Add(KeyNames.GetName(key));
        return string.Join("+", parts);
    }

    private static List<string> GetModifierNames(uint modifiers)
    {
        var parts = new List<string>();
        if ((modifiers & NativeMethods.MOD_CONTROL) != 0) parts.Add("Ctrl");
        if ((modifiers & NativeMethods.MOD_ALT) != 0) parts.Add("Alt");
        if ((modifiers & NativeMethods.MOD_SHIFT) != 0) parts.Add("Shift");
        if ((modifiers & NativeMethods.MOD_WIN) != 0) parts.Add("Win");
        return parts;
    }
}
