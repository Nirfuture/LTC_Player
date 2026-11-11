# クイックスタート

LTC Playerは、LTC（音声タイムコード）に合わせて動画を再生するWindows用ツールです。複数の動画をキューに登録し、動画ごとに再生を開始するタイムコードを指定できます。

手動再生、外部ディスプレイへの映像出力、動画の音声出力、蓋絵への切り替えにも対応しています。

## 動作環境

Windows 11 x64に対応しています。初めて使用するPCには、[Visual C++ランタイム](https://learn.microsoft.com/cpp/windows/latest-supported-vc-redist)と[FFmpeg・ffprobe](https://ffmpeg.org/download.html)を導入します。

<details>
<summary>導入手順</summary>

[Microsoft公式のダウンロード案内](https://learn.microsoft.com/cpp/windows/latest-supported-vc-redist)で、最新のVisual C++ v14ランタイムの一覧から「X64」を選びます。ダウンロードしたインストーラーを実行します。

FFmpeg・ffprobeは次の手順で設定します。

1. [FFmpegのダウンロードページ](https://ffmpeg.org/download.html)からWindows用ビルドを取得し、展開します。
2. `ffmpeg.exe`と`ffprobe.exe`が入った`bin`フォルダーを開き、アドレスバーのパスをコピーします。
3. Windowsの検索で「環境変数」と入力し、「環境変数を編集」を開きます。
4. ユーザー環境変数の「Path」を選び、「編集」→「新規」の順に押します。コピーしたパスを追加し、「OK」で閉じます。

新しいターミナルで次を実行し、両方のバージョン情報が表示されることを確認します。

```powershell
ffmpeg -version
ffprobe -version
```

LTC Playerが起動している場合は、終了して起動し直します。

</details>

## 起動

[LTC Player 0.1.0-preview.10（Windows x64）をダウンロード](https://github.com/Nirfuture/LTC_Player/releases/download/v0.1.0-preview.10/LtcPlayer-0.1.0-preview.10-win-x64.zip)

ZIPをすべて展開し、`LtcPlayer.Avalonia.exe`を起動します。

## キュー登録と手動再生

<a href="images/quickstart-queue.png"><img src="images/quickstart-queue.png" alt="キュー登録と手動再生。①キュー追加、②開始TC、③有効、④再生操作、⑤シークバー、⑥音声ON/OFF。" width="680"></a>

① **キュー追加**：動画を登録します。一覧へのドラッグ＆ドロップでも追加できます。

② **開始TC**：再生を開始したいタイムコードを入力します。

③ **有効**：再生する行にチェックを入れます。チェックを外した行は、LTC受信時には再生されません。

④ **確認**：▶で手動再生、Ⅱで一時停止、■で停止します。

⑤ **シークバー**：再生位置を変更します。

⑥ **音声ON/OFF**：音声出力を切り替えます。出力先は設定の「音声出力」で選択します。

> [!WARNING]
> **注意：LTC信号の受信中は、手動再生・シーク操作が無効になります。**

> [!NOTE]
> 対応形式はMP4/H.264、MOV/HAP・HAP Qです。

## LTC同期

LTC入力機器をPCに接続し、画面右上の設定アイコンから「LTC入力」を開きます。

画像の①〜③を設定し、④「適用」を押します。

1. デバイス：LTCを入力する機器
2. チャンネル：LTCを接続した側のLまたはR
3. 形式：送信元のフレームレートとDF/NDFに合わせた形式

<a href="images/quickstart-input.png"><img src="images/quickstart-input.png" alt="LTC入力。①デバイス、②チャンネル、③形式を選び、④適用。機器名は例示。" width="480"></a>

同梱のテスト動画を使用する場合は、送信元とアプリのLTC形式を`29.97 DF`に設定します。最初の動画の開始TCは`00:00:05;00`です。

設定画面を閉じ、送信元からLTCを送ります。「LTC受信中」が緑色になり、プレビューに動画が表示されることを確認します。

## 外部出力

外部ディスプレイをPCに接続し、設定の「映像出力」を開きます。①出力先ディスプレイを選び、②「適用」を押します。

<a href="images/quickstart-display.png"><img src="images/quickstart-display.png" alt="映像出力。①ディスプレイを選び、②適用。機器名は例示。" width="480"></a>

設定画面を閉じ、画面右上の③「出力開始」を押します。プレビューと同じ映像が外部ディスプレイに表示されます。

<a href="images/quickstart-output-start.png"><img src="images/quickstart-output-start.png" alt="画面右上の③出力開始ボタン。" width="400"></a>

外部映像を停止するには、画面右上の「出力停止」を押し、表示される確認画面で「出力を停止」を選びます。

## 蓋絵

蓋絵は、動画の代わりに表示する静止画です。異常発生時に手動で映像を隠したり、待機中にイベントロゴを表示したりする際に使用します。メイン画面の「蓋絵」で操作します。

画像を登録していない場合は、黒画面に切り替わります。

1. 外部出力を停止しているとき、「変更」を押すことで、PNG/JPEG画像を登録することができます。
2. 「フェード」で切り替えにかける秒数を指定します。0秒にすると即時に切り替わります。
3. 「蓋絵を表示」で静止画に切り替え、「映像に戻す」で動画に戻します。

蓋絵の表示中も、動画の再生と音声出力は継続します。

## 保存

「保存」でキュー構成をJSONファイルに保存します。次回は「読込」で開きます。

> [!WARNING]
> **注意：自動保存はありません。終了前に保存してください。**

[操作リファレンス](USER_GUIDE.md) · [トラブルシューティング](TROUBLESHOOTING.md) · [対応環境・動画形式](COMPATIBILITY.md)
