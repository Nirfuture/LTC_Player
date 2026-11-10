# ビルド手順

## 初回準備

Windows 11 x64で、次のものを用意します。

- Git
- .NET SDK 10.0.403（使用するバージョンは`global.json`で指定）
- Visual Studio 2022またはBuild Toolsの「C++によるデスクトップ開発」（MSVC x64とWindows SDKを含む）

PowerShellでリポジトリをクローンし、フォルダーへ移動します。

```powershell
git clone https://github.com/Nirfuture/LTC_Player.git
cd LTC_Player
```

## ビルドする

```powershell
./scripts/build.ps1
```

C++のDLL作成、NuGetパッケージの復元、アプリのビルドをまとめて実行します。

完成したアプリは`artifacts/win-x64/LtcPlayer.Avalonia.exe`です。起動時に必要なFFmpegなどの準備は[クイックスタート](QUICKSTART.md)を参照してください。

ソースを更新した後も、同じコマンドでビルドできます。

<details>
<summary>テストを実行する</summary>

```powershell
./scripts/test.ps1
```

検証用アプリをビルドし、テスト用の動画を生成して実行します。小さい出力ウィンドウと合成LTCを使用します。

FFmpeg・ffprobeをPathに追加しておいてください。FFmpegにはlibx264、AAC、HAP、PCM、MP3（libmp3lame）のエンコーダーが必要です。

結果は`artifacts/tests-日時/`へ保存します。検証状況と既知の未解決項目は[配布前の確認記録](releases/0.1.0-preview.2.md)を参照してください。

</details>

<details>
<summary>DLLを個別にビルド・差し替えする</summary>

通常のビルドでは、両方のDLLを自動で作成します。個別に作成する場合は、リポジトリのルートで次を実行します。

| DLL | コマンド | 出力先 |
|---|---|---|
| AudioNative.dll | `./native/build-phase2.cmd` | `native/build/AudioNative.dll` |
| PlayerNative.dll | `./native/phase5/build.cmd` | `native/phase5/build/PlayerNative.dll` |

AudioNative.dllは`native/Audio.cpp`と`third_party/libltc/src`のソースを使用します。libltcの元ソース・著作権・ビルド関連ファイルは`third_party/libltc`に含まれます。

作成したDLLをアプリと同じフォルダーへ配置すると差し替えられます。変更したDLLも、アプリと互換性のあるインターフェースを維持してください。

PlayerNative.dllはGPU描画用で、libltcをリンクしません。

</details>

<details>
<summary>ビルドの補足</summary>

- NuGetロックファイルを使用して依存パッケージを復元します。
- 通常のビルドは検証コードを除外し、`--probe-output`を無効にします。検証用にビルドする場合は`./scripts/build.ps1 -Verification`を使います。出力先は`artifacts/verification/`です。
- 別の.NET実行ファイルを使う場合は、`./scripts/build.ps1 -DotnetPath <dotnet.exeのパス>`で指定します。
- ランタイムを同梱し、単一ファイル化・トリミングは行いません。DLLを個別に差し替えられる構成です。
- Avaloniaのビルド統計送信と.NET初回HTTPS証明書の生成を抑止しています。
- C++部分はVisual Studio 2022のMSVCで検証しています。.NET 10をIDEで扱う場合は、対応するVisual Studioを使用してください。上記のコマンドはPowerShellから実行できます。

配布ファイルを作る場合は[リリース作成](RELEASING.md)を参照してください。FFmpegのバイナリを追加して配布する場合は、そのビルドに対応するライセンスと対応ソースも用意します。

</details>
