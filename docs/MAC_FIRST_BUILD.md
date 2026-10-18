# M0・M1：Macでの初回ビルドと生成

この変更は未ビルドのC++骨格です。UBT/UHT、Editor生成、Metal、操作、画質、性能、Cook、パッケージはすべて実機確認待ちです。通常Pythonの`unreal`モジュールは不要で、Editor内蔵Pythonを使います。

2026-10-02の実環境は [環境記録](../validation/environment_observation.json) にあります。M3 Max 30 GPUコア／36GB、macOS 27.0.1、Xcode 27.0、Blender 5.2.0 LTSを観測しました。UEはPATH、`UE_ROOT`、標準配置先の範囲では見つかりませんでした。別の場所に導入済みならそのルートを指定してください。GPUのOS表示はUEレンダリング試験ではありません。

候補はUE 5.8で、パッチ版は未固定です。[EpicのMac要件](https://dev.epicgames.com/documentation/unreal-engine/macos-development-requirements-for-unreal-engine)にある推奨Xcode 26.1.1と、観測した27.0は異なります。27.0の互換性は未確認です。導入UEのパッチ版の要件と照合してから進め、Xcode選択・OS・Engineを自動変更しないでください。空き容量は観測時約14GiBで、EngineやDDC用容量の確保も未完了です。ここではダウンロードを実行しません。

## 1. Engineと独立チェック

リポジトリルートのターミナルで実行します。`UE_ROOT`には`Engine/`ディレクトリを含む実際のインストールルートを指定します。パスはGitへ保存しません。

```bash
export UE_ROOT="<実際のUEインストールルート>"
python3 scripts/probe_environment.py --ue-root "$UE_ROOT" --write-environment --report Artifacts/environment.local.json
python3 scripts/mac_ue.py preflight
python3 scripts/check_source.py
python3 scripts/mac_ue.py build-editor --print-only
python3 scripts/mac_ue.py build-editor
```

`preflight`はファイル・パス検査、`--print-only`は引数表示のみです。正式ビルドは`Build.sh SorajiroEditor Mac Development -Project=<絶対uproject> -architecture=arm64 -WaitMutex`を実行します。`.uproject`のEngineAssociationは意図的に空で、指定Engineを直接使います。プロジェクトの自動変換は不要です。`.Target.cs`のV5は候補用の互換設定であり、採用パッチのUBTで検証してください。

このMacではPATHのGitに実行エラーが観測されたため、`cloud_setup.sh`と観測スクリプトはMacでそのGitが実行できない場合に`/usr/bin/git`へその実行内だけでfallbackします。差分確認をターミナルで行う際も必要なら`/usr/bin/git diff`を使用してください。LFSは別途未解決です。

ビルドの標準出力、終了コード、引数は`Artifacts/Logs/`に残ります。失敗時は最初の根本エラーを報告し、以降の生成・起動を成功扱いにしません。実パッチ・Build ID・配布元とツールチェーンを確認し、初回ビルド後に`project.environment.json`の`version_locked`をレビューして固定します。観測スクリプトだけではロックしません。

## 2. 本物のEditorでマップを生成

```bash
python3 scripts/mac_ue.py editor
```

初回は未生成のStartupMapを避けてEngineのEntryマップで開きます。メインのLevel Editorビューポートを表示し、初期ロードが終わるまで待ちます。Python Editor Script PluginとEditor Scripting UtilitiesはEditor targetにだけ有効です。モジュールがロードできない場合は手順1に戻ります。

既存の作業マップ・コンテンツに未保存変更があれば所有者が保存します。生成スクリプトは未保存変更があると停止し、勝手に保存・破棄しません。Tools → Execute Python Scriptからリポジトリ内の`Tools/Unreal/generate_bootstrap.py`を実行します。初期モードは`preflight`で、資産を書きません。

Output Logの入力モードをPythonに切り替え、次を実行します。

```python
import os
os.environ["SORAJIRO_GENERATE_MODE"] = "apply"
```

再び同じPythonスクリプトをTools → Execute Python Scriptで実行します。成功後、モードを戻します。

```python
os.environ["SORAJIRO_GENERATE_MODE"] = "preflight"
```

生成予定は`/Game/Generated/M1/Bootstrap_v001/`内の`L_Bootstrap`、`M_PreviewBlue`、`M_PreviewFloor`、`M_PreviewWall`です。床600×600cm、外周、12cmのジャンプブロック、開始点、動的ライト2灯を配置します。材質は定数色と粗さだけの仮材質です。GameModeはConfigからC++を指定し、Enhanced Inputのアクション・マッピングはC++の実行時オブジェクトなので入力用`.uasset`は不要です。

出力、レシピhash、Engine版、seed、生成資産hashは`Artifacts/AssetGeneration/Bootstrap_v001.receipt.json`に残ります。map保存・再ロード、生成Actor ID集合、materialロード、本物のpackage存在を確認してからreceiptを書きます。これだけでShader・Cook・描画確認済みにはしません。Output Logの`SORAJIRO`行とエラーを`Artifacts/Logs/`に保存してください。

同じレシピ・Engine・ファイルhashの再実行は読み取りのみです。編集・追加・削除・Engine変更・receipt欠損・途中失敗を検出すると停止します。自動削除・再生成・強制上書きのオプションはありません。手編集は`/Game/Sorajiro/`にコピーして行います。改訂生成には新しいrevision名をレシピへ指定し、DefaultEngine.iniのマップ、DefaultGame.iniの材質とCook対象も同じPRで更新します。途中生成が残っていても消さず、次revisionを使ってください。receiptは資産保護のためローカルで保持・バックアップします。

## 3. 操作と衝突

Editorで`L_Bootstrap`を開いてPlayし、さらに次でStandaloneも試します。

```bash
python3 scripts/mac_ue.py build-game
python3 scripts/mac_ue.py play
```

ゲームウィンドウをクリックして入力を取得します。WASD／矢印移動、Shift走行、Spaceジャンプ、右ボタン押下中のドラッグで旋回、ホイールでズーム、Rで開始点に復帰、Escでポーズ／再開です。PIEではEditorのEsc終了ショートカットが優先されることがあるため、ポーズはStandaloneで確認します。トラックパッドでは右クリック保持とスクロールを別途確認します。

次の結果を [ローカル報告テンプレート](../templates/LOCAL_REPORT.md) に残します。

- 30／60fps制限のそれぞれで同じ経過時間に移動距離がほぼ一致する。斜め移動が速くならず、W+S／A+Dが打ち消される。
- ジャンプ・床・12cmブロック・外周衝突。空中連続ジャンプができない。Rの復帰で床へ埋まらない。
- 壁際のSpringArm衝突、上下角・距離上限。ドラッグしていないと旋回しない。
- Shift／Space／右ボタンを押したままEsc→キーを離す→Escで復帰し、走行・ジャンプ・旋回が残留しない。ウィンドウのフォーカスを外す操作も確認する。
- `M1 PLACEHOLDER | NO FUR / NO BEAR RIG`の診断表示。実モデルは全高40cm相当の楕円体で、くま・毛・歩行リグではない。

移動・衝突は標準CharacterMovement、カメラは標準SpringArmです。独立C++テストはカメラ数値制約だけを検証し、これらUE動作の代わりにはなりません。日本語の製品UI・フォントはM4で実装するため、M1診断HUDは英語です。

## 4. 最小Macパッケージ

操作できる段階でEditorを終了し、早めにCookと起動経路を試します。

```bash
python3 scripts/mac_ue.py package --print-only
python3 scripts/mac_ue.py package
```

RunUAT BuildCookRunによるDevelopmentのMac build/cook/stage/pak/archiveです。成果物は`Artifacts/Package/`、ログは`Artifacts/Logs/`へ出します。実際に生成された`.app`をFinderから起動し、Editorを閉じた状態の起動・同じ操作を確認します。署名・公証・一般配布はまだ対象外です。パッケージ終了コード0でも、起動・毛・画質・性能の合格とは区別します。

## 5. バイナリ保管と次の画質タスク

現環境の`git lfs version`は`Bad CPU type in executable`で失敗します。LFSは有効化していません。`.uasset/.umap`等を当面Gitから除外し、recipeをソースとして共有します。手編集資産を追加する前に、LFSの修復と実体アップロード確認、または別ストレージ運用を合意します。reference画像は通常Gitファイルのままです。

最重要の未実装は参照に近い全身シルエット・布・アップリケ・短毛です。M1が起動したら星・セーブを増やす前に [次のL_LookDevタスク](LOOKDEV_NEXT_TASK.md) に進みます。M0・M1のMacゲートは、ビルド・資産・操作の証拠が揃うまで未完了です。
