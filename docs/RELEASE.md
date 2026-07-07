# 发布说明

## 当前发布方式

当前发布方式是生成 self-contained single-file Windows x64 exe，并放入：

```text
dist/QuickOcr.exe
```

该 exe 可以在未安装 .NET Runtime 的 Windows 机器上运行，但 OCR 仍依赖目标机器安装 Windows OCR 语言包。

## 发布命令

在仓库根目录执行：

```powershell
dotnet publish src/QuickOcr/QuickOcr.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o dist
```

发布后确认：

```powershell
Get-ChildItem dist
```

应至少包含：

```text
QuickOcr.exe
```

不要提交 `.pdb`。

## 发布前检查

1. 构建确认：

```powershell
dotnet build src/QuickOcr/QuickOcr.csproj -c Release
```

2. 发布：

```powershell
dotnet publish src/QuickOcr/QuickOcr.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o dist
```

3. Git 状态确认：

```powershell
git status --short
```

4. 运行确认：

```powershell
dist/QuickOcr.exe
```

手动确认：

- 设置窗口打开
- 快捷键显示正常
- OCR 言語下拉框正常
- 托盘图标出现
- `範囲選択` 可以开始截图

## GitHub 大文件注意

`dist/QuickOcr.exe` 约 80MB。GitHub 对超过 50MB 的文件会显示 warning，超过 100MB 会拒绝 push。

当前状态：

- 仍可直接提交
- 但长期不推荐

推荐后续改进：

- 使用 GitHub Releases 上传 exe
- 或使用 Git LFS
- 或从仓库中移除 `dist/QuickOcr.exe`，只保留源码和发布说明

## 版本标记建议

当前尚未建立正式版本号策略。建议未来采用：

```text
v0.1.0
v0.2.0
v1.0.0
```

发布流程建议：

1. 更新源码
2. 更新 README/docs
3. build
4. publish 到 `dist`
5. 手动运行检查
6. commit
7. tag
8. push
9. GitHub Release 上传 exe

## 目标机器要求

- Windows 10/11
- 对应 OCR 语言包

推荐 OCR 语言包：

- 日本語
- English
- 中文按需安装

如果用户只复制 exe 到其他电脑：

- 程序可启动
- 但 OCR 能力取决于目标电脑的 Windows OCR 语言包
- 自定义设置需要额外复制 `quickocr.settings.json`

