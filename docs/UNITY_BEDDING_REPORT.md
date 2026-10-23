# 寝具の形状と接触の改修

2026-10-18〜19。M2/M3の寝具に限定した美術試作。今回の候補は `Assets/Sorajiro/Generated/Bedside_v009/L_LookDev.unity`。参照品質の所有者承認、Playerビルド、性能判定は未完了。

## 実行結果と証拠

- [寝具の実画面](../Artifacts/Visual/bedside/20261019T010208651/S05_Bedding_Cotton_GI_ON.png)
- [室内の実画面](../Artifacts/Visual/bedside/20261019T010208651/S02_Gameplay_Cotton_GI_ON.png)
- [30秒の実Play動画](../Artifacts/Visual/motion/20261019T010409163/twilight-motion.mp4)
- [機械検証の記録](../validation/unity_bedside_v009_checks.json)

| 確認 | 結果・限界 |
|---|---|
| ソース | 導入済みUnity参照によるRuntime/Editor Roslynコンパイル成功。共用MotionPolicy 9 / PlushGait 2,012 assertions成功。Playerビルドとは別 |
| 保護・集約 | Python unittest 26件成功。追加5件は本番のbaseline集約関数で、既知移行のみの判別と所有者編集の拒否を検査 |
| 生成 | 実Blender 5.2でRoom v012の編集元と実FBXを生成。枕の閉じた面構造を検査。dry-run再実行はREUSE |
| GPUベイク | 実M3 Max / OpenCL。3ライトマップとAPV。全ジョブ約19.34秒、Engineログは6,612箇所のAPV照明計算を記録。描画APIのMetalと区別 |
| 保存の安定性 | 実Editorのスクリプト再読み込み→新規APVのみ保存→全ファイルのreceipt照合に成功。Play後の再検査、stable-createのREUSEにも成功 |
| 実描画 | HDRP / Metal / 1920×1080 / TAA / EV6.0 / 動的解像度なし。固定4画角と照明・素材比較3枚。全4画角と動画の歩行・ジャンプ・走行・旋回サンプルを観察 |
| 切り替え | 平均8bit輝度差：寝具のGI 53.9018、正面のGI 33.5902、布素材5.48522。v003寝具画像との差2.24697。画面の変化を示す値で、画質の点数ではない |
| 実動作 | 本番CharacterController / Step / Generic Animatorで検査。歩行0.09333m、ジャンプ頂点0.12635m。境界、空中連続ジャンプの拒否、着地、Locomotion/Jump/Fall/Landを確認 |
| 動画 | 30秒ルートで実Playを424枚取得。最終サンプル29.94311秒。実時刻でMP4化。録画の取得頻度をゲームFPSと呼ばない |

`GI_OFF` はIndirect Diffuse倍率を0にする比較で、ベイク・APV・環境Diffuse等を含む層の無効化。バウンスだけの分離ではない。`Lit_GI_ON` は現在の照明を維持して旧v010の布素材へ切り替える。v003との画像比較は同じカメラと露出だが、形状・遮蔽・APV設定・再ベイクが同時に変わっており、原因を単独には分離しない。

動作試験は明示した自動入力とfocus=trueのfixtureを使い、終了後に元のフォーカス条件へ戻す。実キーボード操作の受け入れ検査とは別。録画後はPlay停止・未保存変更なしのv009を開いた状態に戻す。

記録は `Artifacts/UnityEditor/bedding-v009/` に保存。作成ジョブ `20261019T005753582_stable-create.json`、ベイク `20261019T005926351_stable-bake.json`、録画後検査 `20261019T010539571_stable-check.json`、REUSE `20261019T010634889_stable-create.json`。

アセット状態は **生成済み・実Unityインポート／ベイク済み・Metal実描画を観察済み・所有者未承認の試作**。枕の欠けと二重に見える重なりは候補の画像では抑えられ、強い明色のキルト線は同色の細い糸になった。編み目の規則性、毛布の縁の浮き、布団の足元の折れ・厚みの見え方、局所的な光漏れ・ランプの白飛びは残る。くまの脚の輪郭、足元の浮き感、短毛と雲の接合も未解決。次の小タスクは、くまの脚と足の接地をマスターおよび同一画角・移動動画で修正すること。

## 変更内容

`Tools/Blender/room_twilight_v12.py` は保護されたRoom v007の編集可能なBlenderデータから新しいRoom v012を生成する。くまのモデル・リグ・歩行計算は変更しない。

- 枕は上下2枚の布面と6mmの縁を持つ閉じたメッシュに変更。中央のつぶれ、縁付近のしわ、細い同色の縫い目を追加。実Blenderのメッシュで非多様体エッジがないことを検査。布団との交差を避けるため位置と奥行きを調整。
- 毛布は4角断面の糸から6角断面の丸い糸へ変更。3,840個のループ、端の巻き込みと房を生成する。最終候補では旧v007の毛布配置と糸の軌道を維持し、断面を丸める変更に絞った。折り返しの2接続部を81列で検査し、今回の最大段差は1mm未満。これはサンプリングした位置の連続性で、全メッシュの非交差保証ではない。
- 布団は旧v005で実Clothを110フレーム計算した重力の垂れ・12mm厚を保持。明るい線状の縫い目を細い同色の糸へ変更。今回Clothを再シミュレーションしたわけではなく、ゲーム中の布変形も追加していない。
- 表面追従の試験ではBVHキャッシュで検索を改善したが、糸の伸びが不自然になったため最終候補には採用しなかった。最終v012は旧配置の再現を使い、繰り返しの布面検索は不要。既存出力にはハッシュ確認によるREUSEを使う。

`BeddingStableBuilder.cs` は旧Twilight v010を保護された入力として別シーンへコピーし、新しいRoom v012のFBXと交換する。布・露出・ライトの設定は前回Bedside v003と同じレシピを維持。床板の下に閉じた床下地、壁の接合部に小さい閉じた裏当てを追加し、通常の実メッシュとして描画とベイクに使う。ベッド周囲のAPVにはValidity Threshold 0.25の調整ボリュームを追加。光漏れ対策の導入であって、全域での解消を保証するものではない。

新しいFBX、11枚のUV2付き静的メッシュ、Cotton/Woolの5素材、照明設定、APV Baking Setを生成先に保存する。元v010は完全一致を検査し、旧シーンと途中試作v004〜v008も削除しない。旧v003のAPV自動更新については次節に記録した。ソース変更・所有者の編集・部分出力を検出した場合は再生成を拒否する。

## 途中試作

Room v008 / Bedside v004では、実画像で枕と布団の重なり、毛布上端の浮きを確認した。Room v009 / Bedside v005では枕の重なりは改善したが、毛布の接触計算が折り返しに段差を作った。Room v010 / Bedside v006では折り返しの段差はなくなったが、前側の毛布が布団に隠れた。Room v011 / Bedside v007では外側から表面投影したが、編み糸が伸びるため採用しなかった。v008は旧v007の配置と編み目の軌道を維持し、丸い断面を使った。候補v009も同じ形状を使い、APVの保存を安定化した。これらの版のソース、シーン、ベイク、固定画像は比較用に保存した。候補v009の証拠には流用しない。

Unity Pipelineのベイク呼び出しは5秒の応答期限を超えることがある。v005とv009では最初の応答時点でジョブがまだ見つからずコマンドは失敗したが、その後に同じ要求のジョブが作られて実ベイクが完了した。重複ベイクは開始していない。開始コマンドの終了コードと、実Editorジョブの完了を区別する。

## APV設定の自動更新と保護

集約時に旧Bedside v003の `APV_BakingSet.asset` だけが旧receiptと一致しないことを検出した。導入済みHDRPの `ProbeVolumeBakingSet.OnValidate()` / `ProbeVolumeBakingProcessSettings.Upgrade()` は、設定版0→1、ray origin bias 0→-0.001、collision mask 0→-5、照明シナリオ空→Defaultの4項目を更新する。実ファイルからこの4項目だけをメモリ上で逆変換したところ、旧receiptのSHA-256に完全一致した。その他のv003ファイルは全て旧ハッシュと一致する。旧ファイル・receiptは書き戻していないため、v003がバイト単位で不変とは報告しない。旧generatorの厳密なREUSEは拒否され得るので、最新v009を開く。

v009は標準のSerializedObject経由で照明シナリオを設定し、UnityのOnValidateが更新を完了したことを検査してからベイクする。最終receiptの前に新規APV資産だけを再シリアライズする。旧ファイルの保護検査を緩めて上書きする処理は追加していない。

集約コードは旧baselineについて4項目の逆変換が元SHA-256に一致する場合だけ「Engineの移行のみ、現ファイルは保持」と明記する。旧シーンの編集・追加ファイル・別のAPV変更は拒否する。新規v009と直接入力v010は従来どおり全ファイルの完全一致を要求する。この境界を本番の集約関数を直接呼ぶ5件のfixtureテストで検証した。

## Macで開く・再現する

Unity 6000.6.5f1 / HDRP 17.6.0 / Blender 5.2。プロジェクトは `$HOME/Documents/Unity/sorajiro-unity`。CLIの別パスは `--project /絶対パス` で指定する。

完成済みシーンを開く入口は `Sorajiro > Bedding Final > 4. Open baked bedding`。Play後にGameビューをクリックし、WASD / Shift / Space / 右ドラッグ / ホイールで操作する。

```bash
python3 scripts/unity_art_review.py status
python3 scripts/unity_art_review.py stop
python3 scripts/unity_art_review.py refresh
# 新コマンドが認識されない場合、Play停止・未保存変更なしで reload-scripts を実行
python3 scripts/unity_art_review.py stable-open
python3 scripts/unity_art_review.py stable-check
python3 scripts/unity_art_review.py stable-capture
```

新規環境では先に[元v010の生成手順](UNITY_ASSET_QUALITY_REPORT.md)を実行し、Room v007の実編集元を用意する。まず以下のdry-runで保護状態を調べる。

```bash
python3 Tools/Blender/room_twilight_v12.py
```

初回のRoom v012がまだない場合だけ実Blenderで生成し、続いてUnityでシーン生成・ベイクを行う。部分出力を削除してやり直さず、原因を確認して新しい出力リビジョンを使う。

```bash
'/Applications/Blender 5.2.app/Contents/MacOS/Blender' --background --factory-startup --python Tools/Blender/room_twilight_v12.py -- --write
python3 scripts/unity_art_review.py stable-create
python3 scripts/unity_art_review.py stable-bake
```

動作記録：

```bash
python3 scripts/unity_art_review.py stable-play
python3 scripts/unity_art_review.py stable-runtime
python3 scripts/unity_art_review.py motion
```

新しい `Artifacts/Visual/motion/<UTC>/complete.txt` を待ち、実際のフォルダを指定する。

```bash
python3 scripts/encode_unity_motion.py Artifacts/Visual/motion/<UTC>
python3 scripts/unity_art_review.py stop
python3 scripts/collect_bedside_evidence.py --revision v009 --project "$HOME/Documents/Unity/sorajiro-unity" --capture Artifacts/Visual/bedside/<UTC> --bake-job Artifacts/UnityEditor/PipelineJobs/<実際のstable-bakeジョブ>.json --motion Artifacts/Visual/motion/<UTC>
```

ソースと保護契約の確認：

```bash
python3 scripts/check_unity_source.py --editor /Applications/Unity/Hub/Editor/6000.6.5f1/Unity.app --project "$HOME/Documents/Unity/sorajiro-unity"
python3 -m unittest discover -s Tests -p 'test_*.py' -v
python3 scripts/validate_handoff.py
git diff --check
```

## 未実行の範囲とアセット管理

新しい有料ソフト、第三者素材、Tripoへの送信、クラウドGPUは使用していない。参照画像は参照のまま。独自の編集可能なBlenderモデルと実FBX、Unity生成物を保持する。大きな生成物と画像・映像はGit管理外のローカルデータで、ソースだけから存在を装ったバイナリは作っていない。

Playerビルド、Windows、FPS／GPUフレーム時間／メモリ圧迫、長時間安定性、実キーボードとフォーカス、LOD、ゲーム中の布物理、所有者の画質承認は今回の対象外。室内モデルは2,338,388三角形（約234万）で、旧約204万から増えている。生成成功や録画の枚数から性能合格を推定しない。
