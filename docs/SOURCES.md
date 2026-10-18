# 公式資料と確認範囲

確認日：2026-10-02。引用した仕様はその時点の資料。実機での達成画質・FPSを証明する資料ではない。
設計上の目標値、寸法、タスク分割、比較方法は本パックの提案であり、以下の資料から実測値を引用したものではない。

| ID | 一次資料 | 文書で用いた範囲 |
|---|---|---|
| S1 | OpenAI, [Codex Cloud](https://learn.chatgpt.com/docs/cloud) | クラウドでコード編集とコマンド実行、環境、レビューの流れ |
| S2 | OpenAI, [Cloud environments](https://learn.chatgpt.com/docs/environments/cloud-environments) | GitHubリポジトリ選択、Install script/Start skill、Publish、タスク別作業領域、ネットワーク設定 |
| S3 | OpenAI, [AGENTS.md](https://learn.chatgpt.com/docs/agent-configuration/agents-md) | 作業前に読むプロジェクト指示、ルートと下位ディレクトリ、既定32KiB上限 |
| S4 | Epic, [macOS requirements](https://dev.epicgames.com/documentation/unreal-engine/macos-development-requirements-for-unreal-engine) | UE 5.8のMac/Xcodeとレンダリング機能の対応欄 |
| S5 | Epic, [Groom platform support](https://dev.epicgames.com/documentation/unreal-engine/groom-platform-support-in-unreal-engine) | Mac M2以降・SM6、Cards/Mesh、Binding、DOF・ベイク照明・描画経路の制約 |
| S6 | Epic, [Groom scalability and performance](https://dev.epicgames.com/documentation/unreal-engine/groom-scalability-and-performance-with-unreal-engine) | StrandsとCards/Mesh、LOD、補間・影等の性能要素 |
| S7 | Epic, [Lumen performance guide](https://dev.epicgames.com/documentation/unreal-engine/lumen-performance-guide-for-unreal-engine) | 内部解像度とTSR、ソフトウェア方式、計測と品質設定 |
| S8 | Epic, [Editor Python](https://dev.epicgames.com/documentation/unreal-engine/scripting-the-unreal-editor-using-python) | Editor用Python、アセットとレベルの自動化、実行時ゲームへの非適用 |
| S9 | Epic, [Automation Test Framework](https://dev.epicgames.com/documentation/unreal-engine/automation-test-framework-in-unreal-engine) | UE依存のテスト、機能・画像比較の確認 |
| S10 | GitHub, [About Git LFS](https://docs.github.com/en/repositories/working-with-files/managing-large-files/about-git-large-file-storage) | ポインタと実データを分ける大容量ファイル管理 |
| S11 | Epic, [Building Linux container images](https://dev.epicgames.com/documentation/unreal-engine/building-the-linux-container-images-from-source) | 専用環境でのUEコンテナ運用は別途構築事項であること |
| S12 | OpenAI, [Codex CLI](https://developers.openai.com/codex/cli/) | ローカルのファイル編集とコマンド実行を使う開発 |

## 情報の読み方

OpenAIの旧`developers.openai.com/codex/cloud/environments/`は調査時にLegacy資料へ移動していた。現行のCloud environmentsと、旧Setup/Maintenance方式の説明を同じものとして混ぜない。
GPUやmacOS、UE導入済みの提供保証は本調査では確認していない。環境内の実際のツールを確認し、クラウドはソース作業中心に使う設計とした。
UEのMac対応には実験的／ベータの項目がある。対応欄があるだけで所有者のシーンが高速・安定と推定しない。
公式資料にも古い版の説明や曖昧な表記が残り得る。採用版の実コード・テンプレート・公式リリース資料・実機結果を合わせて確認する。

## ユーザーと過去成果物からの情報

主目的、所有機、参照画像、UEネイティブで開発する方向は、この会話に基づく。
旧`Sorajiro_Cozy_Walk_v0_2.zip`のREADMEとAGENTS.mdを読み、旧版がWebGL2のブラウザゲームであること、旧版のオフラインHTML要件と新UE版の要件を分ける必要があることを確認した。
旧版のテスト結果は今回再実行しておらず、UE版の検証証拠にも流用しない。
本パックはCodexへ投入・実行済みの結果ではなく、投入用の要件と手順である。
