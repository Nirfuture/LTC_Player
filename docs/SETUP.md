# 初回セットアップ

## 必要なソフトウェア

- [Visual C++ x64ランタイム](https://learn.microsoft.com/cpp/windows/latest-supported-vc-redist)
- [FFmpeg・ffprobe](https://ffmpeg.org/download.html)

Visual C++ランタイムはx64版をインストールします。

FFmpegはWindows用ビルドをダウンロードして展開し、`ffmpeg.exe`と`ffprobe.exe`が入ったフォルダーを、ユーザー環境変数の`Path`に追加してください。

<details>
<summary>Pathへの追加方法</summary>

1. `ffmpeg.exe`と`ffprobe.exe`が入っているフォルダーのパスをコピーします。
2. Windowsの検索で「環境変数」と入力し、「環境変数を編集」を開きます。
3. ユーザー環境変数の「Path」を選び、「編集」を押します。
4. 「新規」を押してコピーしたパスを貼り付け、「OK」で閉じます。

</details>

新しく開いたターミナルで次のコマンドを実行し、両方のバージョン情報が表示されることを確認します。

```powershell
ffmpeg -version
ffprobe -version
```

## ダウンロード・起動

1. [ダウンロードページ](https://github.com/Nirfuture/LTC_Player/releases)を開きます。
2. 「Assets」から`LtcPlayer-0.1.0-preview.1-win-x64.zip`をダウンロードします。
3. ZIPを展開します。
4. 展開したフォルダー内の`LtcPlayer.Avalonia.exe`を起動します。

展開したファイルは、同じフォルダーにまとめたまま使用してください。

起動したら、[クイックスタート](QUICKSTART.md)へ進みます。
