# 开发说明

## 本地环境

推荐环境：

- Windows 10/11
- .NET 8 SDK
- Windows Desktop 支持
- Git

确认 .NET：

```powershell
dotnet --info
```

如果没有全局 SDK，可临时使用本地下载的 SDK，但不要把 SDK 目录提交进仓库。

## 构建

在仓库根目录执行：

```powershell
dotnet build src/QuickOcr/QuickOcr.csproj -c Release
```

Debug 构建：

```powershell
dotnet build src/QuickOcr/QuickOcr.csproj -c Debug
```

## 运行

构建后可运行：

```powershell
src/QuickOcr/bin/Release/net8.0-windows10.0.19041.0/QuickOcr.exe
```

设置窗口预览参数：

```powershell
src/QuickOcr/bin/Release/net8.0-windows10.0.19041.0/QuickOcr.exe --settings-preview
```

`--settings-preview` 用于只打开设置窗口，方便视觉调试。

## 主要源码说明

### `App.xaml.cs`

负责：

- 单实例保护
- 启动流程
- 托盘菜单
- 托盘双击行为
- 设置窗口打开
- 启动截图流程

当前行为：

- 启动 exe 后打开设置窗口
- 托盘双击打开设置窗口
- 托盘菜单 `範囲選択` 开始截图

### `HotkeyManager.cs`

使用 Win32 `RegisterHotKey` 注册全局快捷键。默认快捷键为 `Ctrl+Shift+O`，可由设置文件覆盖。

### `SelectionOverlay.cs`

全屏透明遮罩，鼠标拖拽选择截图区域。

注意：

- 多显示器/DPI 场景可能需要继续验证
- Esc 可取消

### `ScreenCapture.cs`

使用 `Graphics.CopyFromScreen` 对选区截图。

### `WindowsOcrService.cs`

OCR 核心逻辑。

职责：

- 图像尺寸规范化
- Windows OCR 引擎选择
- 日英双 OCR
- URL/英数字行合并策略
- 结果按行输出

修改风险较高。改动后至少测试：

- 日文句子
- 纯 URL
- 日文 + URL
- 英文短句
- 中文文本
- 小字号截图

### `SettingsWindow.xaml`

设置窗口 UI。

注意：

- 日文 UI
- 背景图
- 圆角半透明输入框/下拉框/按钮
- 快捷键输入框通过 `PreviewKeyDown` 记录组合键

### `MainWindow.xaml`

OCR 结果窗口 UI。

注意：

- 使用同一背景图
- 不强制置顶
- 结果文本框可复制和编辑

## 设置文件

运行后会在 exe 同目录生成：

```text
quickocr.settings.json
```

不要提交该文件。

字段：

- `Hotkey`
- `OcrLanguage`

## OCR 语言包

Windows OCR 依赖系统语言支持。

推荐安装：

- Japanese OCR
- English OCR
- Chinese OCR, if needed

没有英语 OCR 时，Auto/日本語模式会提示，因为 URL/英数字行补正需要英语 OCR。

## Git 注意事项

提交前执行：

```powershell
git status --short
```

不要提交：

- `bin/`
- `obj/`
- `.dotnet/`
- `quickocr.settings.json`
- `*.pdb`
- 临时输出

`dist/QuickOcr.exe` 当前仍提交到仓库，但它接近 GitHub 大文件警告阈值。未来建议转移到 GitHub Releases。

## 编码注意事项

README 和 UI 文案包含日文。若终端显示乱码，不一定代表文件损坏，可能是 PowerShell 输出编码问题。

XAML 中如果需要新增日文，建议：

- 确认文件保存为 UTF-8
- 或使用 XML entity

代码中的日文提示可使用 Unicode escape，避免终端编码影响。

