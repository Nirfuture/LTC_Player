# リリース作成

## 1. バージョンと変更点を確定する

`src/LtcPlayer.Avalonia/LtcPlayer.Avalonia.csproj`のVersionと、`RELEASE_NOTES.md`を更新してコミットします。

配布物は指定したコミットからビルドします。作業中のファイルやIDEの設定は含めません。

## 2. 配布ファイルを作る

ビルドに必要な環境を用意し、リポジトリのルートで実行します。

```powershell
./scripts/package-release.ps1 -Version 0.1.0-preview.2
```

通常のdotnetと異なる実行ファイルを使う場合は、`-DotnetPath`で指定できます。過去のコミットを指定する場合は`-Revision`を使います。ソース側のVersionと指定バージョンが一致しなければ停止します。

生成先:

```text
releases/0.1.0-preview.2/
├─ LtcPlayer/                         展開状態のアプリ
├─ LtcPlayer-0.1.0-preview.2-win-x64.zip
├─ LtcPlayer-0.1.0-preview.2-source.zip
├─ SHA256SUMS.txt
├─ RELEASE_NOTES.md
├─ MANIFEST.json
└─ SIGNATURES.json
```

実行用ZIPにはライセンスとAudioNative.dllの対応ソースを同梱します。完全なプロジェクトソースはsource.zipに保存します。MANIFEST.jsonにはバージョンとソースコミット、SIGNATURES.jsonには各EXE・DLLの署名状態を記録します。

既存のバージョンフォルダーは上書きしません。新しい配布には新しいバージョンを使います。失敗した作業は`artifacts/packaging/`に残り、最後まで成功した配布物だけを`releases/`へ移します。

## 3. 確認する

- `LtcPlayer/LtcPlayer.Avalonia.exe`を起動し、使用する機器で動作を確認する。
- 実行用ZIPを別フォルダーへ展開し、同じ操作ができることを確認する。
- MANIFEST.jsonのコミットが公開するソースと一致することを確認する。
- 署名・検証結果と未解決項目を確認する。

`releases/`と`artifacts/`はGitの管理対象外です。公開済みのフォルダーは残しておきます。

## 4. GitHub Releasesへアップロードする

MANIFEST.jsonに記録されたコミットへバージョンのタグを付け、GitHub Releasesを作成します。プレビュー版ではPre-releaseを選びます。

- 実行用ZIP
- source.zip
- SHA256SUMS.txt
- MANIFEST.json
- SIGNATURES.json

本文には同じフォルダーのRELEASE_NOTES.mdを使います。このスクリプトはGitHubへのアップロードや公開を行いません。

## 署名

現時点では未署名プレビューの作成に対応しています。署名サービスへの接続・署名処理はまだ実装していません。

`-RequireSigned`を指定すると、すべてのEXE・DLLに有効な署名がない場合は配布物の確定を拒否します。現状のビルドはこのチェックに通りません。既存の署名済みフォルダーを検査するには、`scripts/test-release-signatures.ps1 -Path <フォルダー>`を使用します。

署名方式の検討状況は[一般利用者向け配布の準備](DISTRIBUTION.md)を参照してください。
