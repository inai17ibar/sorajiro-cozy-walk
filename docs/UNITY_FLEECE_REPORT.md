# 短いフリースの比較試作

2026-10-19。所有者指定の正面マスター `reference/bear_master_20261012.jpg` にある、密な短毛と柔らかい輪郭へ近づけるM2/M3の小タスク。新しい試作は **`Assets/Sorajiro/Generated/Fleece_v005/L_LookDev.unity`**。部屋の保存版は引き続きBedside v009。参考画像と同等の品質に到達した、あるいはゲームへ正式採用したという判定ではない。

## 実画面

| 条件 | 従来 | 今回の候補 |
|---|---|---|
| 中立スタジオ・正面 | [基準](../Artifacts/Visual/fleece/20261019T014821263_studio/F01_Portrait_Baseline.png) | [短毛](../Artifacts/Visual/fleece/20261019T014821263_studio/F01_Portrait_Groomed.png) |
| 表面拡大 | [基準](../Artifacts/Visual/fleece/20261019T014821263_studio/F03_Surface_Baseline.png) | [短毛](../Artifacts/Visual/fleece/20261019T014821263_studio/F03_Surface_Groomed.png) |
| 既存の室内・正面 | [基準](../Artifacts/Visual/fleece/20261019T015222133_room/S01_Portrait_Baseline.png) | [短毛](../Artifacts/Visual/fleece/20261019T015222133_room/S01_Portrait_Groomed.png) |
| 室内・全身 | [基準](../Artifacts/Visual/fleece/20261019T015222133_room/S03_ThreeQuarter_Baseline.png) | [短毛](../Artifacts/Visual/fleece/20261019T015222133_room/S03_ThreeQuarter_Groomed.png) |

上記は実機M3 Max / Unity HDRP / Metalの1920×1080描画。画像生成や写真投影ではない。それぞれの比較内でカメラ・姿勢・露出・照明を固定し、TAAを40回暖機した。スタジオはEV7、室内は従来のEV6なので、両環境をまたいだ明るさの差を毛の改善とは解釈しない。動的解像度なし、スタジオのDOF・Motion Blur・Bloomはオフ。

[30秒の室内Play動画](../Artifacts/Visual/motion/20261019T015302169/twilight-motion.mp4)、[機械検証の記録](../validation/unity_fleece_v005_checks.json)。動画は自動入力・明示したfocus fixtureで取得する。キーボード受け入れ検査と性能測定とは別。

## 変更内容と保護

`FleeceStudyBuilder.cs` で、保護されたTwilight v010の実FBXを別フォルダへコピーし、同じ体・模様・リグ・6クリップを使う比較シーンを生成する。顔、胴体、雲、脚の形状や歩行計算は今回変更していない。新しい毛は実際のSkinnedMeshであり、HDRPが自動生成した毛ではない。

- 元の149,671本の根元から、短く寝た299,342本の細い曲線状リボンを生成。1本は2クアッドで、全体は1,796,052頂点・1,197,368三角形。表面からの浮き0.45〜1mm、接線方向1〜2mm、半幅25〜55µmを試した。これらは制作パラメータで、実物から測定した寸法ではない。
- 表面方向に沿って毛を寝かせ、微小な曲がりと長さの差を加えた。断面方向の法線を変え、細い毛が光を受ける差を作る。根元のスキンウェイトとbind poseを維持する。毛の物理シミュレーションは追加していない。
- 青と白の基布・毛にHDRP Cotton/Woolを試し、既定の織り糸模様は明示的に無効化した。短い湾曲した繊維をコードで描いた1024²の法線・色マップを新規生成。凹凸・毛束の明暗・粗い反射を調整する。元画像の光をテクスチャへ焼き込んでいない。写真・外部モデルのアップロード、購入、追加ダウンロードは行っていない。
- 目・鼻・口と元の樹脂素材を保持。比較用の床、大きな補光、雲を無効にした中立の空を追加。これらはスタジオだけの設定で、既存室内の照明を変更していない。

`FleeceVariantPreview.cs` は本番レンダラーのメッシュと材質を3段階で切り替える。1＝元のLitと元の毛、2＝Cotton/Woolと元の毛、3＝Cotton/Woolと新しい毛。これは見た目を分離する診断用比較で、性能向けの画質段階やLODを完成させたものではない。

`FleeceStudyCapture.cs` はスタジオ11枚、室内4枚を実HDRPのStandardRequestから保存する。室内ではPlay中だけ、対応するbind行列の全要素が1e-5以内で一致することを確認して新しい素材・毛を一時適用する。停止すると元に戻る。**Bedside v009のシーン・ベイク・元モデルを保存し直していない。** `FleeceStudyActions.cs` と `scripts/unity_art_review.py` が生成・確認・撮影を非同期ジョブで実行する。

既存出力は全ファイルのreceiptで照合する。入力の所有者編集、部分出力、異なるレシピを検出した場合は上書きしない。v001〜v004も保存した。レシピを変更して再試作する際は、新しい出力リビジョンとRecipeを使う。

## 毛の方式の比較と残る差

小パッチでは同じリボンを[Fabric](../Artifacts/Visual/fleece/20261019T014821263_studio/F04_Patch_Patch_FabricMesh.png)と[Hair](../Artifacts/Visual/fleece/20261019T014821263_studio/F04_Patch_Patch_HairRibbons.png)で実Metal描画した。Hairは繊維の方向に沿う明暗が強く、今回の設定では暗い櫛目のように見える。全身への採用は保留し、今回の候補はCotton/Wool＋不透明リボンを使う。Hairシェーダーの比較は、専用のstrandレンダラー・Groom・毛の揺れを実装した証明ではない。

途中のv001はFBXで分割された頂点を元の毛と認識できず生成を停止した。v002は小パッチの球面外で平方根を評価したため非有限座標が混じり、画像に毛が出ない無効な比較となった。v003で領域を制限し、非有限座標の拒否と三角形の向きを修正。既定の織り目も除いた。v004は凹凸が大きすぎてしわのように見えたため、v005で密度・強度を調整した。旧画像をv005の証拠には流用しない。

現候補は従来の硬い光沢と針状の輪郭を抑え、表面の細かな方向性を増やした。一方、参考画像にある密な繊維の立体感、細かな自己遮蔽、自然な毛束の乱れにはまだ届かない。近景の口元・雲の縁には細いリボンが見え、室内では青の彩度と陰影も参照と異なる。形状・脚の浮き感も従来からの未解決事項。**生成済み・実インポート済み・Metal実描画を観察済み・所有者未承認の試作**として扱う。

次の小タスクは、顔と口元の小パッチに範囲を絞って、短い繊維の毛束と反射・遮蔽を改善すること。中立光と室内光、旋回時の輪郭で比較し、その後に全身への採用とLOD・負荷検証へ進む。毛数だけでは方式を決定しない。

## 実行した検査と限界

- 導入済みUnity参照によるRuntime/Editor Roslynコンパイル成功。共用MotionPolicy 9 / PlushGait 2,012 assertions成功。Playerビルドとは別。
- 実Editorで新規生成、receipt検査、3段階の本番Applyで実メッシュ／材質参照の一致を検査。全新規毛頂点の有限性、全スキンウェイトの合計とbone indexの範囲を検査。
- 実CharacterController / motor.Stepで移動・ジャンプ・着地、Generic AnimatorでLocomotion / Jump / Fall / Landと地上復帰を確認。スタジオは境界壁を備えないので、この検査から室内の全衝突を保証しない。
- スタジオ11枚＋室内4枚の実画面、30秒ルートの室内Playを420枚保存（実行30.04902秒、最終サンプル29.97115秒）。歩行・ジャンプ・走行・旋回の4時点を観察した。連続動画全体のちらつき合格判定は未了。動画CSVは各取得時点でvariant=2と新しい毛の頂点数を記録する。画像差は切り替えの効果の確認に使い、参考画像との画質スコアやFPSには使わない。
- Python unittest 26件、`git diff --check` を実行。記録の照合には `scripts/collect_fleece_evidence.py` を使用。引き継ぎ構造検査はゲームテストと区別する。

Playerビルド、Windows、FPS・GPU時間・メモリ圧力、LOD・性能向け品質切り替え、実キーボード操作、所有者の見た目の承認は未確認。今回の毛の三角形数は元の約2倍で、性能未測定の試作をそのまま最終設定にはしない。録画の取得頻度をゲームFPSと呼ばない。

## Macで確認・再現する

追加ソフトは不要。実行環境はUnity 6000.6.5f1 / HDRP 17.6.0 / Input System 1.20.0、プロジェクト `$HOME/Documents/Unity/sorajiro-unity`。このリポジトリのUnityパッケージを接続した既存環境を使う。新規環境では先に[Twilight v010の実アセット生成](UNITY_ASSET_QUALITY_REPORT.md)を行う。室内比較には[Bedside v009の生成・ベイク](UNITY_BEDDING_REPORT.md)も必要。

Unityの **Sorajiro → Fleece → 2. Open study** で試作を開き、Play後にGameビューをクリックする。数字1/2/3で比較、WASD・Shift・Spaceで移動・走行・ジャンプ。元の部屋は **Sorajiro → Bedding Final → 4. Open baked bedding**。停止・未保存変更なしの状態でシーンを切り替える。

リポジトリ直下で順番に実行する。各コマンドの完了とPlay切り替えを待つ。別プロジェクトは各呼び出しに `--project /絶対パス` を加える。

```bash
python3 scripts/unity_art_review.py status
python3 scripts/unity_art_review.py stop
python3 scripts/unity_art_review.py reload-scripts
python3 scripts/unity_art_review.py fleece-create
python3 scripts/unity_art_review.py fleece-open
python3 scripts/unity_art_review.py fleece-check
python3 scripts/unity_art_review.py fleece-capture
# Artifacts/Visual/fleece/<UTC>_studio/complete.txt と11枚の完了を待つ
python3 scripts/unity_art_review.py fleece-play
python3 scripts/unity_art_review.py fleece-runtime
python3 scripts/unity_art_review.py stop
python3 scripts/unity_art_review.py stable-open
python3 scripts/unity_art_review.py stable-play
python3 scripts/unity_art_review.py fleece-room-preview
python3 scripts/unity_art_review.py fleece-room-capture
# <UTC>_room/complete.txt と4枚を待ってから録画
python3 scripts/unity_art_review.py motion
# motion/<UTC>/complete.txt を待つ。実際に生成されたパスを指定
python3 scripts/encode_unity_motion.py Artifacts/Visual/motion/<UTC>
python3 scripts/unity_art_review.py stop
python3 scripts/unity_art_review.py fleece-open
python3 scripts/unity_art_review.py fleece-check
```

実行ジョブは `Artifacts/UnityEditor/PipelineJobs/*_fleece-*.json`、実動作ログは `Artifacts/UnityEditor/fleece-*.txt`。Pipelineの呼び出しが時間切れでも実Editorの処理が継続する場合があるため、helperは同じジョブを観察する。重複生成しない。

```bash
python3 scripts/check_unity_source.py --editor /Applications/Unity/Hub/Editor/6000.6.5f1/Unity.app --project "$HOME/Documents/Unity/sorajiro-unity"
python3 -m unittest discover -s Tests -p 'test_*.py' -q
python3 scripts/collect_fleece_evidence.py --project "$HOME/Documents/Unity/sorajiro-unity" --studio Artifacts/Visual/fleece/20261019T014821263_studio --room Artifacts/Visual/fleece/20261019T015222133_room --motion Artifacts/Visual/motion/20261019T015302169
python3 scripts/validate_handoff.py
git diff --check
```
