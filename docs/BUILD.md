# ビルドと対応ソース

必要環境: Windows 11 x64、.NET SDK 10.0.403（global.json/latestPatch）、MSVC x64 C++ツール/Windows SDK、PowerShell。nativeはVS2022 MSVCで検証。SDK10のIDE利用は対応するVisual Studioを用意するか、CLI/VS Codeを使う。

1. リポジトリ全体を展開/cloneする。TestMediaとthird_party/libltcも必要。
2. PowerShellで ./scripts/build.ps1 を実行。成果物はartifacts/win-x64。
3. 検証は ./scripts/test.ps1。外部FFmpegにlibx264、AAC、HAP、PCM、MP3の各エンコーダーが必要。テストは小さい出力ウィンドウと合成LTCを使用する。

ビルドはNuGetロックファイルを使用し、ネイティブDLLをソースから再生成する。Avaloniaビルド統計はUsedAvaloniaProductsを空にして送信せず、.NET初回HTTPS証明書の生成もスクリプト内で抑止する。Windows実行ポリシーを変更する必要がある場合は勝手に変えず、環境の管理者へ確認する。

AudioNative.dllの再リンク: x64 MSVC環境で native/build-phase2.cmd。native/Audio.cppとthird_party/libltc/srcのltc.c/decoder.c/encoder.c/timecode.cを使用する。出来たnative/build/AudioNative.dllを配布フォルダーへ配置すれば差し替えられる。libltcの元ソース/著作権/ビルド関連ファイルはthird_party/libltcに同梱する。

自作PlayerNative.dllはnative/phase5/build.cmdでビルドする。GPU描画用でlibltcをリンクしない。

製品ビルドでは検証クラスを除外し、--probe-outputは無効。検証コードを含める場合のみEnableVerification=trueを指定する。単一ファイル化/トリミングは使用しない。ランタイムとDLLの置換性を維持する。

公式SDKのインストールやVC++再頒布物の条件は各提供元に従う。FFmpegのバイナリを追加して配布する場合は、そのビルドに対応するライセンスと対応ソースを別途揃える必要がある。
