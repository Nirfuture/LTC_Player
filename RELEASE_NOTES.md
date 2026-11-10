# LTC Player 0.1.0-preview.3

Windows 11 x64向けのプレビュー版です。

## ダウンロード

[Windows用アプリをダウンロード](https://github.com/Nirfuture/LTC_Player/releases/download/v0.1.0-preview.3/LtcPlayer-0.1.0-preview.3-win-x64.zip)

必要なのは、このZIPだけです。

実行用ZIPを展開し、フォルダー内の`LtcPlayer.Avalonia.exe`を起動してください。

[クイックスタート](https://github.com/Nirfuture/LTC_Player/blob/main/docs/QUICKSTART.md)

## 変更点

- `00:00:00:00`から始まるテスト用LTC音声を同梱しました（29.97 DF、約65秒）。
- アプリのアイコンを追加しました。
- 二重起動を防ぎ、再度起動した場合は既存のウィンドウを表示します。
- プレビューの読み込み・再生準備・シークや、配信準備中に動くバーを表示します。
- 配信中のプレビューを表示領域のピクセル数に合わせ、サイズ変更にも追従するようにしました。

## 確認が必要な点

別のPCでの動作、長時間の連続稼働、物理ディスプレイ上の同期精度は未確認です。使用する環境で事前に確認してください。

このプレビュー版は未署名です。Smart App Controlが有効な環境では、起動をブロックされる場合があります。

検証状況と未解決項目は[配布前の確認記録](https://github.com/Nirfuture/LTC_Player/blob/main/docs/releases/0.1.0-preview.3.md)を参照してください。

[対応環境・ファイル形式](https://github.com/Nirfuture/LTC_Player/blob/main/docs/COMPATIBILITY.md) · [困ったとき](https://github.com/Nirfuture/LTC_Player/blob/main/docs/TROUBLESHOOTING.md)

<details>
<summary>開発・配布ファイルの照合</summary>

開発する場合は、リポジトリをクローンしてください。

```sh
git clone https://github.com/Nirfuture/LTC_Player.git
```

環境の準備は[ビルド手順](https://github.com/Nirfuture/LTC_Player/blob/main/docs/BUILD.md)を参照してください。

Assetsには、配布物に対応するソースの控え（`source.zip`）、照合用の`SHA256SUMS.txt`、ビルド情報の`MANIFEST.json`、署名状態の`SIGNATURES.json`も置いています。

</details>
