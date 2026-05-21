# Middo

Middo 是一个 Windows 托盘工具：按下全局快捷键，将当前前台窗口居中到它所在显示器的工作区。它适合经常手动整理窗口位置、使用多显示器或高 DPI 缩放的桌面环境。

当前版本：`1.0.0`

## 功能特性

- 全局快捷键居中当前前台窗口，默认 `Ctrl+Alt+C`。
- 多显示器支持：窗口会居中到它当前所在的显示器。
- 使用显示器工作区计算位置，避免压到任务栏。
- 托盘常驻，无主窗口。
- 右键托盘图标可打开设置或退出。
- 双击托盘图标可打开设置窗口。
- 支持在设置窗口捕获自定义快捷键。
- 支持 `Alt`、`Ctrl`、`Shift`、`Win` 修饰键组合。
- 保存设置后窗口不关闭，并显示保存状态。
- 设置窗口同一时间只允许打开一个。
- 支持开机自启动，自启时只进入托盘，不主动弹出设置窗口。
- 支持单实例运行，重复启动会直接退出。
- 设置窗口支持 DPI 自适应。
- 关闭设置窗口后会主动收缩后台工作集，降低任务管理器里看到的常驻占用。
- 单文件发布：提供 .NET 8 依赖版和自包含版两个 exe，不附带 `.pdb`。
- 绿色配置：`hotkey.json` 保存在 `Middo.exe` 同目录，方便复制、备份和迁移。

## 默认设置

| 设置项 | 默认值 |
| --- | --- |
| 全局快捷键 | `Ctrl+Alt+C` |
| 开机自启动 | 关闭 |
| 配置文件 | `hotkey.json` |

默认值只对没有 `hotkey.json` 的新用户生效。已有配置会优先读取。

## 使用方式

1. 下载发行版中的 `Middo-v1.0.0-win-x64.exe` 或 `Middo-v1.0.0-win-x64-self-contained.exe`。
2. 如果选择依赖版，确认系统已安装 `.NET 8 Desktop Runtime`。
3. 将 exe 放到你想长期保存的位置。
4. 双击运行后，按 `Ctrl+Alt+C` 将当前前台窗口居中。
5. 右键托盘图标打开设置，可修改快捷键和开机自启动。

## 下载选择

- `Middo-v1.0.0-win-x64.exe`：Windows x64 单文件 .NET 8 依赖版，体积小，需要安装 .NET 8 Desktop Runtime。
- `Middo-v1.0.0-win-x64-self-contained.exe`：Windows x64 单文件自包含版，体积大，不需要额外安装 .NET 运行时。
- 两个发布包都不包含 `.pdb` 调试符号。

## 配置文件

Middo 会在 exe 同目录生成 `hotkey.json`：

```json
{"Modifiers":3,"Key":67,"AutoStart":false}
```

字段含义：

- `Modifiers`: 修饰键位标志，`1=Alt`、`2=Ctrl`、`4=Shift`、`8=Win`，可叠加。
- `Key`: Windows 虚拟键码，例如 `67` 是 `C`。
- `AutoStart`: 是否写入当前用户开机自启。

## 技术栈

| 项目 | 说明 |
| --- | --- |
| 框架 | .NET 8 Windows Forms |
| 语言 | C# |
| 目标系统 | Windows x64 |
| 全局快捷键 | Win32 `RegisterHotKey` |
| 窗口定位 | `GetForegroundWindow`、`MonitorFromWindow`、`GetMonitorInfo`、`SetWindowPos` |
| 自启动 | 当前用户注册表 `HKCU\Software\Microsoft\Windows\CurrentVersion\Run` |
| 配置文件 | exe 同目录 `hotkey.json` |

## 项目结构

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

## 构建

需要：

- Windows
- .NET 8 SDK
- .NET 8 Windows Desktop Runtime

开发运行：

```powershell
dotnet run --project Middo\Middo.csproj
```

预览设置窗口：

```powershell
dotnet run --project Middo\Middo.csproj -- preview
```

发布单文件 .NET 依赖版：

```powershell
dotnet publish Middo\Middo.csproj -c Release -r win-x64 --self-contained false `
  -p:PublishSingleFile=true `
  -p:DebugType=None `
  -p:DebugSymbols=false `
  -p:UseSharedCompilation=false `
  -p:BuildInParallel=false `
  -p:RunAnalyzersDuringBuild=false `
  -o p-dependent-single
```

输出：

```text
p-dependent-single\Middo.exe
```

发布单文件自包含版：

```powershell
dotnet publish Middo\Middo.csproj -c Release -r win-x64 --self-contained true `
  -p:PublishSingleFile=true `
  -p:EnableCompressionInSingleFile=true `
  -p:DebugType=None `
  -p:DebugSymbols=false `
  -p:UseSharedCompilation=false `
  -p:BuildInParallel=false `
  -p:RunAnalyzersDuringBuild=false `
  -o p-self-contained
```

输出：

```text
p-self-contained\Middo.exe
```

说明：

- 依赖版体积小，目标电脑需要安装 `.NET 8 Desktop Runtime`。
- 自包含版体积大，目标电脑不需要安装 `.NET 8 Desktop Runtime`。
- 当前发布策略分发两个单文件 exe，不附带 `.pdb`。
- `hotkey.json` 不随发行包覆盖，用户配置保存在 exe 同目录。

## 许可证

当前仓库未声明许可证。使用或分发前请先根据你的发布需求补充 LICENSE。
