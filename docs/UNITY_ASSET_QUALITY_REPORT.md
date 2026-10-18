# 部屋の素材・小物とAI制作工程の改善記録

2026-10-18。今回の小タスクはM2の部屋の質感改善と、M3の実Play確認経路の復旧。最新シーンは `Assets/Sorajiro/Generated/Twilight_v010/L_LookDev.unity`。参考画質への到達・所有者承認は未完了。

## 実装とアセットの状態

- `Tools/Blender/room_twilight_v6.py` / `room_twilight_v7.py`：保護された部屋v005の実 `.blend` を入力とする新版生成。交差する編みかご、開いた口・内張り・底、丸い折り返し・糸・房を持つブランケット、青い帯と高さ変化のあるラグ、窓外の立体の木々を追加。v006は中間試作、v007が今回の採用候補。既存版は保持。
- `TwilightLookDevBuilder.cs`：出力をv010へ更新。くまは既存のbear_v006・11ボーンGenericリグ・6クリップを維持。新しい部屋・材質・かごの衝突形状、星の発光、夕暮れの空、暖色補助ライトを組み込む。
- `TwilightSurfaceBuilder.cs`：木材の色・法線・線形Mask Mapを分離。壁の微細な凹凸、素材ごとの粗さ、編み物とラグの低い鏡面反射を設定。全て今回作成した素材で、写真やダウンロード素材の投影ではない。
- `ArtReviewAutomation.cs` / `scripts/unity_art_review.py`：既存のUnity CLI / Pipelineで実Editorを操作。処理をEditor更新イベントに移し、ジョブJSONの完了／失敗を待つ。任意のC#やシェルを受け付けない限定コマンド。
- `TwilightVerification.cs` / `collect_unity_twilight_evidence.py`：実インポート後の小物・UV・素材割り当て、Maskの線形インポート、反射、撮影元シーンを検証。
- [AIアセット制作方針](AI_ASSET_WORKFLOW_JA.md)：記事を参考にしたアセット分解、追加ソフトの判断、Tripo向けの初回仕様、権利・軸・素材の取り込み条件。

部屋の最新編集元は `Artifacts/SourceArt/Room/twilight_v007/TwilightRoom.blend`、実FBXとハッシュreceiptも同所。4,669編集オブジェクトを22グループにまとめ、2,040,184三角形を出力。ブランケットは3,840糸ループ、かごは48本の縦材と32段の横編み。この密度は試作用で、性能採用値ではない。

寝具の垂れは旧v005で実行したBlender Clothの110フレームを継承した静止形状。今回は再シミュレーションしていない。ゲーム中の布変形は未実装。

参照画像は参照状態、くまと部屋は **生成済み・Unityインポート済み・Metal実描画を観察済み・未承認の試作**。Tripoモデルは未生成・未導入。Auto-Rig Pro、Mixamo、新しい有料アセットは使っていない。

## 実際の画像と動画

- [ゲーム画角](../Artifacts/Visual/twilight/20261018T110738422/S02_Gameplay_pile_ON.png)
- [寝具と編み物](../Artifacts/Visual/twilight/20261018T110738422/S05_Bedding_pile_ON.png)
- [くまの正面](../Artifacts/Visual/twilight/20261018T110738422/S01_Portrait_pile_ON.png)
- [30秒の実Play動画](../Artifacts/Visual/motion/20261018T111029071/twilight-motion.mp4)
- [機械可読の検証記録](../validation/unity_twilight_v010_checks.json)

実機はApple M3 Max、Metal、Unity 6000.6.5f1、HDRP 17.6.0。固定画像は1080pのHDRP StandardRequest、6画角＋毛OFF比較2枚。同じカメラを使ったv008の基準は `Artifacts/Visual/twilight/20261013T095157941/`。途中のv009も `20261018T104037416/` に保持。

最初の実描画で規則的な木目、暗すぎるラグ模様、輪状で硬い編み目を発見し、v010で再調整した。最新の毛ON6枚と、動画から歩行・ジャンプ・走行・回転後のフレームを開いて観察。素材と小物の追加は確認できたが、編み物はまだ硬く見え、くまの短毛・アップリケ・脚も目標の触感や比率の審査を通っていない。窓外の木々も単純なシルエット。

動画は411枚、最終サンプル29.99016秒、実記録終了30.08336秒。CSVの実時刻で可変間隔のMP4にした。15fpsは取得の目標サンプリング数で、ゲームの実FPSではない。録画のGPU／CPU負荷を含み、性能の根拠にしない。

## 検証の結果と範囲

| 区分 | 結果 | 根拠・限界 |
|---|---|---|
| C#ソース | PASS | 導入済みUnity参照でRuntime/EditorをRoslynコンパイル。共用MotionPolicy 9・PlushGait 2,012 assertions |
| 生成保護などPython | PASS | `unittest discover` 21件。ゲーム描画のテストではない |
| Blender | 生成成功 | 実 `.blend` / Binary FBX / receipt。v006・v007の新規生成、v007再実行はREUSE |
| Unityアセット | PASS | 実インポート・スキン・クリップ変形・足裏・素材。ログは `Artifacts/UnityEditor/twilight-v010/` |
| 足裏クリップ検査 | PASS | 左877／右851頂点の支持重心。13サンプルで歩行約0.000002m、走行約0.000004mのずれ。一定速度・直線の支持期に限定 |
| 実衝突・Animator | PASS | 本番CharacterController / Step / Generic Animator。歩行0.09333m、ジャンプ頂点0.12635m、空中再ジャンプ拒否・境界・復帰。Locomotion/Jump/Fall/Landを観測 |
| 実描画 | 取得・観察済み | Metal固定8枚と30秒実Play。参考画質の承認ではない |
| キーボード／実フォーカス | 今回は未検証 | CLI呼び出しでは非フォーカスとなる。自動テスト・録画の間だけ明示的なfocus=trueの試験条件を供給し、終了時に元へ戻す。通常ゲームのフォーカス処理は変更していない |
| Player build・Windows | 未実行 | 今回はEditorでの画作りと検証に限定 |
| FPS・メモリ圧迫・長時間安定性 | 未計測 | 1080p/30fpsは引き続き目標。録画の枚数から推定しない |
| 物理GI・LOD・動的寝具 | 未確認／未実装 | 実AssetのSSGIはfalse。暖色補助ライトは美術上の補光で、計算した間接光ではない |

直接の長時間evalはPipelineの5秒制限でタイムアウトしたが、その後Editor側ではv009生成が完了したことをログとreceiptで確認した。最初のdelayCallジョブは開始せず、更新イベントに修正した。成功として採用したv010のジョブは `PipelineJobs/20261018T110524404_create.json`、`20261018T110820833_edit-tests.json`、`20261018T110951233_runtime-tests.json` で、いずれもcompleted。失敗・中間版を成功記録に混ぜていない。

## 実行コマンドと再現手順

既存のUnityプロジェクトを同じEditorで開く。以下はこのリポジトリのルートで実行する。既定のプロジェクト位置は `$HOME/Documents/Unity/sorajiro-unity`。他の場所なら各CLIに `--project /絶対パス` を付ける。

```bash
python3 Tools/Blender/room_twilight_v7.py
'/Applications/Blender 5.2.app/Contents/MacOS/Blender' --background --factory-startup --python Tools/Blender/room_twilight_v7.py -- --write
python3 scripts/check_unity_source.py --editor /Applications/Unity/Hub/Editor/6000.6.5f1/Unity.app --project "$HOME/Documents/Unity/sorajiro-unity"
python3 -m unittest discover -s Tests -p 'test_*.py' -v
python3 scripts/validate_handoff.py
python3 scripts/unity_art_review.py status
python3 scripts/unity_art_review.py stop
python3 scripts/unity_art_review.py refresh
```

statusでPlay=false / compiling=false / dirty=falseを確認する。dirty=trueなら所有者の編集を明示的に保存し、生成物は別リビジョンにする。新しい生成先や既存版を消してやり直さない。

```bash
python3 scripts/unity_art_review.py create
python3 scripts/unity_art_review.py open
python3 scripts/unity_art_review.py capture
```

captureは開始応答であり完了ではない。新しい `Artifacts/Visual/twilight/<UTC>/complete.txt` と8PNGを確認してから次へ。

```bash
python3 scripts/unity_art_review.py edit-tests
python3 scripts/unity_art_review.py play
python3 scripts/unity_art_review.py runtime-tests
python3 scripts/unity_art_review.py motion
```

新しい `Artifacts/Visual/motion/<UTC>/complete.txt` を待つ。続く2コマンドの `<UTC>` は実際の新規フォルダに置き換える。

```bash
python3 scripts/encode_unity_motion.py Artifacts/Visual/motion/<UTC>
python3 scripts/unity_art_review.py stop
python3 scripts/collect_unity_twilight_evidence.py --project "$HOME/Documents/Unity/sorajiro-unity" --capture Artifacts/Visual/twilight/<UTC> --motion Artifacts/Visual/motion/<UTC> --revision v010 --bear-revision twilight_v006 --room-revision twilight_v007
```

通常の手動確認はUnityの `Sorajiro > Twilight > 3. Open twilight lookdev` → Play → Gameビューをクリック。WASD移動、Shift走行、Spaceジャンプ、右ドラッグ回転、ホイールズーム、R復帰、Esc一時停止。今回の自動テストと別に、実際の入力と足運びを所有者が確認する。

生成済みv010の再利用確認も実行した。出力やレシピを変更したときは無断上書きを拒否する。`Artifacts/` の大きなDCC／画像データはローカル保持状態で、公開・LFS移行を今回行っていない。

## 次の小タスク

最も影響が大きいのは、参考画像にある柔らかい間接光と厚みのある布の表現。次はベッド・ランプ・窓・床の範囲で、既存HDRP設定を保護したベイクGI／動くくまへの光の受け渡しを小規模に試験し、同じ固定カメラで比較する。現時点でSSGIやGIの性能・見た目は検証済みとしない。くまの脚と移動の速度変化、アップリケのしわ、短毛の密度も継続課題。Tripoの初回比較は布製収納箱1点からでよく、登録・有料プラグイン購入は今回の成果の利用条件ではない。
