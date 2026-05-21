# Middo

Windows 托盘工具：按全局快捷键，将当前前台窗口居中到它所在显示器的工作区。

## 功能

- 托盘常驻，无主窗口。
- 默认快捷键 `Ctrl+Alt+C`。
- 支持在设置窗口捕获自定义快捷键。
- 支持 `Alt`、`Ctrl`、`Shift`、`Win` 修饰键组合。
- 设置窗口支持 DPI 自适应，适配常见分辨率和缩放。
- 保存设置后窗口不关闭，并显示 `已保存：快捷键`。
- 设置窗口同一时间只允许打开一个。
- 点击托盘图标右键菜单“设置”或双击托盘图标打开设置窗口。
- 支持开机自启，自启时只进入托盘。
- 支持单实例运行，重复启动会直接退出。
- 支持设置窗口自身被快捷键居中。

## 文件结构

```text
Middo/
├── Program.cs                  # 程序入口、单实例、启动模式
├── TrayApplicationContext.cs   # 托盘菜单、热键注册、设置保存、自启
├── SettingsWindow.cs           # 设置窗口 UI 和快捷键捕获
├── HotkeyConfig.cs             # hotkey.json 配置读写和快捷键格式化
├── KeyNames.cs                 # 虚拟键码显示名称
├── MemoryTrimmer.cs            # 后台托盘状态收缩工作集
├── CenteringService.cs          # 当前窗口居中逻辑
├── NativeMethods.cs            # Win32 API 声明
├── Middo.csproj                # .NET 项目文件
└── app.ico                     # 程序图标
```

## 配置文件

程序会在 exe 同目录生成 `hotkey.json`：

```json
{"Modifiers":3,"Key":67,"AutoStart":false}
```

字段含义：

- `Modifiers`: 修饰键位标志，`1=Alt`、`2=Ctrl`、`4=Shift`、`8=Win`，可叠加。
- `Key`: Windows 虚拟键码，例如 `67` 是 `C`。
- `AutoStart`: 是否写入当前用户开机自启。

## 构建

开发运行：

```cmd
dotnet run
```

预览设置窗口：

```cmd
dotnet run -- preview
```

小体积单文件版本，要求目标电脑已安装 .NET 8 Desktop Runtime：

```cmd
dotnet publish -r win-x64 -p:PublishSingleFile=true -p:UseSharedCompilation=false -nodeReuse:false --self-contained false -o small
```

自包含单文件版本，不要求目标电脑安装 .NET，体积更大：

```cmd
dotnet publish -r win-x64 -p:PublishSingleFile=true -p:SelfContained=true -p:IncludeNativeLibrariesForSelfExtract=true -p:EnableCompressionInSingleFile=true -p:UseSharedCompilation=false -nodeReuse:false --self-contained true -o single
```

## 技术点

- `RegisterHotKey` 注册全局快捷键。
- 隐藏 `Form` 通过 `WndProc` 接收 `WM_HOTKEY`。
- `GetForegroundWindow` 获取当前前台窗口。
- `MonitorFromWindow` 和 `GetMonitorInfo` 获取窗口所在显示器工作区。
- `SetWindowPos` 移动窗口到居中位置。
- `Microsoft.Win32.Registry` 写入 `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` 实现自启。
- `GC.Collect` 和 `EmptyWorkingSet` 在设置窗口关闭后收缩后台工作集。
- `Application.SetHighDpiMode(HighDpiMode.PerMonitorV2)` 和 `TableLayoutPanel` 适配 DPI 缩放。

## 注意

- WinForms 项目需要 Windows Desktop SDK，在 Linux 本机通常不能直接编译。
- 在 KVM Windows 虚拟机中发布时建议保留 `-p:UseSharedCompilation=false -nodeReuse:false`，避免编译器服务器或 MSBuild 节点复用导致崩溃。
- 小体积版本适合已安装 .NET 的电脑；自包含版本适合复制到任意 Windows 电脑直接运行。
