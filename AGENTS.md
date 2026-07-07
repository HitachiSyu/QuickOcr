# AGENTS.md

本文件面向未来接手本仓库的 AI coding agent / 开发者。请在改动前先阅读本文件，再阅读 `README.md` 和 `docs/HANDOVER.md`。

## 项目概览

Quick OCR 是一个 Windows 桌面 OCR 工具，用于通过全局快捷键或托盘菜单选择屏幕区域并提取文字。当前实现是 WPF/.NET 8 应用，使用 Windows OCR API，不内置 OCR 模型。

用户界面以日文为主。仓库 README 也使用日文。

## 标准工作目录

本地主要工作目录：

```text
D:\CodexWorkSpace\QuickOcr
```

仓库地址：

```text
https://github.com/HitachiSyu/QuickOcr.git
```

不要再使用旧的 `D:\CodexWorkSpace\OCREveryWhere`。该目录已经被废弃并删除。

## 重要目录

```text
assets/
  素材.png                 原始背景素材

dist/
  QuickOcr.exe             当前便携版 exe

src/QuickOcr/
  WPF 应用源码
```

## 构建命令

在仓库根目录执行：

```powershell
dotnet build src/QuickOcr/QuickOcr.csproj -c Release
```

发布便携版：

```powershell
dotnet publish src/QuickOcr/QuickOcr.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o dist
```

如果当前机器没有全局 .NET SDK，但存在以前下载的本地 SDK，可临时使用对应 `dotnet.exe`。不要把本地 SDK 目录提交到仓库。

## 提交前检查

提交或 push 前必须检查：

```powershell
git status --short
```

需要构建验证时执行：

```powershell
dotnet build src/QuickOcr/QuickOcr.csproj -c Release
```

如果使用临时 SDK 或生成了 `bin/obj`，不要提交这些构建产物。

## 不要提交

以下内容不应提交：

- `bin/`
- `obj/`
- `.vs/`
- `.idea/`
- `.dotnet/`
- `quickocr.settings.json`
- `*.pdb`
- 旧版本输出目录
- 临时截图、调试文件、日志

`.gitignore` 已覆盖这些常见内容。若新增构建工具，请同步更新 `.gitignore`。

## 开发注意事项

- UI 文案主要使用日文。
- XAML 中如出现日文编码异常，优先使用 XML entity 或确认文件保存为 UTF-8。
- 设置窗口和结果窗口都使用 `src/QuickOcr/Assets/SettingsBackground.png` 作为背景。
- 图标资源在 `src/QuickOcr/Assets/QuickOcr.ico`。
- OCR 使用 Windows OCR API。不要无讨论地切换到 PaddleOCR/Tesseract，因为那会显著改变体积、依赖和便携性。
- Auto/日本語模式会使用英语 OCR 辅助 URL/英数字行；因此目标机器需要安装英语 Windows OCR 语言包。
- 窗口不应强制置顶。
- 启动 exe 时默认打开设置窗口。托盘双击也打开设置窗口。截图通过快捷键或托盘菜单 `範囲選択` 触发。
- 结果窗口用于显示识别结果，可复制、重新截图、打开设置、关闭。

## 高风险区域

- `WindowsOcrService.cs`：OCR 语言选择、日英双 OCR 合并、小图放大逻辑都在这里。改动后要重点测试 URL、日文正文、日英混合文本。
- `HotkeyManager.cs`：全局快捷键注册。可能与其他软件冲突。
- `SelectionOverlay.cs`：截图选择区域和多显示器/DPI 相关逻辑。
- `App.xaml.cs`：单实例保护、启动行为、托盘行为。

## 未来 AI 接手建议

1. 先读 `README.md`、本文件、`docs/HANDOVER.md`。
2. 再读 `src/QuickOcr/App.xaml.cs`、`WindowsOcrService.cs`、`SettingsWindow.xaml`、`MainWindow.xaml`。
3. 执行 `git status --short`。
4. 改动前确认当前用户需求是 UI、OCR 逻辑、发布、还是文档。
5. 改动后至少运行 build。
6. 发布新 exe 时覆盖 `dist/QuickOcr.exe`，并说明 exe 较大可能触发 GitHub 大文件警告。

