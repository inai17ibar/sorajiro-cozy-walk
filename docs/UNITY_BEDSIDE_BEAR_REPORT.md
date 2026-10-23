# 選択したBearFit v001・比較3をベッドサイドへ

2026-10-24。所有者が形と色味を選択した **BearFit_v001の比較3** を、保存版のBedside v009へ組み込んだ。新しいシーンは **`Assets/Sorajiro/Generated/BedsideBear_v002/L_LookDev.unity`**。BearFit v002を採用したという意味ではない。元の比較シーン・モデル・素材・部屋は変更していない。

## 開く

既存Macプロジェクトは `$HOME/Documents/Unity/sorajiro-unity`。Unityの **Sorajiro → Bedside Bear → 2. Open selected bear in bedside room** から開く。Play後にGameビューをクリックし、WASD、Shift、Space、右ドラッグ、ホイールで操作する。この部屋では比較用の数字1/2/3切り替えは使わない。作業後はこの新シーンをPlay停止状態で残した。

## 実画面と照明の影響

- [正面](../Artifacts/Visual/bedside-bear/20261023T225809652/S01_Portrait_Preferred.png)、[全身・斜め](../Artifacts/Visual/bedside-bear/20261023T225809652/S03_ThreeQuarter_Preferred.png)、[ゲーム画角](../Artifacts/Visual/bedside-bear/20261023T225809652/S02_Gameplay_Preferred.png)、[寝具](../Artifacts/Visual/bedside-bear/20261023T225809652/S05_Bedding_Preferred.png)。
- 同じ素材・形・カメラ・固定露出での[くま用の光を消した状態](../Artifacts/Visual/bedside-bear/20261023T225809652/S01_Portrait_Hero_OFF.png)、[部屋の直接光を受ける状態](../Artifacts/Visual/bedside-bear/20261023T225809652/S01_Portrait_Room_Direct.png)。光の影響を確認する比較で、画質スコアではない。
- [30秒の実Play動画](../Artifacts/Visual/motion/20261023T230236873/twilight-motion.mp4)、[集約した検証記録](../validation/unity_bedside_bear_v002_checks.json)。

照明を消すと青布は濃い紺色に見え、正面の微細な毛と目鼻の反射も読みにくくなる。したがって選ばれた色味には素材色だけでなく、光の方向・色・強さと露出も影響している。今回、素材の色は変更していない。スタジオとは空・間接光・背景が異なるため、全ピクセルの色が同じになるとは扱わない。

選択シーンの現在モデルと現在ライトを直接複製した。FBXとAnimator Controllerは `BearFit_v001/Bear.fbx`、`BearFit_v001/BearMotion.controller`、青白のCotton/Wool素材と毛素材はFleece v005を参照する。腹部の丸み・広い脚間隔・顔・雲柄を保持し、元のリグと6クリップで動かす。新しいDCC生成や素材の塗り直しは行っていない。

選択版の370lux方向光、65lmの補光、55lmの縁の面光源、固定露出EV7を引き継いだ。室内ではそれだけだと暗くなったため、別版v002で240lmの中立色の面光源を追加した。くまはLight Layer 2、部屋は1で直接光を分け、部屋のシャドウは両方の物体を対象にする。既存HDRPアセットのLight Layersはすでに有効で、パイプライン設定は変更していない。旧部屋の主役補光は新シーン内だけ無効にした。部屋の空・ACES・間接光設定を保持し、プロファイルは新シーン専用に複製した。EVは部屋の従来6から7になるため、従来の部屋との比較は光の方向だけの比較ではない。

主役の面光源は開始地点付近に固定しており、プレイヤーへ追従するライトではない。移動・向きによって明るさは変わる。部屋全域で参照と同じ色を保証していない。

## 変更と保護

`Unity/Packages/com.sorajiro.cozywalk/Editor/BedsideBearBuilder.cs` が保存版の部屋と選択したくまを別出力へ組み込み、専用LightingSettings・APV・光源・Volumeを生成する。旧部屋のAPVやライトマップを新Scene GUIDへそのまま使わず、実GPUベイクで3枚のライトマップと新APVを作った。`BedsideBearActions.cs` は非同期ジョブ、`BedsideBearCapture.cs` は固定撮影を担当する。

`MotorSmokeTests.cs`、`BearAnimationVerification.cs`、`TwilightMotionCapture.cs` に新シーンの検査先を追加。`MotionCaptureProbe.cs` はCSVに実際の `visual_model` 名を追加し、全433サンプルで `Preferred_BearFit_v001_3` が使われたことを検査する。`scripts/unity_art_review.py` に `bedbear-*` の操作、`scripts/collect_bedside_bear_evidence.py` に証拠の照合を追加した。

BearFit v001、Fleece v005、Bedside v009と新出力は全ファイルのreceipt照合に成功。生成・ベイク準備にもハッシュ検査を設け、所有者編集や途中出力を黙って上書きしない。暗かった途中版 `BedsideBear_v001` と撮影 `Artifacts/Visual/bedside-bear/20261023T224352287` も保存した。今回の証拠はv002のものを使う。

## 実行した確認と未確認範囲

| 確認 | 実行結果 |
|---|---|
| ソース | 実Unity DLLを参照したRuntime/Editorコンパイル成功、共用MotionPolicy 9 / PlushGait 2,012 assertions成功。Playerビルドとは別 |
| Python | unittest 31件成功。handoff検査はゲーム試験と区別する |
| ベイク | Apple M3 MaxでProgressive GPUベイク完了、16.70秒、ライトマップ3枚・APV保存済み。再読み込みとPlay停止後もreceipt・参照を検査、APV loaded=True |
| 実アセット | v001のFBX・Controller、Fleece v005等の元素材参照、Light Layer、新Scene GUIDのAPV登録を検査 |
| 実描画 | M3 Max / Metal / HDRP、1920×1080、TAA暖機40回、動的解像度なし、固定6枚。PNGに実カメラ・GPU・露出・参照パスのJSONを併記 |
| 実Play | 本番CharacterController/Stepで接地・移動・走行・ジャンプ・着地・外周・復帰を検査。Generic AnimatorのLocomotion/Jump/Fall/Landを確認 |
| 動画 | 実行30.01771秒、433枚、最終取得29.95333秒。CSVの実時刻でMP4化。歩行・ジャンプ・走行・旋回区間の4画像を目視観察。録画頻度をゲームFPSと呼ばない |

入力は自動ルートと明示したfocus fixture。実キーボードの受け入れ、連続動画全体のちらつき・動作美術、Player build、Windows、FPS・GPU時間・メモリ圧力、LOD/負荷切り替えは未確認。今回のアセット状態は **生成済み・実インポート済み・Metal描画観察済み・選択モデルを室内へ組み込み済みの試作**。所有者が選択したのは元の形と色味で、今回の室内画質全体の承認とは区別する。

残る大きな視覚課題は足元の接地感と股下の変形、部屋の強い発光と主役の陰影の釣り合い。次の小タスクはこの部屋で短い歩行・着地を使い、脚の付け根のウェイトと接触影を調整すること。マスターの密な繊維と柔らかい雲の縁にも差が残る。

## Macで再現する

Unity 6000.6.5f1 / HDRP 17.6.0 / Input System 1.20.0。追加ソフトや有料サービスは不要。新規Macには先に[選択くまの入力](UNITY_BEAR_FIT_REPORT.md)と[保存版の部屋](UNITY_BEDDING_REPORT.md)の実生成資産が必要。以下はリポジトリ直下から実行する。

```bash
python3 scripts/unity_art_review.py stop
python3 scripts/unity_art_review.py reload-scripts
# 未生成時だけ create → bake。既存の完成出力に再ベイクしない
python3 scripts/unity_art_review.py bedbear-create
python3 scripts/unity_art_review.py bedbear-bake
# 同じジョブの state=completed と3枚/APVを待つ
python3 scripts/unity_art_review.py bedbear-open
python3 scripts/unity_art_review.py bedbear-check
python3 scripts/unity_art_review.py bedbear-capture
# Artifacts/Visual/bedside-bear/<UTC>/complete.txt と6枚を待つ
python3 scripts/unity_art_review.py bedbear-play
python3 scripts/unity_art_review.py bedbear-runtime
python3 scripts/unity_art_review.py motion
# Artifacts/Visual/motion/<UTC>/complete.txt を待つ
python3 scripts/encode_unity_motion.py Artifacts/Visual/motion/<UTC>
python3 scripts/unity_art_review.py stop
python3 scripts/unity_art_review.py bedbear-check
```

既存出力の再利用はreceipt一致時だけ。初回生成後のベイクは準備記録と一致する場合だけ行う。ソースや出力が違う場合は削除せず新リビジョンにする。Pipelineが5秒の応答期限を超えても処理が続く場合があるため、同じジョブの完了を観察し、生成を重複させない。別プロジェクトには各Unityコマンドの `--project /絶対パス` を指定する。

今回実行した集約コマンドとコード検査：

```bash
python3 scripts/collect_bedside_bear_evidence.py --project "$HOME/Documents/Unity/sorajiro-unity" --capture Artifacts/Visual/bedside-bear/20261023T225809652 --motion Artifacts/Visual/motion/20261023T230236873 --bake-job Artifacts/UnityEditor/PipelineJobs/20261023T225445065_bedbear-bake.json
python3 scripts/check_unity_source.py --editor /Applications/Unity/Hub/Editor/6000.6.5f1/Unity.app --project "$HOME/Documents/Unity/sorajiro-unity"
python3 -m unittest discover -s Tests -p 'test_*.py' -q
python3 scripts/validate_handoff.py
git diff --check
```

検査ログは `Artifacts/UnityEditor/bedside-bear-*.txt`、非同期ジョブは `Artifacts/UnityEditor/PipelineJobs/*_bedbear-*.json`。実UnityアセットはMacのプロジェクト内にあり、ソースだけのGit checkoutに生成済みバイナリが同梱されるとは扱わない。
