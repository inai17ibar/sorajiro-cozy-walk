# 正面光を抑えて毛並みを読む照明

2026-10-24。所有者の「くまに直接のライトを当てすぎ」という指摘に合わせ、面を均一に明るくしていた正面光を抑え、斜めの面光源へ配分を変えた。**FleeceLighting v002のSide600を初期表示**にする。毛・モデル・素材・露出を変更したという意味ではなく、既存の毛と布の凹凸を読みやすくする照明の試作。

## 開く

既存Macプロジェクト `$HOME/Documents/Unity/sorajiro-unity` の **Sorajiro → Fleece Lighting → 2. Open soft-lit star room** を選び、**▶でPlay**する。調整はPlay時に適用するため、Editモードの部屋だけを見て旧照明と混同しないこと。次の3シーンを一緒に開く。

- `Assets/Sorajiro/Generated/BedsideBear_v002/L_LookDev.unity`：くまと部屋、保存済みライトマップ/APV。
- `Assets/Sorajiro/Generated/StarPlay_v003/L_LookDev.unity`：星の取得、エフェクト、音。
- `Assets/Sorajiro/Generated/FleeceLighting_v002/L_LookDev.unity`：今回の照明。

操作はWASD / Shift / Space、右ドラッグ、ホイール。星と音の操作は[前回の手順](UNITY_STAR_PLAY_REPORT.md)どおり。既存のStar Playだけを開くメニューでは今回の照明を追加しない。

## 実画像

| 画角 | 従来 | 今回の初期表示 |
|---|---|---|
| 顔と胴 | [従来](../Artifacts/Visual/fleece-light/20261024T014436200/S01_Portrait_Before.png) | [正面光を抑えた版](../Artifacts/Visual/fleece-light/20261024T014436200/S01_Portrait_Side600.png) |
| 毛・白い口元の拡大 | [従来](../Artifacts/Visual/fleece-light/20261024T014436200/Fleece_Close_Before.png) | [調整後](../Artifacts/Visual/fleece-light/20261024T014436200/Fleece_Close_Side600.png) |
| 斜め全身 | [従来](../Artifacts/Visual/fleece-light/20261024T014436200/S03_ThreeQuarter_Before.png) | [調整後](../Artifacts/Visual/fleece-light/20261024T014436200/S03_ThreeQuarter_Side600.png) |

実M3 Max / Metal / HDRP / 1920×1080、TAA暖機40回。4画角×従来・Side600・明るめのSide850の合計12枚。同じメッシュ・素材・姿勢・カメラ・固定露出EV7・部屋のベイクを使う。生成画像、色加工した比較、性能測定ではない。

[30秒の実Play動画](../Artifacts/Visual/motion/20261024T014622369/twilight-motion.mp4)、[検証記録](../validation/unity_fleece_light_v002_checks.json)。実行30.06371秒、428枚、最終サンプル29.99722秒。CSVの実時刻でMP4化した。歩行・ジャンプ・走行・旋回区間の4サンプルを目視観察した。連続動画全体の毛のちらつきや動作美術の合格とは区別する。

正面の水色が一様に明るくなる状態を抑え、頬、腹、白い口元と雲の縁に陰影が残る。表面の細かな起伏も読みやすくなったが、参照の密な繊維と毛同士の遮蔽には差がある。画面上の毛の本数を増やしたり、素材のディテールを今回追加したりはしていない。顔が暗くなりすぎた最初のv001（160/220lm案）は不採用として保存した。

## 光の変更

| 光 | 従来 | Side600 |
|---|---|---|
| 旧主光源・方向光 | 370lux | 70lux（約81%減） |
| 正面の部屋適応用補光 | 240lm | 45lm（約81%減） |
| 反対側の柔らかい補光 | 65lm | 18lm |
| 後ろの縁の光 | 55lm | 35lm |
| 新しい斜めの面光源 | なし | 600lm、幅0.28×高さ0.45m |

luxとlmは異なる量なので合算して「照明全体を8割減らした」とは扱わない。弱めたのは特に正面を均す光。新しい面光源は開始地点から左0.42m、上0.48m、手前0.32mに配置し、くまの高さ0.25mへ向ける。明るめの850lm案も比較したが、600lmを初期値とした。

光源はプレイヤーへ追従せず開始地点付近に固定する。移動や向きによって明るさ・陰影が変わり、遠くの背面は暗くなる。全方向・部屋全域で同じ毛の見え方を保証しない。

`Runtime/FleeceLighting.cs` はStar Playの準備完了後に主役用ライトの値を変更し、元の値を保持する。`Editor/FleeceLightingBuilder.cs` は追加シーンと面光源を生成し、入力と出力をreceiptで保護する。元のシーン、マテリアル、モデル、Controller、APV、露出、部屋のライトは保存し直さない。`FleeceLightingCapture.cs` は姿勢を固定して比較撮影し、完了後にSide600と通常の入力・Animatorへ戻す。Play停止時は元の部屋の状態へ戻る。

## 検証と残課題

実Unity参照でRuntime/Editorコンパイル成功。共用MotionPolicy / PlushGait / FootstepBeat / 音源生成の既存検査も成功。照明は可逆な美術調整なので、実装を写すだけの新規単体テストは追加していない。実画像12枚、30秒Play、画像ごとの実際の光量・位置・露出のJSON、保護された全入力と新出力のハッシュ照合を行った。撮影用カメラと停止状態は終了時に片付ける。

アセット状態は **生成済み・実Unity読み込み済み・Metal比較描画と動きを観察済みの照明試作、所有者未承認**。星と音のソース・アセットは保持し、今回の動きも同じ追加シーンの上で記録した。星取得全28項目・実スピーカーの聴感の再検査、Player build、Windows、FPS/GPU時間/メモリ圧力は今回行っていない。

残る大きな視覚課題は、繊維同士の細かな陰影、毛の密度感、足元の接地と股下の変形。次はこの照明で毛の表面と縁を拡大し、繊維の遮蔽・素材側を小さい範囲で調整する。光だけで参照品質に到達したとは扱わない。

## Macで再現する

Unity 6000.6.5f1 / HDRP 17.6.0 / Input System 1.20.0。追加ソフト不要。別のMacには先に[部屋](UNITY_BEDSIDE_BEAR_REPORT.md)と[Star Play](UNITY_STAR_PLAY_REPORT.md)の生成済み入力が必要。リポジトリ直下から実行する。

```bash
python3 scripts/unity_art_review.py stop
python3 scripts/unity_art_review.py reload-scripts
python3 scripts/unity_art_review.py light-create
python3 scripts/unity_art_review.py light-open
python3 scripts/unity_art_review.py light-play
python3 scripts/unity_art_review.py light-capture
# 同じジョブのcompletedと12枚・complete.txtを待つ
python3 scripts/unity_art_review.py motion
# motion/<UTC>/complete.txtを待ち、実際の生成フォルダーを指定
python3 scripts/encode_unity_motion.py Artifacts/Visual/motion/<UTC>
python3 scripts/unity_art_review.py stop
python3 scripts/unity_art_review.py light-check
python3 scripts/collect_fleece_light_evidence.py --project "$HOME/Documents/Unity/sorajiro-unity" --capture Artifacts/Visual/fleece-light/20261024T014436200 --motion Artifacts/Visual/motion/20261024T014622369
python3 scripts/check_unity_source.py --editor /Applications/Unity/Hub/Editor/6000.6.5f1/Unity.app --project "$HOME/Documents/Unity/sorajiro-unity"
python3 scripts/validate_handoff.py
git diff --check
```

各Unityコマンドは別プロジェクト用に `--project /絶対パス` を取る。生成はreceipt全一致時のみ再利用する。既存出力の編集や不一致は削除・上書きせず新リビジョンへ進む。Pipelineがタイムアウトした場合は同じジョブを確認し、重複生成しない。MacのPlayer検査では専用Build ProfileにFleeceLighting v002を先頭、StarPlay v003とBedsideBear v002を追加ロード用として含め、Apple Silicon向けにビルドしてEditor外の描画・音・操作を確認する。既存のBuild Profileは変更していない。
