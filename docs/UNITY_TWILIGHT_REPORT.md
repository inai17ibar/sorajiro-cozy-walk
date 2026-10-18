# マスターくま・歩行とジャンプ・寝具の制作試作

2026-10-12〜13、所有者のMacでBlender生成、実Unity import、Metal描画、Play検査を実行しました。最新は **`Assets/Sorajiro/Generated/Twilight_v006/L_LookDev.unity`**。旧M1/M2、Twilight v001〜v005、元画像は保持しています。画質は未承認で、「目指せピクサー」の到達報告ではありません。

| 確認対象 | 今回の状態 |
|---|---|
| 参照 | 正面は `bear_master_20261012.jpg`、裏側などは `bear_turnaround_20261012.png`。部屋は夕暮れの2画像。原本同一コピー・SHA・権利未確認をmanifestに記録 |
| くま | 編集可能な全身メッシュ・UV・部位材質・9骨Genericリグ。平たい短い足、白い足裏、背面の雲・しっぽ・個別の縫い目。生成・取り込み・実描画まで確認した制作試作 |
| 毛 | 青・白を分けた全身の短い不透明リボンメッシュ。基布と同じリグにスキン。約15.2万本は生成量で、品質や性能の合格値ではない |
| 動き | Unityで編集できるIdle/Walk/Run/Jump/Fall/Landの6クリップ、速度BlendTree、実CharacterControllerに連動するAnimator。root motionは無効 |
| 寝具 | Blenderの実Clothで110フレーム、重力・マットレス衝突・頭側の固定から垂れを生成。12mmの厚み、膨らみ、縁、縫い目、つぶれた角丸の枕を追加。**ゲーム内では静止メッシュ** |
| 室内 | 立体のベッド、本と棚、窓、ひだ付きカーテン、ループ毛ラグ、植物、ランプ、星。写真の背景貼り付けではない。星は美術小道具で収集機能は未実装 |
| 光 | HDRP標準Lit、固定EV6.5、ACES/TAA、夕暮れの窓と暖色ランプ。補助ライトはプレビュー用。GIベイクや映画品質の照明は未検証 |

## 実画像と動画

最新固定カメラは `Artifacts/Visual/twilight/20261013T023032275/`。1920×1080の実HDRP camera StandardRequestで、各ショット24回のウォームアップ要求後に保存しました。ゲーム画面の解像度自動変更、DLSS、FSR2は無効。カメラ位置・画角・機器・AA・露出は画像と同名のJSONに保存しています。これはEditorの描画確認で、オフラインレンダーでも性能測定でもありません。

- [寝具の実画像](../Artifacts/Visual/twilight/20261013T023032275/S05_Bedding_pile_ON.png)
- [くま全身](../Artifacts/Visual/twilight/20261013T023032275/S03_ThreeQuarter_pile_ON.png)、[顔](../Artifacts/Visual/twilight/20261013T023032275/S01_Portrait_pile_ON.png)、[背面](../Artifacts/Visual/twilight/20261013T023032275/S06_Back_pile_ON.png)、[足元](../Artifacts/Visual/twilight/20261013T023032275/S04_Contact_pile_ON.png)
- [室内](../Artifacts/Visual/twilight/20261013T023032275/S02_Gameplay_pile_ON.png)。顔と室内は毛OFFも保存。同一姿勢・照明で比較可能
- [30秒の実Play動画](../Artifacts/Visual/motion/20261013T023345397/twilight-motion.mp4)。419枚の1920×1080 JPEGと `frames.csv` の実時刻からH.264/VFRに変換。待機、歩行、走行、2回のジャンプ、旋回、接近。スクリプト入力で本番motorを動かしており、キーボード操作の録画ではない

固定画像のON全6構図と顔OFF、動画の歩行・ジャンプ・走行・旋回のサンプルフレームを開いて確認しました。動画全フレームのちらつき検査や足滑りの合格判定は未了です。15fpsは記録の目標サンプリング頻度であり、ゲームのFPSではありません。記録負荷を含む動画から30fps達成とは判断しません。古い暗い版・色調の悪い版も比較資料として保存しています。

## 実行と結果

環境はUnity **6000.6.5f1**（Editor表示6.6、revision `3ff58d469c8a`）、HDRP **17.6.0**、Input System **1.20.0**、Blender **5.2.0 LTS**、macOS **27.0.1**、Apple **M3 Max/30 GPU cores/36GB**、Metal。Unityプロジェクトは `$HOME/Documents/Unity/sorajiro-unity`。

リポジトリのルートで実行した最終ソースのコマンド：

```bash
python3 Tools/Blender/bear_twilight_v5.py
python3 Tools/Blender/room_twilight_v4.py
'/Applications/Blender 5.2.app/Contents/MacOS/Blender' --background --factory-startup --python Tools/Blender/bear_twilight_v5.py -- --write
'/Applications/Blender 5.2.app/Contents/MacOS/Blender' --background --factory-startup --python Tools/Blender/room_twilight_v4.py -- --write
python3 scripts/check_unity_source.py --editor /Applications/Unity/Hub/Editor/6000.6.5f1/Unity.app --project "$HOME/Documents/Unity/sorajiro-unity"
python3 -m unittest discover -s Tests -p 'test_*.py' -v
python3 scripts/encode_unity_motion.py Artifacts/Visual/motion/20261013T023345397
python3 scripts/collect_unity_twilight_evidence.py --project "$HOME/Documents/Unity/sorajiro-unity" --capture Artifacts/Visual/twilight/20261013T023032275 --motion Artifacts/Visual/motion/20261013T023345397
python3 scripts/validate_handoff.py
```

Blenderの生成は成功。マットレス上面0.525mに対し、焼き込んだ布の最下部は約0.280mでした。毛を含むimport済みbodyは113,540頂点/207,482三角形、毛は945,228頂点/609,224三角形、各9骨。部屋は約142.5万三角形の試作で、軽量化やLODは未実装です。Blenderのmaterial API非推奨警告と単一メッシュgroupのjoin警告はありましたが、実FBXの出力とUnity importは成功しました。

| 検査 | 結果・証跡 |
|---|---|
| C#ソース | 実Unity参照によるRoslyn Runtime/Editorコンパイルと本番MotionPolicyの9assertions PASS。`validation/unity_source_checks.json`。Player buildではない |
| Python・生成保護 | 21 tests PASS。実Unity JsonUtilityの6保護検査もPASS。`Artifacts/UnityEditor/guard-tests.txt` |
| スキン | 実FBXのUV・材質・9骨、頭/腕/脚を回したbodyと全身毛の変形PASS。`Artifacts/UnityEditor/twilight-skin-test.txt` |
| クリップ | 6個のnative `.anim`、各42曲線のbindingと実メッシュ変形PASS。`Artifacts/UnityEditor/twilight-animation-clips.txt` |
| 実行中Animator | 本番motor/driverと実Animator.UpdateでLocomotion/Jump/Fall/Land/接地復帰PASS。`Artifacts/UnityEditor/twilight-animation-runtime.txt` |
| 衝突 | 実CharacterController/本番Stepで接地、移動、走行、斜め、30/60 step、ジャンプ/着地、空中再ジャンプ拒否、外周、復帰PASS。`Artifacts/UnityEditor/twilight-motor-smoke.txt`。同期スモークであり長時間プレイではない |
| 描画・動作 | 1080p固定8枚、実Play約30.045秒/419枚の取得成功。ffprobeでMP4の1920×1080/H.264/419 frames/30秒を確認 |
| 証跡整合 | Blender/Unity receipt、実画像ヘッダー、アセンブリ更新時刻、実行ログ、動画時刻と空中/走行/4状態を検査。`validation/unity_twilight_checks.json`。画質自動承認ではない |
| 引き継ぎ検査 | `validate_handoff.py`。文書・参照整合のみ、ゲームテストではない |

途中でEditor assemblyのMonoBehaviourをPlayへ追加できない問題を修正し、記録用コンポーネントをRuntimeに移しました。これは検証メニューからPlay中だけ一時作成し、終了時に削除してプレイヤー/カメラを戻します。保存シーンには置きません。生成直前のコンパイルでCreate操作が実行されなかった際は完了扱いせず、コンパイル後に再実行して実receiptとシーンを確認しました。

## 次にMacで開く・編集する

1. Unityで `Sorajiro > Twilight > 3. Open twilight lookdev`。現在は最新シーンを開き、Gameビューを表示し、Play停止にしてあります。▶→Gameビューをクリック。WASD/矢印、Shiftで走る、Spaceでジャンプ、右ドラッグ旋回、ホイール接近、Esc停止、R復帰。
2. 固定画像はPlay停止で `Sorajiro > Twilight > 4. Capture fixed shots and full-pile comparison (Edit, 1080p)`。布団は `S05_Bedding`、背面は `S06_Back`。保存先は新しいUTC名のフォルダーです。
3. モデル元データは `Artifacts/SourceArt/Bear/twilight_v005/Sorajiro_Bear_M2.blend`、部屋は `Artifacts/SourceArt/Room/twilight_v004/TwilightRoom.blend`。部位とrigを編集可能な状態で保存。単位m/Z-up/-Y正面→FBX Forward=-Z/Up=Y→Unity m/Y-up/+Zを実確認しています。元ファイル名のM2は制作履歴で、旧M2の内容ではありません。
4. Unityの `Assets/Sorajiro/Generated/Twilight_v006/` に `BearMotion.controller` と6個の `.anim` があります。**手修正は別の手編集用フォルダーへ複製して行います**。Blenderにはrest rigがあり、UnityのクリップがBlender Actionとして入っているわけではありません。耳骨はありますが耳の遅れ、まばたき、休憩は未実装です。
5. 別Macでは上記Blender/Unityのパスを実際の配置に置き換え、ローカルpackage接続を[Unity手順](UNITY_START.md)で確認。最終Blenderレシピを実行し、Unityの `Twilight > 2. Create master bear and twilight room`→`3. Open`。生成済みのハッシュが一致すれば再生成しません。編集/部分生成を検出したら保存し、新しいrevisionへ出します。既存フォルダーを消して回避しません。
6. 次の配布検査は `File > Build Profiles` で専用macOSプロファイルを作り、最新シーンをScene Listに追加、Apple Silicon向けに `Artifacts/UnityBuilds/macOS/` へBuild。既存プロファイルは保持。Editor外の起動・入力・描画を別途検査します。**今回は未実行**です。

大きい `.blend`/FBX/動画/Unity生成物はローカルとignored `Artifacts/` にあります。ソースだけGitへ移してもこれらは運ばれません。LFS/storageの合意と実オブジェクト確認前に、別PCで資産が取得済みとは報告しません。

## 残る品質差と次の小タスク

最も大きい差は、マスターの密な柔らかい毛束と、現在の均質な細い輪郭毛・滑らかな白アップリケです。雲の縁に折れ/筋、頬の模様の形と位置の差が残り、縫い目と白い点にも投影の歪みがあります。足は平たくしましたが、足滑り・接地タイミング、着地の重さは承認できる段階ではありません。

寝具は以前の波形より重力に沿う形になりましたが、キルティングの糸に浮きと角張りがあり、近景の織り目・圧縮・折り返しの説得力が不足します。照明にも鋭い影、木材の均一さ、植物/本の簡略化が残っています。

次は**中立光でくまの雲のトポロジーと毛束の触感を磨き、同じ顔/全身/足元で比較する**小タスクです。寝具は糸を表面へ再投影し、縁と折り目の接写を再比較します。新しい遊びを増やす前に、この画質差を詰めます。

未実施はPlayer build、Windows、120秒×3回のCPU/GPU/p95とメモリ測定、LOD/画質切替、全動画のちらつき/足滑り判定、ゲーム中の寝具変形、所有者の画質承認。UEは今回使用せず、UEビルド・描画・性能の検証結果はありません。M2/M3の美術・動作試作が進んだ状態で、マイルストーン全完了とはしません。
