# 脚・足運びと背景照明の改修

2026-10-13、所有者の「脚の形と足運びに違和感がある」「背景と照明を参照に近づけたい」という指摘を受けた改修です。最新の生成シーンは **`Assets/Sorajiro/Generated/Twilight_v008/L_LookDev.unity`**。旧v006と比較用v007は保持しました。モデルの改善という所有者の感想を、完成画質の承認とは扱いません。

## 変更した内容

脚は角柱に近いsuperellipsoidから、断面が丸く上側へ先細りする短い脚に変更。青布・足裏・毛を同じ形とウェイトへ作り直しました。足首 `foot_L/R` を追加し11骨になっています。頭・顔・胴の基本形は直前のマスター版を維持しています。

歩行は脚の回転と平行移動を重ねた正弦波から、接地／振り出しを分けた軌道へ変更しました。接地では足がキャラクターの前進速度と同じ速さで後退し、振り出し時だけ持ち上がります。足首は脚と独立して地面へ向け、胴の上下動を足へ伝えません。足首の姿勢を基準に脚の向きを解き、速度に合わせて歩行周期を変えます。待機・歩行・走行・ジャンプ・落下・着地は引き続き編集可能なUnityクリップです。各56曲線、歩行周期0.52秒／走行0.40秒、基準速度0.28m/s／0.52m/s。root motionは無効で、本番CharacterControllerが移動を担います。

背景は床を長い板の千鳥配置に変更し、木の細かな凹凸と材質ごとの粗さを追加。植物を球から曲面の尖った葉・葉脈・鉢の縁・垂れた蔓へ変更しました。布団には元の重力シミュレーション形を使い、最終表面へ縫い目を再投影、雲模様と控えめな織り目を追加。天井も追加しています。これらの図柄と凹凸はオリジナルの手続き生成で、参考画像をテクスチャへ投影していません。

照明は窓の青とランプの暖色を分け、均一な補助光を弱め、床へ暖かい光を落とすよう調整しました。ランプのシェードによる鋭い遮蔽を抑制し、HDRPの室内Reflection Probeと画面空間反射を追加。実Reflection textureは256pxで取得できました。GIをベイクしたという意味ではありません。DOFとMotion Blurは引き続き無効、EV6.5固定です。

主な変更ソースは `Tools/Blender/bear_twilight_v6.py`、`room_twilight_v5.py`、Unity packageの `Editor/BearAnimationBuilder.cs`、`TwilightLookDevBuilder.cs`、`TwilightSurfaceBuilder.cs`、`BearAnimationVerification.cs`、`TwilightVerification.cs`、`Runtime/PlushGait.cs`、`BearAnimationDriver.cs`、`MotionCaptureProbe.cs`。本番共用ロジックのテストを `Tests/Unity/PlushGaitTest.cs` と `scripts/check_unity_source.py` に追加しました。証跡収集スクリプトはrevisionとEdit-only確認を明示できるよう更新しました。

## 実際に確認できたこと

実機は前回と同じM3 Max/30 GPU cores/36GB、macOS27.0.1、Unity6000.6.5f1、HDRP17.6.0、Input System1.20.0、Blender5.2.0 LTS、Metalです。

| 検査 | 結果と証跡 |
|---|---|
| Blender生成 | 実行成功。モデルと部屋の実FBX／編集可能blend／receiptを保存。再実行のdry-runはREUSE |
| C# | 実Unity参照のRoslyn Runtime/EditorコンパイルPASS。`validation/unity_source_checks.json` |
| 本番共用ロジック | MotionPolicy 9 assertions、PlushGait 2012 assertions PASS。接地の速度相殺・平らな支持・振り出しの床離れ・周期・速度追従・不正値拒否 |
| パイプライン | Pythonの21 tests PASS。生成保護など既存の本番処理を検査 |
| 実Unityスキン | body 113,470頂点/206,760三角形、毛928,672頂点/598,684三角形、各11骨。UV・材質・頭/腕/脚と毛の変形PASS |
| 実Unity clip | 6クリップ、各56曲線のbindingと実メッシュ変形PASS |
| 実足裏 | 実import meshの足首ウェイト0.97超、最底面から4mm以内の877/851頂点を使用。左右の歩行/走行について、接地区間を各13回実clip samplingし、仮想的な一定前進量を加算。重心の最大ずれは歩行約0.003mm、走行約0.006mm。**一定速度の直進支持区間の検査で、実Play全体の足滑りの合格ではない** |
| 実材質と反射 | 実生成PNGの青/白のコントラスト、import material binding、実HDRP reflection texture 256pxを確認 |
| 実描画 | 1920×1080固定8枚の取得成功。ON全6構図を開いて確認。毛OFFは顔/室内に保存。AA=TAA、dynamic resolution/DLSS/FSR2無効、各構図24 render requests後に保存 |
| 証跡整合 | `validation/unity_twilight_v008_checks.json`。生成receipt・アセンブリ更新・PNG・実検査ログを照合。Edit-onlyであり、現revisionのlive executionはfalse |

実検査ログは `Artifacts/UnityEditor/twilight-v008/` に保存しました。参照／生成量／API成功を画質承認の代用にしていません。

画像は `Artifacts/Visual/twilight/20261013T095157941/`：

- [室内・照明](../Artifacts/Visual/twilight/20261013T095157941/S02_Gameplay_pile_ON.png)
- [くま全身・脚](../Artifacts/Visual/twilight/20261013T095157941/S03_ThreeQuarter_pile_ON.png)、[足元](../Artifacts/Visual/twilight/20261013T095157941/S04_Contact_pile_ON.png)
- [布団と木材](../Artifacts/Visual/twilight/20261013T095157941/S05_Bedding_pile_ON.png)
- [顔](../Artifacts/Visual/twilight/20261013T095157941/S01_Portrait_pile_ON.png)、[背面](../Artifacts/Visual/twilight/20261013T095157941/S06_Back_pile_ON.png)

v007で布団が白くなる誤りを実画面で発見しました。Mathf.SmoothStepの引数をしきい値関数と取り違えていたため修正し、旧版を消さずv008を生成。実PNGの青/白コントラストとmaterialの参照を検査するメニューを追加しました。床・照明・植物は改善していますが、参照の豊かな間接光、柔らかい影、毛束、家具の使い込まれた細部までは届いていません。

## 未完了の実行確認

v008でPlayを開始した後、操作ツールが `cgWindowNotFound` を返してUnityウィンドウへ接続できなくなりました。アプリ再取得と操作接続の初期化でも解消していません。Editor.logにはPlay開始を確認できますが、**v008のAnimator/衝突のlive検査、新しい30秒動画、実際の足運びの自然さは未確認**です。以前のv006動画を今回の修正後の証拠へ流用しません。Play終了や現在のGameビュー表示も確認できていません。

Player build、Windows、GPUフレーム時間／メモリ／安定30fps、LOD、所有者の画質承認も未確認。布団は静止メッシュで、乗ったときの変形は未実装です。

## 実行コマンドと次のMac手順

リポジトリのルートで以下を実行しました。Blenderは実生成まで、C#検査は実Editor描画とは別に実行しています。

```bash
python3 Tools/Blender/bear_twilight_v6.py
python3 Tools/Blender/room_twilight_v5.py
'/Applications/Blender 5.2.app/Contents/MacOS/Blender' --background --factory-startup --python Tools/Blender/bear_twilight_v6.py -- --write
'/Applications/Blender 5.2.app/Contents/MacOS/Blender' --background --factory-startup --python Tools/Blender/room_twilight_v5.py -- --write
python3 scripts/check_unity_source.py --editor /Applications/Unity/Hub/Editor/6000.6.5f1/Unity.app --project "$HOME/Documents/Unity/sorajiro-unity"
python3 -m unittest discover -s Tests -p 'test_*.py' -v
python3 scripts/collect_unity_twilight_evidence.py --project "$HOME/Documents/Unity/sorajiro-unity" --capture Artifacts/Visual/twilight/20261013T095157941 --revision v008 --bear-revision twilight_v006 --room-revision twilight_v005 --edit-only
python3 scripts/validate_handoff.py
```

Unityプロジェクトは `$HOME/Documents/Unity/sorajiro-unity`。操作接続が戻ったら次を実行します。

1. Play状態を確認。停止して `Sorajiro > Twilight > 3. Open twilight lookdev` でv008を開く。生成済みなので再生成不要。▶→Gameビューをクリックし、WASD/矢印、Shift、Space、右ドラッグ、ホイールで確認。
2. Edit中の `Twilight > 9. Test planted sole motion` と `10. Test room surfaces and reflection capture` は再実行可能。反射検査は固定撮影またはGameビュー描画後に行う。
3. Play中、`Twilight > 7. Test live Animator transitions` と通常メニューの `7. Test motor physics` を実行。続いて `4. Log runtime status`、`Twilight > 8. Record 30s scripted motion`。記録ルートは待機5秒、歩行約10秒、ジャンプ2回、走行、旋回、接近。終了後Playを止める。
4. 新しくできた実記録フォルダーを `python3 scripts/encode_unity_motion.py Artifacts/Visual/motion/<UTC名>` で動画へ変換。前述の証跡収集コマンドから `--edit-only` を外し `--motion Artifacts/Visual/motion/<UTC名>` を追加。新しい実検査ログとruntime snapshotが必要。
5. 足の着地、蹴り出し、加減速、左右への旋回、ラグ端の段差を動画で見る。検査は一定速の支持区間のみなので、補間や旋回で足が滑る場合はここで調整する。足の世界位置を固定する地形追従IKはまだありません。

Blender編集元は `Artifacts/SourceArt/Bear/twilight_v006/Sorajiro_Bear_M2.blend` と `Artifacts/SourceArt/Room/twilight_v005/TwilightRoom.blend`、Unityのcontroller/6 `.anim` はv008フォルダーです。m/Z-up/-Y正面→FBX -Z/Y→Unity m/Y-up/+Z。手編集は複製した別フォルダーで行い、生成物を無断で上書きしません。大きい資産はローカルのignored ArtifactsとUnityプロジェクト内で、Gitだけでは別Macへ運ばれません。

最も影響の大きい次の作業は**実Playで修正後の足運びを確認し、加減速・旋回・段差を詰めること**です。背景はその次に、参照の柔らかい間接光と窓の外の奥行き、木材/布の細部を比較します。現在の状態は生成・import・Editクリップ検査・固定実描画までの試作で、動作と画質の承認待ちです。
