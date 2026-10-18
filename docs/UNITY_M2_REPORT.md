# Unity M2：全身モデルと一場面の初回実描画

2026-10-12。所有者のM1動作報告を受け、M2の造形・取り込み・比較撮影まで進めました。**M2の画質承認は未完了**です。全身の短毛、歩行アニメーション、LOD、Player build、性能測定は残っています。M1／SampleSceneと過去の生成リビジョンを保持しました。

実環境はMacBook Pro M3 Max（30 GPU cores／36GB）、Unity 6000.6.5f1（6.6）、HDRP 17.6.0、Metal、Blender 5.2.0 LTSです。UEの実行報告ではありません。

追加したソースは`Tools/Blender/bear_m2.py`、`Tools/Pipeline/art_contract.py`、`Tests/test_art_contract.py`、Unityの`M2LookDevBuilder.cs`／`M2Verification.cs`、`scripts/collect_unity_m2_evidence.py`です。既存の生成保護にレシピ引数を追加し、物理検査・実行状態の記録をM2にも対応させました。Runtimeの変更はモデル段階を示す診断表示です。

Blenderで頭・胴・短い手足・耳・尻尾、白い口元と雲・点、光沢のある目鼻、口と縫い目を作りました。UV、部位別材質、9本のGeneric用ボーンと試作ウェイトがあります。編集用blendでは部品を保持し、FBXでは本体を統合して、短毛パッチと合わせて2個のSkinnedMeshRendererにしました。写真を照明ごと貼ったモデルではありません。参考画像の再配布権は未確認で、新規メッシュ・手続きテクスチャの来歴をreceiptに残しています。

原本は`Artifacts/SourceArt/Bear/m2_v002/`、Unity実アセットはプロジェクトの`Assets/Sorajiro/Generated/M2_v003/`です。本物のBlender保存・FBX export・Unity importで作成しました。大きなバイナリはローカルのみに保存しGitでは除外しています。別Macへの配布／LFS運用は未設定です。ソースから再生成するか、合意したストレージへ原本を保存してください。

Blenderはm／Z-up／-Y正面、FBXはForward=-Z／Up=Y／Apply Unit Scaleあり／Leaf Bonesなしです。Unityで約0.4075mの高さ（renderer bounds）、マーカーの+Z正面、2個のSkinnedMeshRendererを実検査しました。100倍補正は追加していません。BakeMeshの最下点をラグ上面へ合わせていますが、足の接触影の見え方は改善が必要です。

シーンには窓開口と枠、木の床板、厚みのあるラグ、操作用の台、固定4カメラと毛パッチの診断カメラがあります。ラグは微細凹凸のみで毛はありません。光はDirectional、Area／Pointの補助光、Gradient Skyです。補助光はGIのベイク結果ではありません。固定EV8、DOF／Motion Blur／Bloomなし、雲を明示的に無効化し、Volumeの9部品を実サブアセットとして保存・再読込検査しました。

| 範囲 | 実行した検査・結果 | 証拠 |
|---|---|---|
| Blender | background生成・export、exit 0。通常dry-runはREUSEで無変更 | `Artifacts/SourceArt/Bear/m2_v002/receipt.json`、実blend／FBX／PNG |
| ソース | Unity同梱Roslyn＋実DLLでRuntime／Editorコンパイル成功、実MotionPolicyの9 assertions成功 | `validation/unity_source_checks.json`、`Artifacts/UnitySourceChecks/` |
| Editor | Assets > Refreshで実C#コンパイル、FBX import、シーン生成・保存・再読込 | 実プロジェクト`Library/ScriptAssemblies/Sorajiro.*.dll`、`Logs/Editor.log` |
| スキン・保存 | 実FBXのUV／HDRP材質／Volume保存と有効値。頭・腕・脚を15度動かしBakeMeshの変位を確認、頭で足は動かない | `Artifacts/UnityEditor/m2-skin-test.txt` |
| 物理 | Playで本番Step＋CharacterControllerの接地、歩行・走行・斜め、ジャンプ・着地・空中再ジャンプ拒否、外周衝突、復帰がPASS | `Artifacts/UnityEditor/m2-motor-smoke.txt`。歩行0.23333m、ジャンプ頂点0.12635m |
| 生成保護 | 実Unityの6検査PASS。Pythonの21検査PASS（旧UEレシピ検査も含む） | `Artifacts/UnityEditor/guard-tests.txt`、`Tests/` |
| 描画 | HDRP StandardRequestで4固定カメラ＋S02パッチOFF＋S05パッチON/OFF、計7枚の全PNGが1920×1080 | `Artifacts/Visual/current/20261012T093357045/` |
| Play描画 | 起動・接地・日本語表示を実画像で確認。可変GameビューPNGは1206×1197、Screen報告は1210×1291。1080p証拠とは分ける | `Artifacts/UnityEditor/current-editor-639273948157970440.png`、`runtime-639273948157960040.json` |
| 文書 | handoff検査PASS。ゲームの検査ではない | `validation/handoff_checks.json` |

固定画像はTAA、1920×1080 render target、dynamic scale false、カメラの動的解像度とDLSS／FSR2無効です。S05のみ細線を見分けるためAAなしです。24 render requestsで履歴を暖め、各画像にカメラ・GPU・設定JSONを付けています。[unity_m2_checks.json](../validation/unity_m2_checks.json)は既存証拠の整合性集計で、エンジン実行の代替ではありません。

EditorのコンパイルとM2実行メッセージだけを抽出したログは`Artifacts/UnityEditor/m2-engine-evidence.log`です。最後のCreate再実行はREUSEで書き換えなしでした。Runtime／Editor DLLはそれぞれ対応するC#ソースより新しいことも確認しました。

最初のM2_v001では凹凸が大きすぎ、Volume部品が保存されていませんでした。M2_v002で保存と接地位置を修正し、M2_v003で継承された雲を抑制し空の明るさを調整しました。最初の2回の画像は比較用に保持し、最終状態の証拠には使いません。初回画像の設定JSONはレシピ意図であり、有効Volumeを保証しません。旧M1にもVolume部品未保存があり、その「EV9／効果無効化」は再読込後の検証済み設定とは扱えません。M1を上書きせずM2で修正しました。

参照との差は、全身の滑らかな輪郭、頭と耳の比率、雲の折れと厚み、単純な縫い目です。ラグの毛と床板の面取りは未実装、接触影が弱く、室内は目標より白く均一です。毛は後頭部だけの3600本の交差リボン（14400 quads、0.8〜1.6mm）です。拡大ON/OFFでは描画差を確認できますが、通常距離の毛並みとしては弱く、細線のエイリアシングもあります。**参照品質や全身ファーの完成を示す画像ではありません。**

未実施なのはPlayer build／macOSアプリ起動／Windows、歩行アニメーション、30秒以上の動画と毛のちらつき、CPU/GPU時間・メモリ圧力、LOD／画質切替、所有者の画質承認です。30fpsは未測定です。同期物理スモークは実キーボードや長時間フレームループの代用ではありません。

次の小タスクは、**頭の一部で毛の密度・太さとAAを比較し、接触影を改善すること**です。全身へ増やす前に、近景／ゲーム距離／30秒の動きで安定性とGPU時間を測ります。その後、雲の表面と輪郭、歩行用ウェイトとアニメーションを整えます。

再実行はリポジトリのルートから行います。

```bash
python3 Tools/Blender/bear_m2.py
'/Applications/Blender 5.2.app/Contents/MacOS/Blender' --background --factory-startup --python Tools/Blender/bear_m2.py -- --write
python3 -m unittest discover -s Tests -p 'test_*.py' -v
python3 scripts/check_unity_source.py --editor /Applications/Unity/Hub/Editor/6000.6.5f1/Unity.app --project "$HOME/Documents/Unity/sorajiro-unity"
python3 scripts/collect_unity_m2_evidence.py --project "$HOME/Documents/Unity/sorajiro-unity" --capture "$PWD/Artifacts/Visual/current/20261012T093357045"
python3 scripts/validate_handoff.py
```

最初のBlender起動はsandbox内でexit 139となり、承認後のローカル実行で正常終了しました。生成済みの同一revisionは再利用し、原本・生成物の変更、部分生成、無関係なファイル、symlinkを拒否します。手編集は別フォルダーへコピーしてください。改修時はBlenderのOUTPUT、UnityのOutput／Recipeを新revisionへ進め、既存内容を消して再実行しないでください。receiptのrenderVerified=falseは生成時点の保守的な初期値で、描画範囲は本報告と画像で管理します。

同じMacではUnityの`Sorajiro > M2 > 3. Open bear lookdev`→▶→Gameビューをクリックで操作できます。再生成時はPlayを止め、所有者の編集を自分で保存し、`M2 > 1. Preflight`、`2. Create`、`3. Open`、`5. Test imported skin and UV`、`4. Capture`の順です。4は診断用2枚を含む7枚を保存します。

別MacではUnity 6000.6.5f1／HDRP 17.6.0を用意し、[接続手順](UNITY_START.md)のproject引数とBlender実パスを置き換えます。Player検査は専用macOS Build ProfileのScene Listに`Assets/Sorajiro/Generated/M2_v003/L_LookDev.unity`を追加し、Apple Silicon設定で`Artifacts/UnityBuilds/macOS/`へビルドして外部起動します。これらのPlayer手順は未実行です。

APIは導入済みHDRP 17.6.0ソースと実コンパイルを優先しました。参考：[StandardRequest](https://docs.unity.cn/2023.3/Documentation/ScriptReference/Rendering.RenderPipeline.StandardRequest.html)、[Blender FBX export API](https://docs.blender.org/api/5.0/bpy.ops.export_scene.html)。機能の記載はこのMacの性能結果を示しません。
