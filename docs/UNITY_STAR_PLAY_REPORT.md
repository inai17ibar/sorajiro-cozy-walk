# ベッドサイドの星取得と効果音

2026-10-24。床付近の星3つに接触すると、金色の粒が広がって星が消え、チャイムが鳴る小さな遊びを追加した。歩行・走行の足音、着地音、取得数、日本語の完了表示、音量とミュートを含む。8個の探索レイアウト、セーブ・ロード全体、完成ゲームの達成とは区別する。

## 開き方と操作

Macの既存プロジェクトは `$HOME/Documents/Unity/sorajiro-unity`。Unityの **Sorajiro → Star Play → 2. Open playable bedside stars** を選ぶ。次の2シーンを一緒に開くメニューなので、従来のBedside Bearだけを開いた場合と区別する。

- 見た目・部屋：`Assets/Sorajiro/Generated/BedsideBear_v002/L_LookDev.unity`
- 今回の追加部分：`Assets/Sorajiro/Generated/StarPlay_v003/L_LookDev.unity`

▶でPlayしてGameビューをクリックする。WASD / Shift / Space、右ドラッグ、ホイールは従来どおり。床付近の3つの星にふれると取得できる。壁際や高い場所の飾りの星は収集対象にしていない。画面右上に取得数、音のオン／オフ、効果音量を表示する。**Mでミュート、Escで一時停止、Rで開始点へ復帰**。Rでは取得数を失わない。Playを停止して再度開始すると、星は3つとも戻る。

音は所有者の明示的な追加依頼を受けて初期オン、効果音量65%。設定はMac内のPlayerPrefsだけに保存する。UnityのGameビュー側でMute Audioになっている場合やOS側が消音の場合は、ゲームの設定とは別に確認する。追加ソフト・購入・外部音源のダウンロードは不要。

## 実画面と音の証拠

[取得前](../Artifacts/Visual/star-play/20261024T005633251/Before.png)、[接触して光の粒が出た瞬間](../Artifacts/Visual/star-play/20261024T005633251/Contact_Burst.png)、[3つとも取得した後](../Artifacts/Visual/star-play/20261024T005633251/After.png)。実Unity Play / Apple M3 Max / Metal / HDRPの1920×1080。イベント地点を写す専用の固定カメラで、各PNGにカメラ・取得数・実機情報のJSONを併記した。生成画像やコンセプト画ではない。くまの位置・向きはプレイに伴って変わる。StandardRequestの画像にはIMGUIのカウンターは含まない。

[実AudioSourceの足音DSP出力](../Artifacts/Visual/star-play/20261024T005633251/Actual_Foot_DSP.wav)、[実AudioSourceの取得音DSP出力](../Artifacts/Visual/star-play/20261024T005633251/Actual_Chime_DSP.wav)。これらはゲームが再生した出力側の音声サンプルで、マイク録音ではない。出力の有限値・非無音・ピークを検査した。スピーカー／ヘッドホンからの聴感や音量バランスを確認済みとは報告しない。

集約記録：[validation/unity_star_play_v003_checks.json](../validation/unity_star_play_v003_checks.json)。

## 実装とアセット状態

`Editor/StarPlayBuilder.cs` は、保護されたBedsideBear v002を読み、ゲーム性だけの追加シーンを生成する。元の星は材質別に1メッシュへ結合されていたため、頂点の幾何接続で個別の星へ分離した。元の全三角形を保持したまま、位置・形・素材を引き継ぐ。Play時だけ元の `Room_StarGlow` を非表示にし、追加シーンの分離版へ切り替える。ベッドサイド本体、くまの選択モデル、素材、Controller、Scene GUID、ライトマップとAPVは保存し直さない。

`Runtime/StarPickup.cs` はkinematic Rigidbody＋Sphere Triggerと実CharacterControllerの接触を受け、くまだけを対象に一度だけ取得する。星のRendererとTriggerを無効化し、近くにある星用の点光源も消す。元の静的ベイクに含まれる間接光や部屋の暖かな照明までは、取得ごとに焼き直さない。

`StarPlaySession.cs` はセッション内の取得ID、UI、音量設定、発音を管理する。`StarBurst.cs` は24個の小さな金色のメッシュ粒を広げ、0.85秒で片付ける。フォーカスを失った時や一時停止中には取得・粒の進行・発音を止める。足音は実AnimatorのLocomotionの半周期と、実移動速度・接地状態を使う。停止中・空中には鳴らさず、再開時に音がまとめて鳴ることも避ける。着地は空中から接地への変化で別の柔らかい音を鳴らす。

元の部屋にはAudioListenerがなかったため、Play時にメインカメラへ1つ追加する。またHDRPの方向光シャドウが2灯で競合していたため、この追加シーンのPlay中だけ、くまの選択主光源を残してもう一方の方向光のシャドウを無効化する。元の照明アセットは変更しない。

`Editor/PlushSfx.cs` が独自の合成音を生成する。48kHz / mono / 16bit PCM WAVを実Unityへ取り込み、歩行の小さなバリエーション3つ、着地1つ、4音のチャイム1つを用意する。足音は柔らかい低音とフィルターを通したノイズ、チャイムは減衰する倍音による試作音で、実際の布や木床を録音したフォーリーではない。第三者録音・有料素材・音声AIサービスは使用していない。

状態は **生成済み・実Unity取り込み済み・実トリガー取得／発音DSP／Metal画像を検査済みの試作**。所有者による今回の遊び・エフェクト・聴感の承認は未了。v001/v002の途中出力と失敗した検査記録も残しており、最終証拠へ流用していない。

## 実行した検査

- 実Unity DLLでRuntime/Editorコンパイル成功。共用MotionPolicy 9 / PlushGait 2,012 assertions、足音の周期・停止・空中・再開・位相巻き戻しを扱うFootstepBeat 17 assertions、実音源生成関数の非無音・有限値・ピーク・端点・バリエーション167,077 assertions成功。
- Python unittest 31件成功。handoffは文書・参照整合性の検査で、ゲーム試験とは別。
- 最終版の実Playで28項目成功。非くま・重複拒否、実Controllerの接触、消去とTrigger無効化、補助光の消灯、粒の発生と寿命、Rでの取得維持、3つとも歩いて到達、全取得と3回のチャイム、足音・走行・空中無音・着地・フォーカス停止・ミュート・設定保存を検査。
- 最終ルートでは歩行区間4回、合計足音12回、着地音2回、取得音3回。DSPピークは足音約0.0533、取得音約0.0688。実波形をWAV保存した。これはゲームFPS、機器の出力音圧、聴感上の完成度ではない。
- Play停止→再開後、取得0 / 星3つ / 発音数0に戻ることを実状態で確認。記録は `Artifacts/UnityEditor/star-play-restart.json`。
- 新追加シーンとBearFit v001 / Fleece v005 / Bedside v009 / BedsideBear v002の全receiptを照合。既存のライトマップ3枚を維持。元のくま・部屋を上書きしていない。

自動検査は通常のキーボードUpdateを一時的に止め、本番の `Step` と本物のAnimator・物理・音をフレーム間で動かす明示的なfixture。終了・例外時には元の入力とフォーカス・音量へ戻す。実キーボード・マウスでの操作受け入れ、全時間の動作・毛のちらつきの合格とは別。

Playerビルド、Windows、FPS/GPU時間/メモリ圧力、スピーカー／ヘッドホンの聴感、8個を使った探索の導線、アプリを終了しても星の取得状態が残るセーブは未実施／未実装。今回は小さな3個の収集と音のタスク。次は足元の接地感・股下の変形を保留課題として残し、取得の見やすさと音量を実プレイで調整する。

## Macで生成・再検査する

Unity 6000.6.5f1 / HDRP 17.6.0 / Input System 1.20.0。必要な既存アセットは[ベッドサイドのくまの報告](UNITY_BEDSIDE_BEAR_REPORT.md)を参照。リポジトリのルートから順に実行する。別プロジェクトには各Unityコマンドへ `--project /絶対パス` を指定する。

```bash
python3 scripts/unity_art_review.py stop
python3 scripts/unity_art_review.py reload-scripts
python3 scripts/unity_art_review.py star-create
python3 scripts/unity_art_review.py star-open
python3 scripts/unity_art_review.py star-check
python3 scripts/unity_art_review.py star-play
# 初期化を待つ。取得数0の新しいPlayでのみ実行する
python3 scripts/unity_art_review.py gameplay-status
python3 scripts/unity_art_review.py star-runtime
# 同じジョブのcompletedと Artifacts/Visual/star-play/<UTC>/complete.txt を待つ
python3 scripts/unity_art_review.py stop
python3 scripts/unity_art_review.py star-check
python3 scripts/collect_star_play_evidence.py --project "$HOME/Documents/Unity/sorajiro-unity" --capture Artifacts/Visual/star-play/20261024T005633251
python3 scripts/check_unity_source.py --editor /Applications/Unity/Hub/Editor/6000.6.5f1/Unity.app --project "$HOME/Documents/Unity/sorajiro-unity"
python3 -m unittest discover -s Tests -p 'test_*.py' -q
python3 scripts/validate_handoff.py
git diff --check
```

生成はreceiptが全一致する時だけ再利用し、編集済み・途中出力は上書きしない。既存の部屋を複製・再ベイクせず追加ロードする設計なので、macOSの専用Build Profileを用意する際には **StarPlay v003を起動シーン（先頭）、BedsideBear v002を追加ロード用**としてScene Listへ両方含める。既存Build Profileは変更していない。Apple Silicon向けにビルド後、Editor外で入力・音量設定・星の復帰を確認する。Playerビルドは今回未実施。
