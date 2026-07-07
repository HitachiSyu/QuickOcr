# Quick OCR 交接说明

## 项目目标

Quick OCR 是一个 Windows 全局截图 OCR 工具。目标是让用户通过快捷键或托盘菜单选择屏幕区域，离线识别其中的文字，并在结果窗口中复制使用。

项目重点：

- Windows 上便携运行
- 离线 OCR
- 日文 UI
- 日文、英文、中文 OCR
- 对日文正文与 URL/英文路径混合场景尽量稳定
- 视觉风格使用蓝白 OCR 插画素材

## 当前技术栈

- C# / .NET 8
- WPF
- Windows Forms `NotifyIcon` 用于托盘
- Windows OCR API (`Windows.Media.Ocr`)
- `System.Drawing` 用于屏幕截图与图像缩放

目标框架：

```text
net8.0-windows10.0.19041.0
```

## 仓库结构

```text
assets/
  素材.png

dist/
  QuickOcr.exe

src/QuickOcr/
  App.xaml
  App.xaml.cs
  AppSettings.cs
  HotkeyManager.cs
  MainWindow.xaml
  MainWindow.xaml.cs
  ScreenCapture.cs
  SelectionOverlay.cs
  SettingsWindow.xaml
  SettingsWindow.xaml.cs
  WindowsOcrService.cs
  QuickOcr.csproj
  Assets/
    QuickOcr.ico
    QuickOcr.png
    SettingsBackground.png
```

## 当前核心功能

- 单实例保护
- 启动 exe 后打开设置窗口
- 托盘常驻
- 托盘菜单：
  - `範囲選択`
  - `設定`
  - `終了`
- 托盘双击打开设置窗口
- 自定义全局快捷键
- 通过全局快捷键或托盘菜单截图选区
- 透明全屏遮罩拖拽选区
- OCR 识别
- 结果窗口显示文本
- 一键复制
- 重新截图
- 设置 OCR 语言模式

## OCR 语言策略

设置项：

- Auto
- Japanese
- English
- Chinese

当前逻辑位于 `WindowsOcrService.cs`。

Auto/日本語模式：

- 主 OCR 使用日文或 Auto 首选语言
- 额外尝试英语 OCR
- 对 URL、邮箱、路径、英数字较多的行优先使用英语 OCR 结果
- 对日文正文优先使用主 OCR 结果
- 如果缺少英语 OCR 语言包，会显示警告

English/Chinese 模式：

- 单语言 OCR
- 不执行英语辅助合并

小图处理：

- 对高度小于 96px 或宽度小于 480px 的截图区域做高质量放大
- 目的是改善单行 URL、小字号文本识别为空的问题

## 重要设计决策

### 使用 Windows OCR API

原因：

- 离线
- 不需要额外模型文件
- self-contained exe 体积仍可接受
- Windows 桌面工具集成简单

代价：

- 依赖用户系统安装 OCR 语言包
- URL/混合语言识别稳定性有限
- 对小图和低清晰截图敏感

### 不使用 PaddleOCR/Tesseract

当前项目目标是轻量、便携、少依赖。PaddleOCR 会显著增大体积，Tesseract 日文/混合文本效果也不一定满足需求。因此暂不使用。

### UI 使用日文

用户希望整体 UI 和提示为日文。README 也已改成日文。

### 背景素材

设置窗口和结果窗口使用同一张蓝白 OCR 素材背景：

```text
src/QuickOcr/Assets/SettingsBackground.png
```

原始素材保留在：

```text
assets/素材.png
```

### 窗口不强制置顶

用户明确不希望窗口强制置顶。不要重新添加 `Topmost="True"`，除非用户明确要求。

## 当前产物

当前便携版：

```text
dist/QuickOcr.exe
```

该 exe 是 self-contained single-file 产物，体积约 80MB。GitHub 会对超过 50MB 的文件给出 warning，但目前未超过 100MB 硬限制。

## 已知限制

- Windows OCR 语言包不是默认都有，尤其是英语/日语/中文 OCR 支持需要用户确认安装。
- Auto/日本語模式依赖英语 OCR 做 URL 辅助；没有英语 OCR 会提示。
- OCR 结果仍可能受截图清晰度、字体、缩放比例影响。
- 多显示器和 DPI 复杂环境未做充分自动化验证。
- 没有自动化 UI 测试。
- 当前发布 exe 直接提交到 Git；长期建议改 GitHub Releases 或 Git LFS。

## 最近开发上下文

最近完成事项：

- README 改为日文
- 仓库整理到 `D:\CodexWorkSpace\QuickOcr`
- 旧工作目录 `D:\CodexWorkSpace\OCREveryWhere` 已删除
- `テスト` 和 `Sugoホームページ` 旧目录曾尝试删除，但当时为空且被进程占用，未删除成功
- OCR 加入日英双识别合并
- 小图 OCR 前放大
- Auto/日本語模式缺少英语 OCR 时提示
- 设置窗口和结果窗口使用素材背景
- 窗口不强制置顶

## 后续建议

- 增加 GitHub Releases，用 release 附件分发 exe
- 考虑从 Git 中移除 `dist/QuickOcr.exe`，改为 release artifact
- 为 OCR 合并逻辑增加可测试的纯函数单元测试
- 增加设置项控制“英语辅助 OCR”开关
- 增加“复制后自动关闭”设置
- 增加开机启动设置
- 更系统地验证多显示器/DPI
- 改善重复启动时唤醒现有实例，而不只是弹提示

