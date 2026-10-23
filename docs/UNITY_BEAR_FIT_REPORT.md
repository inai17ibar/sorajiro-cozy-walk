# 前回2の素材を基準にした体型と照明の調整

2026-10-24追記：所有者が選択したのは **BearFit_v001の比較3**。そのモデルを新しいBedsideBear v002へ配置した。[室内への組み込み・実画像・確認手順](UNITY_BEDSIDE_BEAR_REPORT.md)。以下のv002比較試作の記録とは区別する。

2026-10-19。所有者の「2がよい」「正面の毛並み」「脚を広げ、お腹をふくよかに」という指摘への比較試作。最新は **`Assets/Sorajiro/Generated/BearFit_v002/L_LookDev.unity`**。前回Fleece v005の比較2＝Cotton/Wool素材＋従来の約15万本の毛を全モードで維持する。前回3の約30万本の寝た毛へ変更していない。この選好を、今回の体型・照明・動き全体の承認とは扱わない。

## 実画像と結果

| 同じ画角 | 従来 | 照明だけ変更 | 照明＋新体型 |
|---|---|---|---|
| 正面全身 | [基準](../Artifacts/Visual/bear-fit/20261019T024303675/F05_FrontFullBody_Baseline_Fleece2.png) | [面光源](../Artifacts/Visual/bear-fit/20261019T024303675/F05_FrontFullBody_Relit_Fleece2.png) | [候補](../Artifacts/Visual/bear-fit/20261019T024303675/F05_FrontFullBody_Fuller_Relit_Fleece2.png) |
| 表面拡大 | [基準](../Artifacts/Visual/bear-fit/20261019T024303675/F03_Surface_Baseline_Fleece2.png) | [面光源](../Artifacts/Visual/bear-fit/20261019T024303675/F03_Surface_Relit_Fleece2.png) | [候補](../Artifacts/Visual/bear-fit/20261019T024303675/F03_Surface_Fuller_Relit_Fleece2.png) |
| 斜め全身 | [基準](../Artifacts/Visual/bear-fit/20261019T024303675/F02_FullBody_Baseline_Fleece2.png) | [面光源](../Artifacts/Visual/bear-fit/20261019T024303675/F02_FullBody_Relit_Fleece2.png) | [候補](../Artifacts/Visual/bear-fit/20261019T024303675/F02_FullBody_Fuller_Relit_Fleece2.png) |

実M3 Max / Metal / HDRP、1920×1080、TAA暖機40回、動的解像度・DOF・Motion Blur・Bloomなし。上記に加えて正面寄りの固定画角を取得し、合計12枚。生成画像やオフライン描画ではない。前回の基準素材は同じだが、今回の「照明変更」には光源・間接の明るさ・露出の変更を含む。露出は基準EV7、変更版EV6.3。光の方向だけの分離比較とは呼ばない。照明だけの版と新体型版は同じ照明・露出で比較する。

[30秒ルートの実Play動画](../Artifacts/Visual/motion/20261019T024836754/twilight-motion.mp4)、[検証記録](../validation/unity_bear_fit_v002_checks.json)。実行30.04558秒、430枚、最終取得29.97665秒。実時刻でMP4化し、録画頻度をゲームFPSと呼ばない。歩行・ジャンプ・走行・旋回の4時点を目視観察した。連続動画全体のちらつき・動作美術の合格判定は未了。

正面の青布と白布の凹凸、目鼻の大きい反射が従来より読めるようになった。新体型は腹部が丸く、左右の脚の間隔が広がる。一方、参考画像の密な繊維と細かな遮蔽、雲・口元の毛の縁、足裏の形と接地影には差が残る。歩行サンプルの股下には細い伸びも見えるため、スキニングや動作の品質合格とはしない。

## モデルとアニメーション

`Tools/Blender/bear_fit_v1.py` が保護された編集元 `Artifacts/SourceArt/Bear/twilight_v006/Sorajiro_Bear_M2.blend` を実Blender 5.2で読み、新しい `Artifacts/SourceArt/Bear/fit_v001/` に編集可能な `.blend` と本物のFBXを生成した。

- `bear_fit_shape.py` の共通変形で、腹部の中央付近の幅を最大12%、奥行きを最大22%増やす。高さ方向で滑らかに減衰し、顔の高さ22cm以上は形を維持する。前後の腹部断面の測定は約125.87mm→153.56mm。receiptの `body_width_*` は腕を含む高さ断面の全幅であり、腹部だけの幅ではない。
- 左右の足の中心を各8mm外へ移し、中心間隔84mm→100mm。靴のような別パーツを付け足したり、全体を横に拡大したりしていない。身長・足裏の高さは維持。雲・点・縫い目・毛にも同じ連続変形を適用する。
- 骨格の対応箇所にも変形を適用。毛のカスタム法線は変形の逆転置行列で変換し、非有限値と折り返しを拒否する。元のウェイト・UV・骨名を保つ。手作業のリトポロジーやウェイトの仕上げが完了したという意味ではない。
- Unityでは新しいrest poseを読み、既存の本番 `BearAnimationBuilder` からIdle / Walk / Run / Jump / Fall / Landを新規生成した。古い位置キーをそのまま流用して脚幅が戻ることを避ける。移動速度と共通PlushGaitの計算は変更していない。

アセット状態は **生成済み・実Unityインポート済み・Genericリグと実クリップの変形を検査済み・Metal実画面を観察済み・所有者未承認の試作**。参照写真からの投影、外部モデル取得、追加ソフト・有料サービスは使っていない。

## 照明と比較コード

`BearFitBuilder.cs` はFleece v005を保護された入力として別シーンを生成し、元モデルと新モデルを保持する。650lmの長方形の主光源（0.65×0.55m）を斜め前から当て、65lmの柔らかい補光と55lmの縁の光を加えた。主光源は実HDRPの面光源シャドウを有効化。旧Directionalの硬い長い影から面光源の影へ変更し、環境と露出を調整する。既存HDRPパイプライン設定・品質アセットは書き換えていない。

途中のBearFit v001は方向光で毛の凹凸を確認したが、暗い顔と硬い影が残ったためv002で面光源へ変更した。v001のシーンと比較画像、builderソースの控えは残している。v002の証拠には流用しない。

`BearFitPreview.cs` はモデル・光・motorのvisualRoot・Animatorの参照を一緒に切り替える。今回の数字キーは次の意味になる。前回の「2の素材」はどれでも同じ。

1. 従来の体型＋従来の光。
2. 従来の体型＋新しい光。
3. ふくよかなお腹・広げた脚＋新しい光。初期表示。

`BearFitCapture.cs` が4画角×3状態を実描画し、`BearFitActions.cs` と `scripts/unity_art_review.py` が非同期の生成・検査・撮影を行う。`MotionCaptureProbe.cs` のCSVに比較状態と実際のactive model名を記録し、全取得時点で新モデルが有効だったことを確認した。

元のFleece v005、Twilight v010、保存版の部屋Bedside v009は全ファイルのreceipt照合に成功。部屋への新モデルの組み込み・再ベイクは今回行っていない。新旧の出力と編集元を削除・上書きせず、再生成は入力・レシピ・出力のハッシュ一致時だけREUSEになる。

## 実行した検査と限界

| 検査 | 結果・範囲 |
|---|---|
| ソース | 実Unity参照のRuntime/Editor Roslynコンパイル成功。共用MotionPolicy 9 / PlushGait 2,012 assertions成功。Playerビルドとは別 |
| Python | unittest 31件成功。追加5件は本番の変形関数で顔・足裏高さ・脚の平行移動・腹部・対称性・連続性・横方向の折り返しを検査 |
| DCC | 実Blenderで生成・FBX出力。dry-run再実行はREUSE。非有限座標・変形行列の負の行列式を拒否 |
| 実アセット | Import済みFBXの脚中心間隔、bind数、有限座標、shader support、元資産と出力の全ハッシュを検査 |
| 実クリップ | 6クリップ×56曲線のパス結合・変形を実FBXのBakeMeshで検査。足裏の直進接地区間を左右×Walk/Run×13点で検査。補償後の中心ずれは約3〜6µm。旋回や遷移の足滑り合格ではない |
| 実Play | 本番CharacterController/StepとGeneric Animatorで移動・ジャンプ・着地・Locomotion/Jump/Fall/Landを確認。3比較モードのモデル・光・motor・Animator参照を検査 |
| 足と床 | 接地後のIdleスキンの裸布・足裏最低点は床上2.789mm。毛先の高さや、影が自然に接触して見えるかの合格判定とは別 |
| 実画面 | 固定12枚、30秒ルート430枚。画像差で照明・体型切り替えの画面への効果を確認。画質スコアには使わない |

スタジオには境界壁を設けていないため、共用motor検査の範囲チェックを「部屋全体の衝突を合格」とは報告しない。入力は自動入力と明示したfocus fixtureで、実キーボード受け入れ検査とは別。

Playerビルド、Windows、FPS・GPU時間・メモリ圧力、LOD・負荷を変える品質切り替え、全時間の動作安定性、所有者の画質承認は未検証。次の小タスクは、股下・脚の付け根のウェイトと足元の接触影を小さな移動区間で修正し、その後に室内照明での組み込みを比較すること。正面で毛が見えることだけでM3完了にはしない。

## Macで開く・再現する

Unity 6000.6.5f1 / HDRP 17.6.0 / Input System 1.20.0、Blender 5.2。既存プロジェクトは `$HOME/Documents/Unity/sorajiro-unity`。Unityの **Sorajiro → Bear Fit → 2. Open shape and lighting study**。Play後にGameビューをクリックし、数字1/2/3で比較する。WASD / Shift / Space / 右ドラッグ / ホイールで操作できる。作業後はPlay停止・未保存変更なしのv002を残す。

新規環境では先に[前回Fleece v005の入力アセット](UNITY_FLEECE_REPORT.md)を用意する。以下はリポジトリ直下で順番に実行する。既存出力が編集済み・部分生成・レシピ違いなら削除してやり直さず、新リビジョンへ進む。

```bash
python3 Tools/Blender/bear_fit_v1.py
# 初回だけ実Blenderで生成
'/Applications/Blender 5.2.app/Contents/MacOS/Blender' --background --factory-startup --python Tools/Blender/bear_fit_v1.py -- --write
python3 scripts/unity_art_review.py stop
python3 scripts/unity_art_review.py reload-scripts
python3 scripts/unity_art_review.py fit-create
python3 scripts/unity_art_review.py fit-open
python3 scripts/unity_art_review.py fit-check
python3 scripts/unity_art_review.py fit-clips
python3 scripts/unity_art_review.py fit-capture
# Artifacts/Visual/bear-fit/<UTC>/complete.txt と12枚を待つ
python3 scripts/unity_art_review.py fit-play
python3 scripts/unity_art_review.py fit-runtime
python3 scripts/unity_art_review.py motion
# Artifacts/Visual/motion/<UTC>/complete.txt を待ち、実際の生成パスを指定
python3 scripts/encode_unity_motion.py Artifacts/Visual/motion/<UTC>
python3 scripts/unity_art_review.py stop
python3 scripts/unity_art_review.py fit-check
```

各Unityコマンドには別プロジェクト用の `--project /絶対パス` を指定できる。処理とPlay切り替えが完了してから次へ進む。撮影の開始ジョブの成功だけでは撮影完了ではない。Pipelineが5秒の応答期限を超えてもEditor処理が継続する場合があるため、同じジョブを観察し、重複生成しない。

```bash
python3 scripts/check_unity_source.py --editor /Applications/Unity/Hub/Editor/6000.6.5f1/Unity.app --project "$HOME/Documents/Unity/sorajiro-unity"
python3 -m unittest discover -s Tests -p 'test_*.py' -q
python3 scripts/collect_bear_fit_evidence.py --project "$HOME/Documents/Unity/sorajiro-unity" --capture Artifacts/Visual/bear-fit/20261019T024303675 --motion Artifacts/Visual/motion/20261019T024836754
python3 scripts/validate_handoff.py
git diff --check
```

検査ログは `Artifacts/UnityEditor/bear-fit-*.txt`、ジョブは `Artifacts/UnityEditor/PipelineJobs/*_fit-*.json`。Unityアセットの生成先・実Blender編集元・検証用画像と動画を区別して保持する。
