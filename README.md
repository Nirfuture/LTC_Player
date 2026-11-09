# LTC Player 0.1.0-preview.1

Windows 11 x64向けのLTC同期映像プレイヤー。身内向け検証版。

- 利用者: GitHub Releases用のwin-x64 ZIPを展開し、[簡単な使い方](docs/QUICKSTART.md)を参照。
- 仕様: [最終仕様](docs/FINAL_SPEC.md)、[互換性](docs/COMPATIBILITY.md)。
- 開発者: [ビルド手順](docs/BUILD.md)。ソリューションはLtcPlayer.sln。
- 自作アプリ/描画はMIT、libltc音声DLLはLGPL。LICENSE、NOTICE.md、LICENSESを参照。

FFmpeg/ffprobeとVC++ x64ランタイムは別途必要。.NET 10ランタイムは配布ZIPへ同梱。ネットワーク配信機能ではなく、指定ディスプレイへの映像送出を行う。

物理的同期精度、長時間稼働、他PCでの動作は未保証。公開前の残課題はPRE_RELEASE_REVIEW.mdを参照。

## 配布ファイル
- [Windows x64実行用ZIP](https://github.com/Nirfuture/LTC_Player/releases/download/v0.1.0-preview.1/LtcPlayer-0.1.0-preview.1-win-x64.zip)
- [対応ソースZIP](https://github.com/Nirfuture/LTC_Player/releases/download/v0.1.0-preview.1/LtcPlayer-0.1.0-preview.1-source.zip)
- [Releaseとハッシュ](https://github.com/Nirfuture/LTC_Player/releases/tag/v0.1.0-preview.1)

非公開リポジトリのため、リンクを開くには招待されたGitHubアカウントでのログインが必要です。

