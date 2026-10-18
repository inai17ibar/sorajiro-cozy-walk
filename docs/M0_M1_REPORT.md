# M0・M1 ソース実装報告（2026-10-02）

M0/T00・T01とM1/T10・T11のUE非依存部分を実装しました。**M0/M1のMac完了ゲートは未達**です。UE版固定、UBT/UHT、Editor資産生成、実操作、Metal描画、画質、性能、パッケージは未検証です。

## 変更と対象要件

| 変更ファイル | 内容 |
|---|---|
| `Unreal/Sorajiro.uproject`、`Unreal/Source/` | Runtime module、Game/Editor targets、Character、Controller、GameMode、診断HUD。GM-01/02/03/08の最小ソースで、正式動作は未検証 |
| `Unreal/Config/` | マップ、GameMode、Enhanced Input、ソフトウェアLumen候補、hardware RTなし、Cook対象。描画設定は未実機検証 |
| `Tools/Unreal/generate_bootstrap.py`と`recipes/bootstrap_v001.json` | 実Editorで床・外周・ブロック・開始点・2灯・3仮材質を作る経路。dry-run/preflight、再実行時の読み取り、編集資産保護 |
| `Tools/Pipeline/recipe_contract.py`、`Tests/` | 本番共用のカメラ数値制約、レシピ・既存資産hash・LFS pointer・symlink・途中失敗等の保護検査 |
| `scripts/check_source.py`、`probe_environment.py`、`mac_ue.py` | 独立チェック、識別番号を除いた環境観測、UBT/UHT・起動・CookのMac実行導線 |
| `project.environment.json`、`validation/` | 観測値、限定されたSOURCE検査結果、資産台帳、機能表、未実行ゲート |
| `docs/MAC_FIRST_BUILD.md`、`LOOKDEV_NEXT_TASK.md`、LookDevレシピ | 再現手順と次の4固定カメラ・くま・窓・床・ラグの試験入力 |
| `.gitignore`、README・既存docs・Mac prompt | キャッシュ／未合意バイナリの除外、実装状態と次の入口 |

開始時に`.uproject`や既存ゲームソースはなく、READMEだけがGit追跡済みでした。既存の未コミット引き継ぎ資料を保持し、その上に実装を追加しました。旧ブラウザゲームのソース一式はこの作業ディレクトリにありませんでした。履歴、参照画像、指示ファイルは変更せず、コミット・マージはしていません。

## 実際の環境と実行結果

[environment_observation.json](../validation/environment_observation.json)に、Darwin arm64、M3 Max 30 GPUコア／36GB、macOS 27.0.1、Xcode 27.0 build 27A266a、Python 3.14.7、Apple clang 21.0.0、Blender 5.2.0 LTSを記録しました。Gitは`/usr/bin/git --version`で2.54.0、最終Python観測のsubprocessでは2.56.0を報告し、呼び出し経路の差を未解決として記録します。Blenderの`--version`は終了0でしたが警告を出しました。モデル生成・bpy実行は行っていません。通常Pythonには`unreal`と`bpy`がありません。UEは指定環境変数・PATH・標準配置先の範囲で未検出です。GPUはOSがMetal対応と報告しただけで、UEの描画経路は試していません。

実行コマンドは次の通りです。結果の詳細・終了コードは [source_checks.json](../validation/source_checks.json)、元ログは`Artifacts/SourceChecks/`にあります。

```bash
python3 scripts/probe_environment.py --write-environment --report validation/environment_observation.json
python3 scripts/check_source.py
bash scripts/cloud_setup.sh
python3 scripts/validate_handoff.py --report validation/handoff_checks.json
git diff --check
```

`check_source.py`内で実行した検査は、引き継ぎ文書／参照整合、Python構文のみ、uproject JSONのみ、Python unittest、純粋C++コンパイルと実行です。C++は`clang++ -std=c++17 -Wall -Wextra -Werror -pedantic -IUnreal/Source/Sorajiro/Public Tests/Pure/camera_policy_test.cpp -o Artifacts/SourceChecks/camera_policy_test`を使います。UCLASSの偽ヘッダーやfake unrealは使用していません。

Pythonは生成保護ロジックを直接検査し、C++はCharacterが呼ぶ`CameraPolicy.h`を直接検査します。これらの合格はUEクラスの構文、UE入力、移動・衝突、生成APIの実行、材質や画質の合格を意味しません。M1診断HUDは英語で、日本語製品UI／フォントは未実装です。

最終チェックはPython unittest **16件**と純粋C++ **14検査**を含みPASSでした。`cloud_setup.sh`の初回実行はPATH上の`/usr/local/bin/git`が`Bad CPU type in executable`で失敗しました。MacでPATHのGitが実行できない場合は`/usr/bin/git`をスクリプト内だけで使うfallbackを追加し、再実行はPASSです。最終のPython子プロセスからのsetupではGit 2.56.0を報告しfallback分岐に入らず、直接のzshから同じPATH候補は引き続きCPU typeエラーでした。OS、PATH、Git本体は変更していません。Git LFSの実行エラーは未解決のままです。

## 未実行とMacの次の操作

| 層 | 状態・理由 |
|---|---|
| SOURCE | 独立チェックPASS。UE C++全体のコンパイルは対象外 |
| BUILD | NOT_RUN：利用できるUEのBuild Tool／ヘッダーなし。Engine lockとXcode互換性も保留 |
| ASSETS | NOT_RUN：実UE Editor未実行。`.uasset/.umap`、rig、Groom、Bindingなし |
| RUNTIME | NOT_RUN：移動、衝突、入力、カメラ、ポーズ、復帰をゲームで未確認 |
| VISUAL | NOT_RUN：参照2枚を閲覧したが、UE実画面・動画・所有者品質承認なし |
| PERFORMANCE | NOT_RUN：FPS、GPU時間、メモリ圧力、スワップは未測定 |
| PACKAGE / WINDOWS | NOT_RUN：Cook・Mac app起動・RTX 3070確認なし |

必要な指定値は実際の`UE_ROOT`、採用UEパッチ・配布元、対応Xcodeの確認です。[Mac手順](MAC_FIRST_BUILD.md)で`preflight → build-editor → editor → Editor生成preflight/apply → build-game → play → package`を行います。手編集を保存してから生成し、成功ログ・receipt・実画面をローカル報告へ残してください。自動のEngine取得・Xcode切替はありません。

## 実資産の状態と最大の見た目課題

参照画像はreference／concept／旧Web実描画です。M1 Characterはソースで定義した毛なし楕円体、床・壁・ブロック・材質はplaceholderレシピで未生成です。新規imported／generated binary／render-verified／approved資産は0です。[asset_manifest.json](../validation/asset_manifest.json)が現状の台帳です。

最高優先の課題は全身のくまらしい造形、布・雲の厚みと短毛がまだないことです。次はM1の実起動を通してから、くま・窓・床・ラグと4カメラに絞った [L_LookDev](LOOKDEV_NEXT_TASK.md) を作ります。星・セーブ・大きな部屋の追加より先に参照との造形と光の差を確認します。
