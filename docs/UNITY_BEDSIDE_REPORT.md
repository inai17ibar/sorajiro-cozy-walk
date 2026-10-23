# ベッド脇のベイク照明・布素材の改善

> 2026-10-19追記：以下はv003検証時の記録です。現在は[寝具改修版](UNITY_BEDDING_REPORT.md)へ進んでいます。v003のAPV設定1ファイルは後にUnity OnValidateの4項目更新が入り、旧receiptとの厳密なREUSEは拒否され得ます。元ハッシュとの差の照合と現状は新報告を参照してください。

2026-10-18。M2/M3の一角の画質試作。最新は `Assets/Sorajiro/Generated/Bedside_v003/L_LookDev.unity`。所有者の参考画質への到達・承認は未完了。

## 結果

ベッド・ランプ・窓・床を中心に、実際のGPUベイクと布素材の比較を行った。旧 `Twilight_v010` のシーン・モデル・素材・クリップは保護した入力であり、新しいシーンが参照する。くまの造形や歩行ロジックは今回変更していない。

- [現在の室内実描画](../Artifacts/Visual/bedside/20261018T123249573/S02_Gameplay_Cotton_GI_ON.png)
- [布団と編み物](../Artifacts/Visual/bedside/20261018T123249573/S05_Bedding_Cotton_GI_ON.png)
- [くまの正面](../Artifacts/Visual/bedside/20261018T123249573/S01_Portrait_Cotton_GI_ON.png)
- [30秒の実Play動画](../Artifacts/Visual/motion/20261018T123743855/twilight-motion.mp4)
- [検証記録](../validation/unity_bedside_v003_checks.json)

初回APV版のv002は顔が暗すぎたため保存して比較用に残し、v003で露出と主役用の補光を調整した。v003の寝具・床・壁の陰影、くまの顔の水色と白い口元を実画像で観察。布用シェーダーにより編み目の強い光沢は抑えられたが、編み物の形状や規則性、枕の縁、縫い目、家具付近の光漏れ、くまの短毛・アップリケ・足元は引き続き改善対象。

## 変更したもの

`BedsideCornerBuilder.cs` は保護されたv010をコピーし、以下を新規生成する。

- 部屋の9メッシュに専用のUV2を生成。ライトマップ解像度は64 texels/m、最大アトラス1024、Direct 64 / Indirect 128 / Environment 64 samples、3 bounces。密な毛・ラグ・編み目はベイク対象から外し、APVで照らす。
- Apple Siliconに対応するProgressive GPUでベイク。今回の実バックエンドは **OpenCL / Apple M3 Max**。結果は2枚のライトマップ、APVのベイク済みデータ。全体の記録時間は約13.52秒で、ゲームのフレーム時間とは無関係。
- 既存プロジェクトで有効だったAPVをそのまま使い、シーン内の4.8×2.5×4.3mにLocal Volumeを配置。最小間隔0.25m、専用Baking Setも生成先の中に保存。Engineログは6,612箇所のAPV照明計算を記録している。標準LightProbeGroupの個数とは別の値。
- 布団・枕・カーテン・編み物2種を、導入済みHDRPの `Fabric CottonWool.shadergraph` に切り替え。独自512pxの線形Thread/Fuzzマップを作成。ベースカラー・法線・UVを引き継ぎ、布の反射を調整。シェーダーが輪郭の毛を生成するわけではない。
- ランプ本体をBakedにし、0.12mの光源半径と追加のベイク用矩形光源で静的な影を作る。主役を読むための広いリアルタイム補光を残す。この補光は美術用ライトで、ベイクした間接光とは区別する。
- 固定露出EV6.0、間接Diffuse倍率1.6、AO強度0.55。被写界深度とモーションブラーは無効のまま比較。旧v010はEV6.5なので、旧画像との全体比較には露出変更も含まれる。

`BedsideCornerCapture.cs` は同一カメラ・露出の4画角と比較3枚を取得する。`GI_OFF` はIndirect Diffuse倍率を0にするため、ベイク済みDiffuse・APV・環境光などを含む層の無効化であり、「反射光のバウンスだけを分離した比較」ではない。`Lit_GI_ON` は光を維持して旧布素材に戻す比較。

`ArtReviewAutomation.cs` と `unity_art_review.py` は生成・再読み込み・検証をEditor更新イベント上で実行し、実際の完了ファイルを待つ。CLIの5秒RPC制限がベイク開始時に発生した場合、新しいベイクを重ねて開始せず、すでに作成したジョブを監視する。撮影コマンドのジョブ完了は撮影開始までで、画像の完了は `complete.txt` で別に確認する。

`BearAnimationVerification.cs`、`MotorSmokeTests.cs`、`TwilightMotionCapture.cs` は新シーンでも既存の実動作検証を実行できるようにした。`VerificationTools.cs`、`MotionCaptureProbe.cs` は記録にシーン名とアセット状態を明記する。ゲームの移動・アニメーション計算は変えていない。

`collect_bedside_evidence.py` は生成物のハッシュ、元v010の不変性、実ベイク・比較画像・動作記録を照合する。ffmpegで画像の差を測り、同一画像のYUV黒レベルを差し引く。画像を加工しない。

## 実際の検証

| 項目 | 実行結果・範囲 |
|---|---|
| Runtime/Editorソース | 導入済みUnity参照でRoslynコンパイル成功。共用MotionPolicy 9 / PlushGait 2,012 assertions成功 |
| 生成保護など | Python unittest 21件成功。ゲーム画質の検査ではない |
| GPUベイク | 実M3 Maxで成功。2ライトマップ、APV `HasBakedData=true`、再読み込み後もデータロードを確認 |
| 素材 | 実HDRP Cotton/Woolの5材質・独自Threadマップの割り当てを確認 |
| 実画像 | HDRP / Metal / 1920×1080 / TAA、動的解像度なし。7PNGを保存。正面・寝具・ゲーム画角と移動動画のサンプルを観察 |
| 切り替えの効果 | 8bit輝度差の平均：寝具の照明54.1527、正面の照明33.6687、寝具の素材5.68743。切り替えに画面上の効果がある証拠で、画質の点数ではない |
| 実動作 | 本番CharacterController / Step / Generic Animatorで移動・境界・ジャンプ・着地を検査。歩行0.09333m、ジャンプ頂点0.12635m。Locomotion/Jump/Fall/Landを観測 |
| 30秒記録 | 実Playの433フレームを実時刻でMP4に変換。取得負荷込みで、ゲームFPSの測定ではない |
| 生成物保護 | 完成後の再生成はREUSE。元v010とベイク済みv003のファイルハッシュを検証 |

動作検証・録画は明示的な自動入力とfocus=trueの試験条件を使い、終了時に元のフォーカスへ戻す。実際のキーボード・フォーカスの受け入れ検査とは区別する。

主な記録は `Artifacts/UnityEditor/bedside-v003/`、GPUバックエンド抜粋は `bedside-bake-engine.txt`。生成ジョブは `20261018T123031425_corner-create.json`、ベイクは `20261018T123103780_corner-bake.json`。未保存変更なし・Play停止の状態で終了する。

初回v001は従来プローブ方式で準備したが、APV有効のプロジェクトで必要なデータを生成できず、検証は失敗として記録した。途中シーン・準備記録・その時点のBuilderソースを保持している。v002は実ベイク成功だが暗すぎる比較試作、v003が今回の最新候補。失敗や暗い画像を成功版の証拠として扱わない。

## 未実行・残る課題

Playerビルド、Windows、FPS／メモリ圧迫、長時間安定性、LODと画質切り替え、実フォーカス／キーボード、ゲーム中の寝具変形は今回の対象外。ベイク時のGPU使用や30秒の録画から性能合格を推定しない。APVには照明データを用意したが、部屋全域の光漏れ・移動時の全経路を合格としたわけではない。

アセット状態は **生成済み・実Editorでインポート／ベイク済み・Metal実描画を観察済み・所有者未承認の試作**。参照画像は参照のまま。くま・部屋のBlender編集元は前回のものを保持。新しい有料ソフト、外部サービス、ダウンロード素材は使用していない。大きな生成物と映像は `Artifacts/` とローカルUnityプロジェクトにあり、Git管理外。

次は寝具だけを対象に、編み目の形状と密度、枕のつぶれ・縁、布団の縫い目の接触を修正するのが効果的。照明については家具周辺の光漏れをAPV調整ボリュームやベイク用形状で追い込む。今回のライトと素材を基準に、同一カメラで個別比較する。

## Macで開く・再現する

Unity **6000.6.5f1 / HDRP 17.6.0**、プロジェクトは `$HOME/Documents/Unity/sorajiro-unity`。別の場所なら下記CLIに `--project /絶対パス` を追加。既存のUnity CLI / Pipelineを使い、更新や再導入は不要。

そのまま確認する場合は `Sorajiro > Bedside > 4. Open baked corner` → Play → Gameビューをクリック。WASD / Shift / Space / 右ドラッグ / ホイールで操作する。旧版は `Sorajiro > Twilight` から開ける。

```bash
python3 scripts/unity_art_review.py status
python3 scripts/unity_art_review.py stop
python3 scripts/unity_art_review.py refresh
python3 scripts/unity_art_review.py corner-open
python3 scripts/unity_art_review.py corner-check
python3 scripts/unity_art_review.py corner-capture
```

`status` でcompiling=false / playing=false / dirty=falseを確認する。作成済みのv003は再ベイクで上書きしない。新規環境では元v010の生成を先に済ませ、次の順で初回生成とベイクを完了してから上記のopen/check/captureを実行する。作成済みの場合、corner-createはREUSEの確認だけで終了する。

```bash
python3 scripts/unity_art_review.py corner-create
python3 scripts/unity_art_review.py corner-bake
```

撮影では新しい `Artifacts/Visual/bedside/<UTC>/complete.txt` と7PNGの完了を確認する。続いて動作を記録する。

```bash
python3 scripts/unity_art_review.py corner-play
python3 scripts/unity_art_review.py corner-runtime
python3 scripts/unity_art_review.py motion
```

新しい `Artifacts/Visual/motion/<UTC>/complete.txt` を待ち、実際の新しいフォルダを指定して変換・集約する。

```bash
python3 scripts/encode_unity_motion.py Artifacts/Visual/motion/<UTC>
python3 scripts/unity_art_review.py stop
python3 scripts/collect_bedside_evidence.py --project "$HOME/Documents/Unity/sorajiro-unity" --capture Artifacts/Visual/bedside/<UTC> --bake-job Artifacts/UnityEditor/PipelineJobs/<実際のcorner-bakeジョブ>.json --motion Artifacts/Visual/motion/<UTC>
```

ソースと文書の確認で今回実行したコマンド：

```bash
python3 scripts/check_unity_source.py --editor /Applications/Unity/Hub/Editor/6000.6.5f1/Unity.app --project "$HOME/Documents/Unity/sorajiro-unity"
python3 -m unittest discover -s Tests -p 'test_*.py' -v
python3 scripts/validate_handoff.py
git diff --check
```

Generatorソースや入力が変わる場合は、新しい出力リビジョンを使う。準備中・失敗中の出力や所有者の編集を消して再実行しない。新規環境では前回のBlenderソースアートとv010の生成が先に必要で、[前回の再現手順](UNITY_ASSET_QUALITY_REPORT.md)を参照。
