# かわいい足音・ジャンプ音と少し俯瞰する操作カメラ

2026-10-24。足音を短く丸い「ぽふ」、ジャンプを上がる「ぴょん」、着地を小さく弾む音へ変更した。標準カメラを26度の俯瞰・距離1.25mにして、Q/Eの45度旋回、Cでくまの背後へ復帰、画面ボタンを追加した。実機の生成・取り込み・Play・Metal描画まで検査済みの試作。聴感と操作感の所有者承認、Player build、性能は未完了。

## 開き方

Macの `$HOME/Documents/Unity/sorajiro-unity` をUnity 6000.6.5f1で開き、**Sorajiro → Play Comfort → 2. Open overhead camera and cute sounds** を選ぶ。▶でPlayし、Gameビューをクリックする。今回の入口は旧Fleece Lighting / Star Playとは別で、必要な4シーンをまとめて開く。

| 操作 | 内容 |
|---|---|
| WASD / 矢印 | 画面の向きを基準に移動 |
| Shift / Space | 走る / ジャンプ |
| Q / E | カメラを左右へ45度ずつ回す |
| C | くまの向いている方向の背後へ戻し、標準の俯瞰と距離へ戻す |
| 画面左下の3ボタン | 左へ / 後ろへ戻す / 右へ。キーと同じ機能 |
| 右ボタンを押してドラッグ | 自由な向きと上下角度 |
| ホイール / 横スクロール | ズーム / 左右旋回 |
| M / 右上の音量 | 効果音の消音 / 音量。ジャンプ音にも適用 |
| Esc / R | 操作停止・再開 / 開始位置と標準カメラへ復帰。取得した星は維持 |

UnityのGameビューが拡大表示になってUIが切れる場合は、Gameビュー内のScaleを下げて全体を表示する。追加ソフト・購入・アカウントは不要。

## 実画面・映像・音

- [標準の俯瞰](../Artifacts/Visual/comfort-play/20261024T025557583/Overhead_Default.png)、[右へ45度](../Artifacts/Visual/comfort-play/20261024T025557583/Turn_Right.png)、[くまの後ろへ](../Artifacts/Visual/comfort-play/20261024T025557583/Behind_Bear.png)。Apple M3 Max / Metal / HDRP、1920×1080の実Play画像。各PNGにカメラ実座標・角度・モデル・APIのJSONを併記した。
- [低い角度の比較](../Artifacts/Visual/comfort-play/20261024T025557583/Before_Low_Camera.png)は同じ現在の照明でpitch3度・距離1.2mを再構成した比較。参照切れがあった旧固定カメラの完全な再現画像ではない。
- [30秒の実Play動画](../Artifacts/Visual/motion/20261024T045726452/twilight-motion.mp4)。歩行、ジャンプ2回、走行、70度旋回、ズーム。429枚・最終標本29.96744秒。CSVに実カメラ座標・角度とジャンプ発音数を保存した。可変時刻でエンコードし、音声は含まない。15fpsは目標標本間隔でありゲームFPSではない。
- [足音・着地の実DSP出力](../Artifacts/Visual/comfort-play/20261024T025557583/Cute_Foot_And_Land_DSP.wav)、[ジャンプの実DSP出力](../Artifacts/Visual/comfort-play/20261024T025557583/Cute_Jump_DSP.wav)。マイク録音ではなく実AudioSource側の出力。非無音・有限値・ピークを検査した。スピーカー／ヘッドホンの聴感や音圧バランスの確認は残る。

集約：[validation/unity_comfort_play_v002_checks.json](../validation/unity_comfort_play_v002_checks.json)。画像生成による検証の代替は使用していない。RenderRequestの画像・動画にはIMGUIの操作ボタンは含まれない。ボタン表示とQ/E/Cは実Gameビューでも確認した。

## 実装・変更ファイル

`Runtime/OrbitCamera.cs` と `CameraComfortPolicy.cs` に、フレーム時間に基づく回転と追従の補間を追加した。上下の追従をやや緩めてジャンプ時の揺れを抑える。壁判定は本物のSphereCastを使い、コライダーの手前へカメラを寄せる。新しい操作は `comfortControls` 有効時に使用し、旧シーンの保存値は書き換えない。

**保存済みBedsideBear v002の参照切れも修正した。** ルートごとの複製によって `OrbitCamera.target` と `SorajiroMotor.view` が実プレイヤー／カメラに接続されていなかった。旧版ではカメラが保存時の位置に留まり、移動はワールド座標へフォールバックしていた。`Runtime/ComfortPlay.cs` がPlay中だけ正しい相互参照を設定する。今回の検査は参照だけでなく、実際のカメラ高・旋回・画面方向の移動も確認する。旧検査のカメラ操作合格として今回の結果を流用しない。

`Editor/CutePlushSfx.cs` はオリジナルの合成音を作る。歩行3種は0.115秒の丸い下降音、ジャンプは0.22秒の上昇音、着地は0.24秒の小さな二段バウンド。48kHz / mono / PCM16bit WAVを実Unityへ取り込む。外部録音、有料素材、AI音声サービスは使用していない。

`ComfortPlay.cs` は既存の星取得システムへ新しい足音・着地音を接続し、実接地状態から離地したときだけジャンプ音を鳴らす。空中でSpaceを繰り返しても重複発音せず、Rでも鳴らない。既存の音量・ミュート・停止・フォーカス制御を引き継ぐ。星のチャイムは維持する。

`Editor/ComfortPlayBuilder.cs` が新しい追加シーンと5音を生成する。今回の出力は `Assets/Sorajiro/Generated/ComfortPlay_v002/L_LookDev.unity`。FleeceLighting v002 / StarPlay v003 / BedsideBear v002を入力とし、選択されたBearFit v001・比較3、フリース、Controller、部屋、ライトマップとAPVを上書きしない。入力と出力のreceiptは全一致を確認した。状態は **生成済み・Unity取り込み済み・実Metal描画／DSP／操作検査済み、所有者承認待ち**。

検査関連は `Editor/ComfortPlayVerification.cs`、`Runtime/MotionCaptureProbe.cs`、`Tests/Unity/CameraComfortTest.cs`、`CutePlushSfxTest.cs`、`scripts/check_unity_source.py`、`unity_art_review.py`、`collect_comfort_play_evidence.py`。文書入口と `project.environment.json` も更新した。

## 検証結果と限界

- 実UnityライブラリでRuntime/Editorのコンパイル成功。CameraComfort 19、CutePlushSfx 82,829 assertions成功。既存MotionPolicy 9 / PlushGait 2,012 / FootstepBeat 17 / PlushSfx 167,077も成功。
- 実Play28項目成功。26度／1.25m、参照接続、くま全身の画面内位置、45度旋回・復帰、フォーカス停止、R、実BoxColliderによる壁衝突と回復、実カメラ方向への移動、歩行音、離地音1回、空中の重複拒否、着地音、ミュート、実星トリガーと取得維持、実DSPを検査した。検査ルートは足音5回・ジャンプ1回・着地1回・星1個。DSPピークは足音／着地0.05356、ジャンプ0.04471。
- 通常のUpdate入力を一時停止して本番Step／Followを呼ぶfixtureで上記を検査した。別途、実GameビューへQ/E/Cを入力し、実yawをE=29度、Q=344度、C=180度と記録した。横スクロールや右ドラッグの物理デバイス受け入れは未了。
- 新しいPlayの30秒ルートで実追従・26度俯瞰・ジャンプ発音2回を記録。歩行8秒、ジャンプ16.2秒、走行21秒、旋回27秒の実フレームも目視確認した。すべての時間のちらつき合格、全家具での遮蔽解決、性能合格は意味しない。
- Python unittest 31件、handoff文書検査、差分空白検査を実行。handoffはゲーム試験ではない。

試行のv001は実カメラ高の検査で失敗し、上記参照切れを発見した。v002生成直後は同時に開いた旧追加シーンを検査関数が選んで失敗したが、`comfort-open` で開き直した後に出力検査・Playが成功した。初回のジャンプDSP検査は待機中の無音で検査バッファが満杯になって失敗したため、離地直前にバッファをリセットする検査へ修正し、新しいPlayで28項目が成功した。失敗記録と旧出力は保持し、最終証拠として使っていない。

**未実施：Player build、Windows、FPS／GPU時間／メモリ圧力、聴感評価、長時間の操作受け入れ。** Build Profileは変更していない。今回の実装を保存・休憩・フォトモードなどの全ゲーム機能の完了とは扱わない。

残る大きな見た目の課題は、股下と足元の変形・接地、および移動先による毛の陰影の差。次の小タスクは、この俯瞰と歩行速度で足裏の接地と着地の重さを磨くこと。毛の品質・今回の音・操作感は実プレイを見て所有者が判定する。

## Macで再検査する

既存プロジェクトと生成アセットがあるMacでは、リポジトリルートから以下を実行する。別プロジェクトはUnityコマンドに `--project /絶対パス` を指定する。

```bash
python3 scripts/unity_art_review.py stop
python3 scripts/unity_art_review.py reload-scripts
python3 scripts/unity_art_review.py comfort-create
python3 scripts/unity_art_review.py comfort-open
python3 scripts/unity_art_review.py comfort-check
python3 scripts/unity_art_review.py comfort-play
python3 scripts/unity_art_review.py play-feel-status
# ready=True・取得0の新しいPlayで実行する
python3 scripts/unity_art_review.py comfort-runtime
# 28 assertions / completed と Artifacts/Visual/comfort-play/<UTC>/complete.txt を確認
python3 scripts/unity_art_review.py stop
python3 scripts/unity_art_review.py comfort-play
python3 scripts/unity_art_review.py motion
# Artifacts/Visual/motion/<UTC>/complete.txt ができてからエンコード
python3 scripts/encode_unity_motion.py Artifacts/Visual/motion/<UTC>
python3 scripts/unity_art_review.py stop
python3 scripts/unity_art_review.py comfort-check
python3 scripts/collect_comfort_play_evidence.py --project "$HOME/Documents/Unity/sorajiro-unity" --capture Artifacts/Visual/comfort-play/20261024T025557583 --motion Artifacts/Visual/motion/20261024T045726452
python3 scripts/check_unity_source.py --editor /Applications/Unity/Hub/Editor/6000.6.5f1/Unity.app --project "$HOME/Documents/Unity/sorajiro-unity"
python3 -m unittest discover -s Tests -p 'test_*.py' -q
python3 scripts/validate_handoff.py
git diff --check
```

receipt保護により同一レシピ・完全一致した生成物だけを再利用する。既存編集や途中出力は上書きしない。音やカメラの生成レシピを変える場合は新リビジョンを作る。収集スクリプトの実キー記録は今回の `Artifacts/UnityEditor/comfort-key-{q,e,c}.json` を使うため、別Macで再検査する際は実キー確認も別途記録する。

macOS用Build Profileを用意する場合のScene Listは次の4つ。先頭を起動シーンとし、残りは追加ロード用に含める。今回Player buildは実行していない。

1. `Assets/Sorajiro/Generated/ComfortPlay_v002/L_LookDev.unity`
2. `Assets/Sorajiro/Generated/FleeceLighting_v002/L_LookDev.unity`
3. `Assets/Sorajiro/Generated/StarPlay_v003/L_LookDev.unity`
4. `Assets/Sorajiro/Generated/BedsideBear_v002/L_LookDev.unity`

最後はPlay停止の編集状態で、4シーンを開いたままにしている。次回は▶から試せる。
