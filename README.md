# Quick OCR

Quick OCR is a small Windows desktop OCR tool for quickly extracting text from a selected screen region.

The app is designed for offline use and uses the Windows OCR API. It is especially intended for Japanese text, English text, Chinese text, and mixed Japanese/English screenshots such as URLs, issue tracker links, chat messages, and short document snippets.

## Features

- Global hotkey screen-region OCR
- Tray app with capture/settings/exit menu
- Japanese UI
- Offline OCR through Windows OCR
- OCR language modes: Auto, Japanese, English, Chinese
- Auto/Japanese mode uses English OCR assistance for URL and ASCII-heavy lines
- Preserves OCR line breaks where possible
- Copyable OCR result window
- Custom global hotkey
- Single-instance protection
- Portable self-contained Windows executable

## Download / Run

The current portable build is included at:

```text
dist/QuickOcr.exe
```

Run `QuickOcr.exe`, configure the hotkey/language in the settings window, then use the hotkey or tray menu item `範囲選択` to select a screen region.

## Important: Windows OCR Language Packs

This app does not bundle OCR models. It depends on OCR language support installed in Windows.

Recommended Windows OCR language packs:

- Japanese
- English
- Chinese, if needed

Auto/Japanese mode uses English OCR as an auxiliary pass for URLs and ASCII-heavy lines. If English OCR support is not installed, the app will show a warning.

## Default Usage

1. Start `QuickOcr.exe`.
2. The settings window opens.
3. Choose OCR language mode.
4. Set or confirm the global hotkey.
5. Use the hotkey, or right-click the tray icon and choose `範囲選択`.
6. Drag a screen region.
7. The OCR result appears in a copyable result window.

Default hotkey:

```text
Ctrl + Shift + O
```

## Settings File

User settings are saved beside the executable:

```text
quickocr.settings.json
```

This file is intentionally ignored by Git.

## Build From Source

Requirements:

- Windows 10/11
- .NET 8 SDK with Windows Desktop workload support

Build:

```powershell
dotnet build src/QuickOcr/QuickOcr.csproj -c Release
```

Publish a self-contained single-file executable:

```powershell
dotnet publish src/QuickOcr/QuickOcr.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o dist
```

## Repository Layout

```text
assets/
  素材.png                 Original background material

dist/
  QuickOcr.exe             Current portable build

src/QuickOcr/
  WPF application source
```

## Notes

- The app is currently Windows-only.
- OCR quality depends on the installed Windows OCR language packs.
- For mixed Japanese and URL/English text, install both Japanese and English OCR support.
