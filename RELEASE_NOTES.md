# LTC Player 0.1.0-preview.8

Windows 11 x64向けのプレビュー版です。

## ダウンロード

[Windows用アプリをダウンロード](https://github.com/Nirfuture/LTC_Player/releases/download/v0.1.0-preview.8/LtcPlayer-0.1.0-preview.8-win-x64.zip)

必要なのは、このZIPだけです。

実行用ZIPを展開し、フォルダー内の`LtcPlayer.Avalonia.exe`を起動してください。

[クイックスタート](https://github.com/Nirfuture/LTC_Player/blob/main/docs/QUICKSTART.md)

## 変更点

- 一覧の再生・停止ボタンを図形アイコンにし、中央に揃えました。開始TCの数字と区切り文字のフォント・行の高さも揃えました。
- キューのドラッグ中、挿入先を青い線で表示します。行の上半分・下半分で挿入位置を選べます。
- ドラッグを一覧の外へ移動した場合やEsc・フォーカス喪失時はキャンセルします。

LTC停止中の手動出力、出力キューのハイライト、縦長画面への対応なども含みます。

## 確認が必要な点

別のPCでの動作、長時間の連続稼働、物理ディスプレイ上の同期精度は未確認です。使用する環境で事前に確認してください。

このプレビュー版は未署名です。Smart App Controlが有効な環境では、起動をブロックされる場合があります。

検証状況と未解決項目は[配布前の確認記録](https://github.com/Nirfuture/LTC_Player/blob/main/docs/releases/0.1.0-preview.8.md)を参照してください。

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
