# Unity M0／M1実装・検証報告 — 2026-10-12

M2検査時の追記：所有者がM1の動作を報告しました。旧M1のVolume部品が保存されていない問題も判明し、EV9／効果無効化を再読込後の検証済み設定とは扱えません。M1の起動・物理・実画像の証拠は維持します。M1を上書きせず、[M2](UNITY_M2_REPORT.md)で保存を修正し実検査しました。

所有者のUnity選択に沿い、既存HDRPプロジェクトへ操作土台を導入しました。**Editorでのコンパイル、生成・読み込み、実CharacterControllerのスモーク検査、Metalの仮モデル実描画まで確認しました。完成画質、配布アプリ、性能は未確認です。** M1の完全な手動操作・Mac Player起動ゲートはまだ残ります。UEの以前のソースは保存しています。

変更した主なファイルは`Unity/Packages/com.sorajiro.cozywalk/`のRuntime／Editor／Unity生成の`.meta`、`scripts/check_unity_source.py`、`scripts/link_unity_package.py`、`Tests/Unity/MotionPolicyTest.cs`、`Tests/test_unity_link.py`です。README、AGENTS、要件・マイルストーン・アーキテクチャに現行Unity方針を追記し、旧UE設計を保存しました。`.gitignore`にUnityキャッシュを追加しています。

リポジトリ外では、所有者の許可で既存プロジェクトの`Packages/manifest.json`にローカル依存1件を追加しました。既存依存は保持し、元manifestを`Artifacts/UnityLink/7a3908f2a66b147b0f5560db6d67260687915fd553fac633900c5d78969aa663.manifest.json`へバックアップしました。実Editorが`Assets/Sorajiro/Generated/M1_v001/`にシーン、6材質、Volume、receiptを新規生成しました。SampleSceneは上書きしていません。UPMのlock／LibraryやEditorのローカル状態もUnityが更新します。

観測版はUnity **6000.6.5f1 (3ff58d469c8a)**、Editor表示**6.6**、HDRP **17.6.0**、Input System **1.20.0**です。実描画時のGPU表示は**Apple M3 Max／Metal**でした。M0の版観測と再現手順は用意しましたが、毛方式の対応・実測やPlayer互換性は未確認です。

| 検査 | 実行結果と境界 |
|---|---|
| Unity参照ライブラリによるC#コンパイル | Runtime／Editor PASS。Unity同梱Roslyn使用。Player buildではない |
| 本番MotionPolicyの直接実行 | 9 assertions PASS。斜め速度、非有限入力、カメラ上下限、分割ズーム |
| 接続スクリプト | 3 unittest PASS。既存依存・追加設定の保持、再実行、別依存・版変更の拒否 |
| 実Unity import／コンパイル | Sorajiro.Runtime.dll／Sorajiro.Editor.dll生成、実メニュー出現を確認 |
| 実Editorでの生成・ロード | Preflight→Create→Openを実行。別シーン、Lit材質、Volumeを実生成・再読み込み |
| 生成物の保護 | Unity内で6 checks PASS。空出力、無所有receipt、再利用、版違い、receipt上書き、所有者編集の拒否 |
| 実CharacterController | 本番Stepで接地、歩行／走行／斜め、30／60刻みの移動一致、ジャンプ／着地、空中ジャンプ拒否、外周、開始位置復帰 PASS。同期呼び出しで、実入力・実フレームループ試験ではない |
| Gameビュー | 実Play／Metal描画を目視確認。日本語HUDのフォント警告を修正後、PNG内の3行を確認 |
| キー入力 | Escでpaused=trueになる状態記録あり。短いW入力では位置変化を確認できず、移動キーの実入力試験は保留 |
| Mac Player build／起動 | NOT RUN。今回の範囲はEditor導入と土台の確認。Build Profilesから専用プロファイルで実行する手順を記載 |
| 完成モデル／毛／全身rig／歩行アニメ | 未作成・未インポート。仮形状の実描画を完成品質の検証へ流用しない |
| 4固定ショット・30秒動画・LOD | カメラ配置のみ。比較撮影、動画、LOD試験はNOT RUN |
| 1080p／30fps／p95／GPU時間／メモリ圧力 | NOT RUN。可変GameビューでScreen報告1210×1291、保存PNGは1206×1197。この差は未解決で、内部解像度・アップスケーラーも未確認 |
| Windows／RTX 3070 | NOT RUN。このMacの結果は流用しない |

実行したCLIは[手順](UNITY_START.md)のコード検査・接続dry-run／applyと、`python3 scripts/validate_handoff.py`です。Unityの実行は上部`Sorajiro`メニューからPreflight、Create、Open、GuardTests、MotorSmokeTests、RuntimeStatus、Captureを呼びました。結果一覧は`validation/unity_source_checks.json`と`validation/unity_editor_checks.json`へ記録します。既存のhandoff検査はゲームテストではありません。

実証ファイルは以下です。Artifactsはローカル出力でGit対象外です。Editorの元ログ全体には認証引数等が含まれ得るため共有せず、必要部分だけ抽出します。

- `Artifacts/UnitySourceChecks/`：C#コンパイル、純粋ロジック実行ログ。
- `Artifacts/UnityEditor/guard-tests.txt`：実Unityの6保護検査。
- `Artifacts/UnityEditor/motor-smoke.txt`：歩行0.23333m、ジャンプ上昇0.12635mを含む実物理スモーク結果。
- `Artifacts/UnityEditor/runtime-639273895009972970.json`：最終キャプチャ時のGPU／Metal／カメラ／画面寸法。
- `Artifacts/UnityEditor/runtime-639273891117934160.json`：Esc入力後の停止状態。
- `Artifacts/UnityEditor/baseline-placeholder-editor-639273895009991880.png`：保存して開いた実Gameビュー。**Editor baseline／仮モデル**。S01〜S04の正式比較画像ではありません。

アセット状態は、参照画像＝reference（変更なし）、くま＝generated placeholder／基本描画確認済み（毛・rigなし、未承認）、室内＝generated placeholder／基本描画確認済み、完成用FBX／Blenderモデル／毛＝未作成です。生成シーン本体は実Unityプロジェクト側にあり、このリポジトリにはレシピとソースを置いています。

最も大きい見た目の差は、毛のない球を組み合わせたくま、口・模様・布の不足、強い明暗差と単色の床／ラグです。次はゲーム機能を増やす前に、全身の編集可能なくまメッシュ・UV・部位別材質を作り、同じ場面へインポートします。短毛は小パッチでMetal上の方式比較から始めます。

所有者の次の操作は、生成シーンを開いた状態で▶を押し、Gameビューをクリックして移動・ジャンプ・右ドラッグ・ズーム・Esc・Rを試すことです。Macビルド、資産生成の再現、Blender座標、比較撮影、性能計測の正確な手順とパスは[Unity開発・実機確認](UNITY_START.md)にまとめています。
