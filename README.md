# Quick OCR

## Assets Preview

<p>
  <img src="src/QuickOcr/Assets/QuickOcr.png" alt="Quick OCR icon" width="96">
</p>

![Settings background](src/QuickOcr/Assets/SettingsBackground.png)

Quick OCR は、画面上の指定範囲から文字を素早く抽出するための Windows 向け OCR ツールです。

オフライン利用を前提としており、OCR には Windows OCR API を使用しています。日本語、英語、中国語、および URL や課題管理ツールのリンクを含む日本語/英語混在テキストの読み取りを想定しています。

## 主な機能

- グローバルショートカットキーによる範囲選択 OCR
- タスクトレイ常駐
- 日本語 UI
- Windows OCR API によるオフライン OCR
- OCR 言語モード: 自動 / 日本語 / 英語 / 中国語
- 自動/日本語モードでは、URL や英数字が多い行に英語 OCR を補助的に使用
- 可能な範囲で改行を保持
- OCR 結果をコピー可能なウィンドウで表示
- ショートカットキーをカスタマイズ可能
- 多重起動防止
- .NET ランタイム同梱のポータブル exe

## 実行方法

現在のポータブル版は以下に配置されています。

```text
dist/QuickOcr.exe
```

`QuickOcr.exe` を起動すると設定画面が開きます。OCR 言語とショートカットキーを設定したあと、ショートカットキー、またはタスクトレイメニューの `範囲選択` から OCR を開始できます。

## Windows OCR 言語パックについて

このアプリには OCR モデルを同梱していません。Windows にインストールされている OCR 言語サポートを使用します。

推奨する Windows OCR 言語パック:

- 日本語
- 英語
- 中国語が必要な場合は中国語

自動/日本語モードでは、URL や英数字が多い行の補正に英語 OCR を使用します。そのため、英語 OCR 言語パックが未インストールの場合は警告を表示します。

## 基本的な使い方

1. `QuickOcr.exe` を起動します。
2. 設定画面で OCR 言語とショートカットキーを確認します。
3. ショートカットキー、またはタスクトレイメニューの `範囲選択` を実行します。
4. 認識したい画面範囲をドラッグして選択します。
5. OCR 結果ウィンドウに認識結果が表示されます。
6. 必要に応じて `コピー` を押して結果をコピーします。

初期ショートカットキー:

```text
Ctrl + Shift + O
```

## 設定ファイル

ユーザー設定は exe と同じフォルダに保存されます。

```text
quickocr.settings.json
```

このファイルは Git 管理対象外です。

## ソースからビルドする

必要環境:

- Windows 10/11
- .NET 8 SDK
- Windows Desktop 対応の .NET SDK 環境

ビルド:

```powershell
dotnet build src/QuickOcr/QuickOcr.csproj -c Release
```

自己完結型の単一 exe として発行:

```powershell
dotnet publish src/QuickOcr/QuickOcr.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o dist
```

## ディレクトリ構成

```text
assets/
  素材.png                 背景素材

dist/
  QuickOcr.exe             現在のポータブル版

src/QuickOcr/
  WPF アプリケーション本体
```

## 注意事項

- 現在は Windows 専用です。
- OCR 精度は Windows にインストールされている OCR 言語パックに依存します。
- 日本語本文と URL/英語が混在する画面を認識する場合は、日本語と英語の OCR 言語サポートを両方インストールしてください。
- `dist/QuickOcr.exe` はサイズが大きいため、将来的には GitHub Releases または Git LFS での配布に切り替える可能性があります。
