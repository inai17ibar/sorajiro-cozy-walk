# SORAJIRO / THE SUNLIT ROOM — Unity HDRP

**2026-10-24 更新：足音・ジャンプ音と俯瞰カメラを改良しました。** 最新の入口は `Sorajiro → Play Comfort → 2. Open overhead camera and cute sounds`。26度の俯瞰、Q/Eの45度旋回、Cで背面へ復帰、画面ボタンを追加し、追従と移動方向の参照切れも修正。実Play28項目・Q/E/C実キー・Metal画像4枚・30秒の動作を確認しました。聴感・操作感の所有者承認、Player build、性能は未完了です。[実画像・音・操作・再現手順](docs/UNITY_COMFORT_PLAY_REPORT.md)。

**2026-10-24 更新：くまの正面光を抑え、斜め光で毛並みを読む版を追加しました。** `Sorajiro → Fleece Lighting → 2. Open soft-lit star room` から開き、▶で適用します。元のくま・部屋・星と音は保持。実Metal固定12枚と30秒Playを記録し、画質承認・Player build・性能は未完了です。[比較画像・変更・手順](docs/UNITY_FLEECE_LIGHTING_REPORT.md)。

**2026-10-24 更新：星への接触・取得エフェクト・効果音を追加しました。** 床の星3つを集めると光の粒が広がって消え、取得数と完了を表示します。足音・着地音、Mでミュート、音量保存も追加。Unityの `Sorajiro → Star Play → 2. Open playable bedside stars` で部屋と追加シーンを一緒に開けます。元のくまと部屋は保持。実トリガー・音声DSP・Metal画像を検査し、Player build・性能・聴感承認は未完了です。[実画像・音・再現手順](docs/UNITY_STAR_PLAY_REPORT.md)。

**2026-10-24 更新：選択されたBearFit v001・比較3をベッドサイドへ配置しました。** 新シーンは `Assets/Sorajiro/Generated/BedsideBear_v002/L_LookDev.unity`。素材色を維持し、部屋用の補光・専用GPUベイク/APVを追加。実Metal固定6枚と30秒Playを記録しました。Unityの `Sorajiro → Bedside Bear → 2. Open selected bear in bedside room` で開けます。足元・股下・室内画質承認・Player build・性能は残課題です。[実画像・検証・Mac手順](docs/UNITY_BEDSIDE_BEAR_REPORT.md)。

**2026-10-19 更新：体型と正面の毛並みの比較試作はBearFit v002。** 前回2の素材を維持し、腹部の丸み・脚の間隔・面光源を調整。実Metal固定12枚、6クリップの変形と足裏検査、30秒ルートのPlayを記録しました。Unityの `Sorajiro → Bear Fit → 2. Open shape and lighting study` から開けます。股下の伸び・接地影・画質承認・Player build・性能は残課題です。[実画像・検証とMac手順](docs/UNITY_BEAR_FIT_REPORT.md)。

**2026-10-19 更新：短毛の比較試作はFleece v005。** 寝た細い毛束と布の微細な凹凸を追加し、実Metalのスタジオ11枚・室内4枚と30秒ルートのPlay動画を取得しました。既存の部屋はBedside v009を保持。Unityの `Sorajiro → Fleece → 2. Open study`、Play後の数字1/2/3で比較できます。参照品質・Player build・性能は未判定です。[実画像・変更・確認手順](docs/UNITY_FLEECE_REPORT.md)。

**2026-10-19 更新：最新の画質試作はBedside v009。** 枕・布団の縫い目・編み糸の断面を改修し、床／壁の光漏れ対策とAPV保存の安定化を追加しました。実Metalの固定比較7枚、30秒Play動画、再読み込み後のアセット検査を完了。画質承認・Player build・性能は未完了です。[実画像・検証・Mac手順](docs/UNITY_BEDDING_REPORT.md)。

**2026-10-18 更新：前回の画質試作はBedside v003。** ベッド脇にGPUベイク・APV・Cotton/Wool布素材を導入し、実Metalの固定比較7枚と30秒Play動画を取得しました。旧Twilight v010は保護した入力として保持。画質承認・Player build・性能は未完了です。 [実画像・動画・検証と手順](docs/UNITY_BEDSIDE_REPORT.md)。

**2026-10-18 更新：前回の試作はTwilight v010。** 編みかご・立体の編み物・ラグ模様・木材素材・窓外の形状・発光と補光を更新しました。実Unity/Metalの1080p固定8枚と30秒Play動画を取得し、実アセット・足裏・衝突・Animatorを検証。画質承認・Player build・性能は未完了です。 [変更・実画像・動画・再現手順](docs/UNITY_ASSET_QUALITY_REPORT.md)、[追加ソフトとTripo制作方針](docs/AI_ASSET_WORKFLOW_JA.md)。以下は過去の記録です。

**2026-10-12：所有者のUnity選択に合わせて、ネイティブUnity版のM1操作土台を追加しました。** 実際の環境はUnity **6000.6.5f1（6.6）／HDRP 17.6.0／Input System 1.20.0**です。

入口は[Unity開発・実機確認手順](docs/UNITY_START.md)と[今回の実装・検証報告](docs/UNITY_M1_REPORT.md)です。`Unity/Packages/com.sorajiro.cozywalk`を既存HDRPプロジェクトに接続します。既存SampleSceneを残し、`Assets/Sorajiro/Generated/M1_v001/L_LookDev.unity`を本物のEditorで生成します。

**2026-10-13の記録：Twilight v006。** 指定マスターと全方向シートを基準に、全身の短毛・試作リグ、待機/歩行/走行/ジャンプ/落下/着地、夕暮れの立体室内を追加しました。布団は実Blender Clothの重力/マットレス衝突で作った垂れ、厚み、縫い目を持ちます。ゲーム内では静止メッシュです。実Unity/Metalの1080p固定8枚と30秒Play動画を取得しました。[実画像・動画・再現手順・残課題](docs/UNITY_TWILIGHT_REPORT.md)。**目標画質の承認、Player build、性能は未完了**です。旧M1/M2と中間版は保持しています。旧M2は所有者が画質を目標に遠いと判断しました。

以下は保存したUnreal版の引き継ぎ記録です。現在の開発先と混同しないでください。

**2026-10-02 / 要件書 v1.0 / M0・M1の未ビルドC++骨格と生成手順を追加。UEゲームの起動・画質・性能は未確認です。**

青い雲模様のくまのぬいぐるみで、暖かな部屋を歩き回るネイティブ3Dゲームを作ります。
優先順位は **参照画像への見た目の近さ → 動いたときの安定性 → 実機性能 → 遊びの拡充** です。
クラウドCodexをコードと制作手順の作成に、手元のMacBook Pro M3 Max／36GBをUEビルド・描画確認に使います。

## まず行うこと

開発開始後の入口は [Mac初回ビルド・生成手順](docs/MAC_FIRST_BUILD.md) と [M0・M1実装報告](docs/M0_M1_REPORT.md) です。`Unreal/Sorajiro.uproject`にRuntimeソース、`Tools/Unreal/`にEditor生成レシピを置いています。以下の引き継ぎ手順も保持します。

1. このフォルダーの**中身**を、新しい開発用Gitリポジトリのルートに置きます。既存リポジトリのファイルには無断で上書きせず、差分を確認してください。個人の非公開リポジトリを初期案とします。
2. `reference/` の画像もコミットします。会話内の画像や過去の添付は、Codexのリポジトリに自動で渡る前提にしません。
3. Codex Cloudでリポジトリを選び、`docs/CLOUD_AND_LOCAL.md` の方針で環境を用意します。
4. 最初のタスクに `prompts/01_CLOUD_FIRST_TASK.md` の本文を貼ります。クラウドにUEやGPUがなくても進められる土台から作ります。
5. MacのCodex CLI／ローカル実行環境では `prompts/02_MAC_FIRST_RUN.md` を使い、UEの実ビルドと初回描画を確認します。

## 読むファイル

| ファイル | 用途 |
|---|---|
| [REQUIREMENTS_JA.md](REQUIREMENTS_JA.md) | ゲームの目的・必須機能・画質・対象機・受け入れ条件。仕様の中心 |
| [AGENTS.md](AGENTS.md) | Codex向けの短い恒常指示。リポジトリのルートに置く |
| [docs/CLOUD_AND_LOCAL.md](docs/CLOUD_AND_LOCAL.md) | クラウドとMacの分担、セットアップ、PRの往復 |
| [docs/RENDERING_PLAN.md](docs/RENDERING_PLAN.md) | ぬいぐるみ・毛・光・室内美術の制作と比較実験 |
| [docs/ARCHITECTURE.md](docs/ARCHITECTURE.md) | C++、UE Editor Python、アセット、Gitの構成 |
| [docs/MILESTONES.md](docs/MILESTONES.md) | 小さなタスクと完了ゲート。最初に画質を検証する |
| [docs/VERIFICATION.md](docs/VERIFICATION.md) | ビルド・アセット・見た目・性能を別々に確認する方法 |
| [docs/SOURCES.md](docs/SOURCES.md) | 公式資料と、確認した範囲・バージョン依存の注意 |
| [reference/README.md](reference/README.md) | 同梱画像の意味。目標画像と旧実描画を混同しない |
| [templates/LOCAL_REPORT.md](templates/LOCAL_REPORT.md) | Macで試した結果をCodexへ戻す報告書 |
| [templates/feature_matrix.json](templates/feature_matrix.json) | 機能対応を実測して埋める表。初期値は未検証 |

## この配布物で実行できる確認

```bash
python3 scripts/validate_handoff.py
bash scripts/cloud_setup.sh
python3 scripts/check_source.py
```

前2つは文書・JSON・参照画像の整合確認です。`check_source.py`はPython構文、Editor生成保護ロジックとRuntime共用のカメラ数値制約を検査します。**UEのコンパイル、Groomの描画、ゲームの起動、M3 MaxでのFPSの検証ではありません。**
同梱スクリプトはUE本体を取得せず、外部通信や課金サービスの設定も行いません。

## 引き継ぎ上の重要事項

- 新版はUEネイティブ版です。旧HTML版の「単一HTML」「WebGL」「localStorage互換」は、新版の必須条件ではありません。
- 旧ブラウザ版は破壊せず別に保管します。本パックには旧ゲームのソース一式は再同梱していません。
- 参照イメージは、完成したUE版のスクリーンショットではありません。
- `.uproject`とC++ソースは追加済みですが未ビルドです。完成した全身メッシュ、Groom、生成済みUEアセット、Engine本体は入っていません。
- 作成済みの頭部GLBを持っていても、全身リグ・歩行・Groomが完成しているとは扱いません。
- 現セッションでmacOS 27.0.1／Xcode 27.0／Blender 5.2.0 LTSを観測しました。UEは標準配置先等で未検出、版固定と組み合わせの互換性確認は保留です。
- 参照画像や未確認アセットのライセンスを、リポジトリのコードライセンスで一括許諾しないでください。
