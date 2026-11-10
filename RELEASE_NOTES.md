# LTC Player 0.1.0-preview.9

Windows 11 x64向けのプレビュー版です。

## ダウンロード

[Windows用アプリをダウンロード](https://github.com/Nirfuture/LTC_Player/releases/download/v0.1.0-preview.9/LtcPlayer-0.1.0-preview.9-win-x64.zip)

必要なのは、このZIPだけです。

実行用ZIPを展開し、フォルダー内の`LtcPlayer.Avalonia.exe`を起動してください。

[クイックスタート](https://github.com/Nirfuture/LTC_Player/blob/main/docs/QUICKSTART.md)

## 変更点

- 外部出力を開始する前も、LTCに同期した映像を画面内で確認できます。LTC受信中は手動操作より同期を優先し、信号が途切れると映像を保持します。
- 設定画面で蓋画（PNG・JPEG）を登録できます。未登録時は黒です。
- プレビュー下の「蓋画」で、映像と蓋画を指定時間でフェード切り替えできます。0秒で即時切り替え、途中での反転にも対応します。
- 蓋画表示中もLTC同期を続け、解除すると現在位置の映像へ戻ります。プレビューと外部出力へ同じ合成を適用します。
- LTC停止中の確認再生は、出力開始・停止時にキューと再生位置を引き継ぎます。

出力キューのハイライト、ドラッグ挿入線、縦長画面への対応なども含みます。

## 確認が必要な点

別のPCでの動作、長時間の連続稼働、物理ディスプレイ上の同期精度は未確認です。使用する環境で事前に確認してください。

このプレビュー版は未署名です。Smart App Controlが有効な環境では、起動をブロックされる場合があります。

検証状況と未解決項目は[配布前の確認記録](https://github.com/Nirfuture/LTC_Player/blob/main/docs/releases/0.1.0-preview.9.md)を参照してください。

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
