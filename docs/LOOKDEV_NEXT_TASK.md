# M2の次の小さなタスク：くま・窓・床・ラグ

2026-10-12：以下は保存したUE向け計画です。現在のUnity M2のモデル・実画像と、次の毛／接触影の試験は[UNITY_M2_REPORT.md](UNITY_M2_REPORT.md)を参照してください。

対象はCH-01/02/03の造形・布の基礎、ENV-02/03、QA-Vの4固定カメラです。M1のMac初回起動を確認した後、実際の`L_LookDev`を作ります。今回はその入力レシピを [lookdev_plan_v001.json](../Tools/Unreal/recipes/lookdev_plan_v001.json) に準備した段階で、マップ・全身リグ・毛は未生成です。

画像では頭と耳の大きさ、短い手足、白い口元、小さい黒い樹脂の目・鼻、青白の雲の薄い厚みが識別を左右します。毛は長い獣毛ではなく均一すぎない短いフリースです。部屋は低いゲームカメラと右奥の大きな窓光、足元の柔らかな影、木とラグの暖かさを先に合わせます。

## 制作する範囲

1. `SourceArt/Bear/`に編集可能な全身40cm試作、UV、root・胴・頭・左右腕脚のリグ、部位別材質を作る。毛なしでも正面・横・背面で頭胴比を確認できること。目鼻、青布、白アップリケ、糸を分ける。写真を照明ごとテクスチャ投影しない。
2. `L_LookDev`にはそのくま1体、窓、木の床、厚みのあるラグだけを置く。本は主要4要素の比較後の小さな追加とする。Generatedの新revisionから手編集用`/Game/Sorajiro/LookDev/`へ分ける。
3. レシピの4カメラをCameraActorとして作り、位置とlook-atから回転を計算する。くまは+Xを向く。S01顔、S02後方ゲーム視点、S03斜め全身、S04足元。寸法・FOVは試験値なので構図確認後に最終値を記録する。
4. 暖かな主光源、ソフトウェアLumen候補、固定露出から調整する。DOF・Motion Blurは切る。床板の面取り、ラグの厚みと接触、白と青の色分離を先に見る。M1の定数色材質は最終床・布として再利用しない。
5. 毛は1〜4mmを試験範囲とし、まず小さなパッチでGroom Import／root UV／Binding／スキン追従を確認する。Cards/Meshも同じ姿勢・光・カメラで比較する。方式採用はM3の実描画と実測で判断する。

UEはcm／Z-up／+X前方、Blenderはmetric／unit scale 1.0で40cmを0.4mとして作成し、-Y前方／Z-upからFBXのForward=-Y、Up=Z、Apply Unit Scaleあり、Add Leaf Bonesなしを試験設定とします。インポーターの単位変換を含め、UE内の高さ40cmとroot前方を実際に計測し、二重の100倍補正をしません。この経路・軸・rig・Groom exporterはまだ検証していません。選択したBlender／UE版で最初に小さなメッシュの往復試験を行います。

## 実画面の証拠

Level EditorのCameraActorを選択しPilot、Game Viewを有効にし、同じ出力1920×1080・内部解像度・露出・姿勢でUEの実画面を取得します。実際のパッチで利用できるHigh Resolution ScreenshotまたはStandaloneの`HighResShot 1920x1080`を使います。EditorビューポートとStandaloneの解像度・描画経路差を記録し、画像保存を見て成功と判定します。これはまだ自動キャプチャの実装ではありません。

保存先を`Artifacts/Visual/current/`とし、`S01_Portrait_<commit>_<profile>.png`等4枚と、Engine版、Mac情報、実カメラtransform、実EV100、姿勢、出力／内部解像度、AA、光、毛方式を同名JSONへ記録します。最初のUE画像だけをbaselineとして別保存します。conceptは`reference/target_room.png`、旧Web実描画はlegacyとして分けます。オフラインレンダーは`Artifacts/Visual/offline/`に分け、実時間結果と混ぜません。

待機5秒→歩行10秒→停止と旋回10秒→近遠移動5秒以上の実行動画も取得します。輪郭、ちらつき、接地、残像を確認します。静止画だけで時間方向の品質合格を出しません。

性能は別ランです。[検証契約](VERIFICATION.md)の30秒ウォームアップ、120秒×3回、録画なし、1080p出力、内部解像度固定、VSync/FPS上限条件を記録します。Groom・Cards・影の表示と負荷が変わることを確認してから [feature_matrix.json](../validation/feature_matrix.json) を実測で更新します。現時点の性能値はすべて未測定です。

このタスクの終了条件は、実メッシュ・実マップのロードと4枚のUE画像、参照との差を顔・比率・布・接地ごとに報告することです。毛が試作なら明記し、所有者の目視承認まで品質承認にはしません。星、SaveGame、大きな部屋、フォトUIは今回の範囲に含めません。
