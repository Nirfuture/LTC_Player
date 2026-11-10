# 一般利用者向け配布の準備

## 現在の状態

一般利用者向けにはGitHub Releasesで署名済みのWindows用アプリを配布する方針です。

現在のプレビュー版は未署名です。Smart App Controlが有効なWindows 11で、実行ファイルがブロックされることを確認しています。署名サービスの契約・審査と、署名済み配布物の作成はまだ完了していません。

## 配布の流れ

1. 公開するソースを確定し、製品用ビルドとテストを実行する。
2. 実行ファイルとDLLに信頼されたコード署名を付ける。既存の第三者署名は維持する。
3. `scripts/test-release-signatures.ps1`で、展開後の全EXE・DLLの署名を確認する。
4. Smart App Controlを有効にした別のWindows 11環境で、ブラウザーからのダウンロード・展開・起動・LTC入力・映像出力を確認する。
5. ライセンスと対応ソースを添付し、署名後の配布物からハッシュを生成する。
6. GitHub Releasesで実行用ZIP・対応ソース・ハッシュを公開する。

署名の検証だけでは、すべてのPCでの起動やSmartScreenの警告が出ないことは保証できません。実際のダウンロード経路で確認します。

## 署名サービスの候補

公開OSS向けのSignPath Foundationを第一候補として審査条件を確認します。利用が承認された事実はまだありません。

- 公開ソースから検証可能な形でビルドする。
- 署名対象と、第三者が提供するバイナリを区別する。
- メンバーの多要素認証と、リリースごとの署名承認を整える。
- 採用後にコード署名ポリシーと役割を公開する。

AudioNative.dllは自作コードとlibltcをリンクしているため、署名対象として扱えるか事前確認が必要です。.NET・Avaloniaなどの依存物についても、同梱ファイルの署名状態と利用条件を確認します。第三者の未署名バイナリを自分の証明書で一括署名しません。

SignPathの審査が通らない場合は、個人・所在地の条件を確認して有償のコード署名を比較します。申請・契約・課金は別途行います。

## 一次資料

- [Smart App Control向けのコード署名](https://learn.microsoft.com/en-us/windows/apps/develop/smart-app-control/code-signing-for-smart-app-control)
- [SignPath Foundationの条件](https://signpath.org/terms)
- [Windowsアプリの署名方式](https://learn.microsoft.com/en-us/windows/apps/package-and-deploy/code-signing-options)
