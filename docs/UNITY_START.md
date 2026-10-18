# Unity HDRPでの開発・実機確認

**2026-10-18 更新：最新はTwilight v010。** 編みかご・立体の編み物・ラグ模様・木材素材・窓外の形状・発光と補光を更新しました。実Unity/Metalの1080p固定8枚と30秒Play動画を取得し、実アセット・足裏・衝突・Animatorを検証。画質承認・Player build・性能は未完了です。 [変更・実画像・動画・再現手順](UNITY_ASSET_QUALITY_REPORT.md)、[追加ソフトとTripo制作方針](AI_ASSET_WORKFLOW_JA.md)。以下は過去の記録です。

**2026-10-13の過去記録：Twilight v006。`Sorajiro > Twilight > 3. Open twilight lookdev`→▶→Gameビューをクリック。** 全身短毛、編集可能な歩行/走行/ジャンプなど6クリップ、重力と衝突で垂れを作った静止寝具を追加しました。[実画像・30秒動画・編集元データ・再生成・未確認範囲](UNITY_TWILIGHT_REPORT.md)を参照してください。M2は所有者から画質未達の指摘を受けており、以下のM1/M2記録を最新品質の説明として読まないでください。

2026-10-12追記：M2の全身制作モデルを生成・取り込み済みです。**`Sorajiro > M2 > 3. Open bear lookdev`→▶→Gameビューをクリック**で操作できます。生成、固定1080p撮影、未確認範囲は[M2報告と手順](UNITY_M2_REPORT.md)を参照してください。以下のM1は初回導入の記録です。旧M1のVolume部品未保存が判明したため、そのEV9／効果無効化は保存後の有効値として保証しません。M2では修正し実検査しました。

現在の主対象はMacBook Pro M3 Max／36GBのネイティブゲームです。Unity **6000.6.5f1**（Editor表示は**6.6**）、HDRP **17.6.0**、Input System **1.20.0**を実プロジェクトから観測しました。「6.5」という呼び方より`ProjectSettings/ProjectVersion.txt`の完全な版を優先します。Blenderは別の制作ツールで、HDRPに内蔵されるものではありません。

HDRPはUnityの高品質な描画パイプラインです。開いたHDRPテンプレートには導入済みで、追加購入や再インストールは不要です。HDRPを入れるだけで毛・良い造形・間接光が完成するわけではありません。

現在のプロジェクトは`$HOME/Documents/Unity/sorajiro-unity`、ソースはこのリポジトリの`Unity/Packages/com.sorajiro.cozywalk`です。絶対パスを別Macへそのままコピーしないでください。依存の接続はローカル`file:`参照なので、リポジトリを移動したら参照の変更が必要です。既存の他の依存は保持します。

初回導入／再接続はリポジトリのルートで次を実行します。先に差分予定を確認し、`--apply`でmanifestだけを変更します。元manifestは`Artifacts/UnityLink/`へハッシュ名でバックアップします。すでに同じ参照なら書き換えません。別のSorajiro依存や未確認の版は拒否します。

```bash
python3 scripts/link_unity_package.py --project "$HOME/Documents/Unity/sorajiro-unity"
python3 scripts/link_unity_package.py --project "$HOME/Documents/Unity/sorajiro-unity" --apply
```

Unityのコンパイル終了後、上部`Sorajiro`メニューを使います。

1. `1. Preflight`：HDRP・版・未保存シーン・生成先を確認。書き込みません。未保存の所有者編集がある場合は自分で保存するか取り消してから進めます。
2. `2. Create LookDev scene`：別シーンを加算で作り、保存後は元シーンへ戻ります。初回だけ生成し、同一版・同一ハッシュなら再実行で何もしません。生成済みの編集や部分生成は保護のため拒否します。改修は新しい生成リビジョンへ出してください。生成レシピを変える際は`Output`と`Recipe`を更新します。
3. `3. Open generated LookDev scene`：検証シーンを開きます。SampleSceneを上書きしません。
4. 上中央の▶でPlay。**Gameビューをクリックして**入力を渡します。WASD／矢印で移動、Shiftで走る、Spaceでジャンプ、右ドラッグで旋回、ホイールでズーム、Escで操作停止、Rで開始点へ復帰。現在の停止はプレイヤーとカメラ入力に限定したM1実装です。
5. `4. Log runtime status`で位置・接地・停止・カメラ・画面ピクセル寸法を保存。`5. Capture Game view`で次のフレームをPNG保存。保存先はリポジトリの`Artifacts/UnityEditor/`です。PNGの存在と中身を確認してください。
6. Play停止後、`6. Test generated-content protection`で実UnityのJSONを使う6つの保護検査を実行。テスト専用フォルダーだけを片付けます。
7. Play中、`7. Test motor physics`で本番`Step`と実CharacterControllerの移動・ジャンプ・衝突を検査できます。検査はプレイヤーを開始位置へ戻します。同期APIのスモーク検査で、キーボード・マウスの実入力や長時間プレイの代用ではありません。

RuntimeはC#＋CharacterController＋Input System、EditorはC#＋HDRP標準Lit／Volume／Lightです。M1は6m四方の簡易室内、窓開口、床、ラグ、ジャンプ台、約40cmの仮くま、追従カメラ、固定カメラ4個を持ちます。星・保存・休憩・フォトモード・画質設定は未実装です。現在のキャプチャメニューはEditor検証用で、ゲームのフォトモードではありません。

Unityの単位は**m、Y-up、+Z前方**です。M2ではBlenderのm／Z-up／-Y正面からFBX Forward=-Z、Up=Yでexportし、Unityの実インポートで約40cmと+Z正面を確認しました。詳細はM2報告を参照してください。UEのcm／Z-up／+X前方レシピをそのまま使わないでください。

固定カメラは`S01_Portrait`、`S02_Gameplay`、`S03_ThreeQuarter`、`S04_Contact`です。画角はUnityの**垂直FOV**です。4枚の比較撮影はまだ別途必要です。現在は固定露出EV9、DOF／Motion Blur／Bloomを生成Volumeで無効化しています。テンプレートの既定設定を全て置き換えたとは扱いません。木目、ラグの毛、GIのベイク、完成くま、毛、LODはありません。

Macで追加する検査は次の順です。

1. 起動後の床への着地、WASD／矢印／斜め、Shift、Spaceと着地、台へのジャンプ、外周、右ドラッグ、壁際のカメラ、ズーム上下限、Esc再開、フォーカス喪失と復帰、Rを確認。マウスとトラックパッドを別々に試します。
2. Gameビューを1920×1080に固定し、4カメラで同条件の実画像を保存。出力・内部解像度、AA、アップスケーラー、露出を記録します。可変GameビューやEditor UIのスクリーンショットを1080p性能測定と扱いません。
3. `File > Build Profiles`でmacOSの専用プロファイルを作り、`Scene List`に生成したLookDevのみを追加します。既存プロファイルのシーン一覧は書き換えず、Apple Silicon向けの設定を確認して`Artifacts/UnityBuilds/macOS/`へBuildします。Editor外で起動し、入力・衝突・日本語文字を再確認します。署名・公証・公開配布はこの初回ローカルBuildとは別作業です。
4. 高品質のくまと毛を入れた後に、1080p出力／内部解像度固定、30秒ウォームアップ、120秒×3回でCPU／GPU／p50／p95／メモリ圧力を測ります。30fpsは目標で、今の軽い仮モデルの速度を完成画質の性能へ流用しません。

コード検査は以下です。Unity同梱Roslynと実ライブラリを使いますが、これだけでEditor import／Player build／描画の合格にはなりません。

```bash
python3 scripts/check_unity_source.py \
  --editor /Applications/Unity/Hub/Editor/6000.6.5f1/Unity.app \
  --project "$HOME/Documents/Unity/sorajiro-unity"
python3 -m unittest discover -s Tests -p test_unity_link.py -v
python3 scripts/validate_handoff.py
```

次の小タスクは、参照に近い全身くまの編集可能メッシュ・UV・材質分けをBlenderで作り、この窓・床・ラグの場面へ実インポートすることです。その後、小さな短毛パッチでMetal上の見た目・ちらつき・コストを比較します。UnityにUE Groomが付属するとは仮定しません。M2は現在の球のくまでは完了できません。

公式資料：[ローカルパッケージの導入](https://docs.unity3d.com/ja/current/Manual/upm-ui-local.html)、[CharacterController.Move](https://docs.unity3d.com/ja/current/ScriptReference/CharacterController.Move.html)、[HDRP 17.6のLight API](https://docs.unity.cn/Packages/com.unity.render-pipelines.high-definition%4017.6/manual/creating-and-editing-lights-at-runtime.html)。今回のAPI互換性は、導入済み17.6.0ソースと実コンパイルも確認しています。
