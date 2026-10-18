# ソース・アセット・自動化の構成

2026-10-12：現在のUnity版は`Unity/Packages/com.sorajiro.cozywalk/Runtime`と`Editor`に分離したローカルUPMパッケージです。[Unity手順](UNITY_START.md)が現行構成、以下は保存したUE設計です。

## 1. 予定するリポジトリ

以下は構成案です。M0・M1で`Unreal/`のCharacter／Controller／GameMode／診断HUD、`Tools/Unreal/`の最小マップ生成、`Tools/Pipeline/`の保護検査、`Tests/`を追加しました。これらは未UEビルドです。SaveGame、星、品質設定、Editor module、SourceArt、全身rig、Groomはまだありません。[実装報告](M0_M1_REPORT.md)に現状を記録します。

```text
AGENTS.md
REQUIREMENTS_JA.md
project.environment.json
Unreal/
  Sorajiro.uproject
  Config/
  Source/
    Sorajiro/                 # runtime: gameplay, camera, save, settings
    SorajiroEditor/           # optional: editor-only asset utilities
    Sorajiro.Target.cs
    SorajiroEditor.Target.cs
  Content/
    Sorajiro/                # hand-authored production content
    Generated/               # reproducible editor-generated content
  Plugins/                   # only approved project plugins
SourceArt/
  Bear/                      # editable meshes, textures, groom source
  Room/
Tools/
  Unreal/                    # runs inside the real UE Editor
  Blender/                   # runs inside the actual Blender version
  Pipeline/                  # engine-independent validation/conversion
Tests/
  Pure/                      # same pure logic used in production
  Fixtures/
docs/
prompts/
reference/
scripts/
Artifacts/                   # local outputs; normally ignored by Git
```

Epic標準機能をまず利用する。汎用ゲームフレームワーク、独自物理、独自GI、Engine forkをこの小さなゲームの前提にしない。

## 2. Runtimeの責務

| 構成例 | 役割 |
|---|---|
| `ASorajiroCharacter` | 移動、速度、ジャンプ、状態、Skeletal Meshの接続 |
| `ASorajiroPlayerController` | 入力、UIへの切り替え、ポーズ、フォト操作 |
| SpringArm + Camera | 衝突回避、追従、ズーム、ゲームカメラ |
| `ASorajiroGameMode` | 小さなゲームの開始と終了条件 |
| `USorajiroSaveGame` | 安定IDによる星・進行・設定の保存 |
| `AStarCollectible` / RestSpot | 回収と休憩。重複処理しない |
| Quality settings component | 毛・影・解像度のプロファイルを明示的に適用 |
| UI / Animation Blueprint | 表示とアニメーションの調整。重いロジックを分散しすぎない |

クラス名は設計案で、より単純な構成にしてよい。すべてに不要なTickを付けない。
MacにないWindows専用プラグイン、DLSS専用の設計、GroomのハードウェアRT必須化を避ける。
再起動が必要な描画設定とランタイム切り替え可能な設定を区別する。未対応CVarを黙って無視して設定成功としない。

## 3. UEと非UEのテスト境界

UEコードのコンパイルには対応するEngine、Build Tool、ヘッダー生成等が必要。クラウドの一般C++コンパイラだけで`UCLASS`を含むUEプロジェクト全体を正式に検証したとしない。
純粋C++へ分ける対象は、星のID重複判定、進行状態など実用的な小部分に限る。Runtimeが使わない別実装をテストし、ゲームを検証したと称しない。

独立Pythonで行うものはJSON、参照ファイル、数値範囲、変換レシピの検査。`unreal`を使う処理はEditor、`bpy`を使う処理はBlender内に限定する。[S8][S9]
テストがあるだけでなく「どこで実行したか」を結果に保存する。

## 4. アセット生成

Editor Pythonで行う候補は、インポート、Material／Material Instanceの組み立て、Actor配置、カメラ、マップ保存、アセット一覧取得。必要に応じてEditor C++を小さく追加する。[S8]
選んだUE版で公開されているAPIを調べ、生成後にロードして検証する。Blueprintグラフ等の複雑な編集を、未確認のPython APIだけで可能と断言しない。
自動化できない箇所は具体的なEditor操作と保存先を文書化し、手作業であることを明示する。

生成スクリプトには以下を要求する：

- 入力manifestと出力先、乱数seed、ツール版を記録する。
- dry-run/preflightで入力存在、対応プラグイン、既存資産の状態を確認する。
- 再実行でActorやMaterialを重複生成しない。
- `/Game/Generated/`以外の手編集データを無断上書きしない。
- 生成済みでもロード、Shader、Cookが未確認なら、状態を分ける。

通常の外部Pythonから`pip install unreal`等でEditor APIを置き換える案は採らない。
`.uasset`／`.umap`は本物のEngineで生成・保存する。ダミーの拡張子ファイルを作らない。

## 5. Groomの資産経路

まず小さな毛パッチで、DCC→エクスポーター→ファイル→UE Groom Import→Binding→動くSkeletal Meshまで通す。
導入するGroom出力プラグインの対応Blender版、属性、root UV、座標、ライセンス状態を記録する。必要なデータを輸出できない場合は別ツールやCardsを検討し、勝手に有料購入しない。

本格Groomの一方でCards／Mesh fallbackも残す。通常のゲーム距離で良い見た目が得られるかを先に見る。毛の本数の単純増加は最適化ではない。
Skeletal Mesh更新後はBindingの再作成／検証が必要かを明示し、旧Bindingのまま浮く毛を出さない。

## 6. バージョン固定

`project.environment.json`は現在は未確定。M0で以下を観測して更新する。

UEのmajor/minor/patch・配布元・Build ID、macOS、Xcode、Macアーキテクチャ、Blender、追加プラグイン版、Git LFSの使用、機能対応結果。
Engineインストールの絶対パスや個人アカウント情報はローカル環境変数に置く。全員に同じ絶対パスを強制しない。
複数のUEを使う場合は既存プロジェクトを上書き変換しない。版の変更は別PRと再検証を必要とする。

## 7. Gitと大きな資産

`templates/gitignore.example`と`templates/gitattributes.example`をたたき台にする。LFSは利用できることを確認してから有効化する。
UEアセットやAlembicなどをLFSにし、エンジン本体・キャッシュ・中間生成物はGitへ入れない。参照画像は少量なので初期は通常のGitで扱い、クラウドから開きやすくする。
LFSはGit上のポインタと実データを分ける仕組みである。ポインタが存在するだけで、実データが取得済みとは判断しない。[S10]

`Build/`全体を機械的に除外しない。プロジェクト用のアイコンやプラットフォーム資産が含まれる場合がある。
アセット編集は同じ`.uasset`を複数タスクで同時変更しない。C++・テキストレシピを中心に並列化する。

## 8. データと配布

ゲーム本体はオフラインで動作する。画像や動画、プレイ履歴を外部へ自動送信しない。
参照画像はアート目標であり、ゲーム起動中にロードすることを必須にしない。配布可能性を確認するまで、コードと一括で公開ライセンスを付けない。
Steam等のストア配布・コード署名・公証は初期MVPのあと。ローカルパッケージの起動と一般公開配布の要件を混同しない。
