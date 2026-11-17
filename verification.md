# BallCollect — 確認記録（2026-11-17）

環境: Unity 6000.6.4f1 / unity-cli 0.19.1（CLI・Bridge 版一致）。入力は `input_keyboard` によるシミュレート、状態は `eval_csharp` / `get_component_values` で再取得。

## 基本仕様

| 条件 | 操作 | 期待 | 実際 | 根拠 | 判定 |
|------|------|------|------|------|------|
| B1 | Play開始直後 | 0/5、CLEAR非表示、アイテム5個 | pos(0,0.5,−7) count=0 items=11111 CLEAR=False | 状態再取得 / `02_B1_initial_play.png` | 確認済み |
| B2 | D0.4s→A0.4s→S0.2s→W0.4s | +X/−X/−Z/+Z に移動 | x 0→2→0、z −7→−8→−6、解除後 vel=0 | 入力前後のTransform | 確認済み |
| B3 | W0.5s でItem_1通過→S で戻る→W で再通過 | 1/5、二重加算なし | count=1 のまま、Item_1 非表示 | 状態再取得 | 確認済み |
| B4 | W2.6s（4個）→W0.5s（5個目） | 4/5ではCLEARなし、5/5でCLEAR | 4/5 CLEAR=False → 5/5 CLEAR=True、画面にCLEAR | 状態 + `03_B4_four_of_five.png` / `04_B4_clear.png`（目視） | 確認済み（修正後） |
| B5 | 1個回収後 D2.6s で床外へ | 位置・速度・回収数・アイテム・UI初期化 | x=11.96,y=−0.25 で落下中 → pos(0,0.5,−7) vel=0 count=0 items=11111 ui=0/5 | 落下中と後の状態再取得 | 確認済み |
| B6 | 1/5でR / クリア後にR | B1と同じ状態 | どちらも pos(0,0.5,−7) count=0 items=11111 CLEAR=False | 状態 + `05_B6_reset_after_clear.png` | 確認済み（修正後） |

## 追加ルール：30秒の時間制限（実30秒で確認、短縮なし）

| 条件 | 操作 | 期待 | 実際 | 根拠 | 判定 |
|------|------|------|------|------|------|
| A1 | Play後に無操作 | 残り時間が減る | 29.34 → 5秒後 23.70、画面 TIME 24 | 状態 + `06_A1_timer_counting.png` | 確認済み |
| A2 | 無操作で30秒待つ→W1.0s | TIME UP、移動しない | remaining=0 isTimeUp=True、W後も z=−7.00 | 状態 + `07_A2_timeup.png` | 確認済み |
| A3 | W3.1s で全回収→2秒待つ | CLEAR、時計停止 | CLEAR=True、remaining 26.00 のまま | 状態2回 + `08_A3_clear_in_time.png` | 確認済み |
| A4 | クリア後さらに28秒待つ（開始から30秒超） | TIME UPにならない | isTimeUp=False、remaining 26.00 | 状態再取得 | 確認済み |
| A5 | TIME UP後R／1/5でR／1/5で落下 | 時間も30秒へ、Rで再開できる | いずれも remaining≈29（コマンド往復の遅延分）・count=0・items=11111、R後W0.4sで z −7→−5 | 状態再取得 | 確認済み |
| 優先順位 | 5個目と時間切れが同じフレーム | 回収を優先 | EditModeテストで「残り0.01秒で全回収→時間を1秒進める」と CLEAR=True / TIME UP=False | テスト `LastItemAndTimeoutSameFrame_ClearWins` | ロジックは確認済み（実操作での同時発生は未確認） |

### 回帰（追加後）
B2（D0.4sで x 0→2）、B3（1/5）、B4（5/5でCLEAR）、B5（落下で全初期化）、B6（途中・クリア後のR）を追加後に再実行し、いずれも成立。

## EditMode テスト（`Assets/Tests/EditMode/GameManagerTests.cs`）

実際の GameManager / Item / PlayerController を組み立てて実行。`run_tests` → `get_test_status` で完了まで待ち、件数と名前を照合。
結果: **合計8件・成功8・失敗0・スキップ0**（2026-11-17）

| テスト | 対応 | 結果 |
|--------|------|------|
| SameItemTwice_CountsOnce | B3 二重加算なし | Passed |
| FourItems_NotCleared_FiveItems_Cleared | B4 | Passed |
| ResetGame_AfterClear_RestoresInitialState | B5/B6 初期化 | Passed |
| Timer_RunsOut_TimeUpAndStopsMovement | A1/A2 | Passed |
| AfterTimeUp_CollectIsIgnored | A2 | Passed |
| LastItemAndTimeoutSameFrame_ClearWins | 優先順位 | Passed |
| AfterClear_TimerStopsAndNeverTimesUp | A3/A4 | Passed |
| ResetGame_AfterTimeUp_RestoresTimeAndMovement | A5 | Passed |

テストのための変更: スクリプトを `BallCollect.asmdef` に分離、時間処理を `TickTimer(deltaTime)` に切り出し（動作は同じ）。変更後にPlayで回収・R・時間減少を再確認し、Consoleエラー0。

## 途中で見つけて直したもの

1. **Play が進まない**: Editorが非アクティブで `frameCount` が止まっていた → PlayerSettings の Run In Background を有効化し、Unityを前面にして実施。
2. **B6 未成立 → 修正**: Rの入力が届いても `wasPressedThisFrame` が取りこぼし、リセットされなかった → `isPressed` の変化で押下を検出する実装に変更し、再確認で成立。
3. **B4 未成立（画面）→ 修正**: 状態は `isCleared=True` だが画像にCLEARが出ない。ClearText の枠(600×120)が120ptの文字に対して小さく描画されなかった → 枠を900×240、overflow 有効にして再撮影で表示を確認。

## 未確認

- 実キーボードでの手動プレイ（すべてシミュレート入力で確認）
- 画像は AI が画像を開いて目視確認（`analyze_screenshot` は未使用）

## 終了処理
入力解除（pressedKeys=[]、予約解除0）、Stop（isPlaying=false）、シーン保存（isDirty=false）、Consoleエラー0を確認。

## 再テスト（資料を再確認して全項目をやり直し、2026-11-17）

スクリプトのasmdef分離・`TickTimer`切り出し後の状態で、資料の確認項目を新しいPlayセッションで再実行。座標・回収数の直接書き換えはせず、すべて入力→フレーム待ち→再取得で確認。画像は `Evidence/retest/`。

| 資料 | 項目 | 実際 | 判定 |
|------|------|------|------|
| p.5 接続 | ping / doctor | 接続先=UnityPrj、versionCheck=match、doctor OK | 確認済み |
| p.4 B1 | 構成と初期表示 | camera1・light1（重複なし）・Item5・GameManager1、0/5、CLEARなし（R01） | 確認済み |
| p.4 B6 | Input System統一 | 旧 `Input.GetKey` 等の使用なし（grep） | 確認済み |
| p.4 範囲外 | おまけ追加なし | Assets は Materials3・Scene1・Scripts3・Tests のみ、音・外部素材なし | 確認済み |
| p.10-1 移動 | W/S/D/A 各1秒 | z −7→−2→−7.1、x 0→5→0 | 確認済み |
| p.10-2 1個回収 | | 1/5、S で戻って再通過しても1のまま（R02） | 確認済み |
| p.10-3 途中R | | 0/5・items=11111・pos初期・時間≈29 | 確認済み |
| p.10-4 落下 | 1個回収→D2.6s | 途中 x=12.96,y=−1.21 で落下を観測 → 全初期化（R04） | 確認済み |
| p.10-5 全回収 | | 4/5でCLEARなし → 5/5でCLEAR（R05） | 確認済み |
| p.10-6 クリア後R | R→W0.4s | 0/5、z −7→−5 で再開 | 確認済み |
| p.14 時間切れ | 操作せず32秒（実時間） | TIME UP、W/D入力でも位置不変（R07）、R後 W で移動再開 | 確認済み |
| p.14 時間内クリア | W3.1sで全回収→29秒待つ | 時計 26.32 で停止、TIME UPにならない（R08） | 確認済み |
| p.14 途中R・落下 | | 回収数・アイテム・時間すべて初期化（上の p.10-3/4） | 確認済み |
| p.14 EditMode | 全EditModeテスト | 合計62・成功62・失敗0・スキップ0（うちBallCollect 8件を名前で照合、残り54件はunity-cli Bridgeパッケージのもの） | 確認済み |
| p.16 終了処理 | | 予約入力0、isPlaying=false、isDirty=false、Consoleエラー0 | 確認済み |
| p.16 提出物 | 秘密情報 | 該当文字列は ProjectSettings の空の既定欄のみ | 確認済み |

R直後の残り時間が29秒台なのは、R入力から状態取得までのコマンド往復（約1秒）の分。30秒ちょうどへの初期化は EditMode テストで確認。

### 残る未確認
- 実キーボードでの手動プレイ（ペア確認 p.15 で実施）
- 5個目と時間切れの実操作での同時発生（ロジックはテストで確認）
- 提出用コピーの作成（p.16、提出先未定のため未実施）

## 見た目の追加後の確認（2026-11-17）

風景・ロボット・エネルギーキューブ・灯台を追加（ルール変更なし）した後に再実行。画像は `Evidence/visual/`。

| 確認 | 実際 | 判定 |
|------|------|------|
| B1 初期 | 0/5、ゲージ0・画面バッテリー0、灯台消灯（V01） | 確認済み |
| B2 移動 | D +1.95X、A 戻る、S −0.98Z、W +2.05Z。ロボットは進む向きを向く（D中 yaw=90） | 確認済み |
| B3 回収 | 1/5、背中ゲージ1・画面バッテリー1 | 確認済み |
| B5 落下 | x=11.86,y=−0.17 で海へ落下を観測 → 全初期化 | 確認済み |
| B4 クリア | 4/5でゲージ4・灯台消灯（V02）→ 5/5でCLEAR・ゲージ5・アンテナ点灯・灯台点灯（V03、目視） | 確認済み |
| B6 R | 途中・クリア後とも初期状態、ゲージ0・灯台消灯 | 確認済み |
| A1/A2 | 無操作31秒でTIME UP、W入力でも動かない（V04） | 確認済み |
| A3/A4 | 全回収後3秒で残り時間26.06のまま | 確認済み |
| A5 | TIME UP後Rで再開、W0.4sで z −7→−5.05 | 確認済み |
| EditMode | GameManagerTests 8件・成功8・失敗0・スキップ0 | 確認済み |
| 終了処理 | 予約入力0、Stop、isDirty=false、Consoleエラー0 | 確認済み |

### 見つけて直した問題
- **球がz=−1で止まる（未成立→修正）**: 前回「全EditModeテスト」を実行した際、unity-cli Bridge パッケージのテストが開いていたシーンに `TestObject`（BoxCollider付き）を2個残し、その後のシーン保存で `BallCollect.unity` に入っていた。原点の見えない箱に球がぶつかっていた。2個を削除して保存。以後のテストは `filter: GameManagerTests` で自作テストのみ実行し、実行後に Hierarchy が変わらないことを確認。
- 灯台が画面上端で切れていた → 手前（15, 11）へ移動。
- 背中のゲージが小さく見えにくい → 背負いバッテリーとして大きくし、画面左上にも同じバッテリー表示を追加。

## 「手動で動かない」への対応（2026-11-17）

- 原因: Input System の既定 `PointersAndKeyboardsRespectGameViewFocus` により、Play中でもGame Viewにフォーカスが無いと実キーボード入力がゲームに届かない（シミュレート入力はこの制限を受けないため、これまでの確認では表に出なかった）。
- 対応: `Assets/Settings/InputSystem.inputsettings.asset` を作成しプロジェクトの Input System 設定に登録、`AllDeviceInputAlwaysGoesToGameView` に変更。画面下に「WASD：移動　R：やり直し」を表示。
- 確認: 設定値が反映されていること、Inspector にフォーカスがある状態で W 入力→移動・回収できること（シミュレート入力）、画面に操作説明が出ること（V05）。
- 未確認: 実キーボードでの手動操作（利用者の操作で確認）
- **追記・本当の原因**: 上記設定後も実キーボードで動かなかった。Input System のデバイス一覧に unity-cli の仮想キーボード `UnityCliVirtualKeyboard` しかなく、実機の Keyboard / Mouse が消えていた。以前「全EditModeテスト」を実行した際、Bridge パッケージの `InputSystemHandlerTests` が SetUp で全デバイスを削除し、戻さないため。Unity Editor を再起動して実機 Keyboard / Mouse（native=True）が戻ったことを確認。以後テストは `GameManagerTests` に絞って実行する。

## ホラー版の確認（2026-11-17）

亡霊（追跡）・夜の見た目・魂の灯の配置変更を追加。画像は `Evidence/horror/`。

| 確認 | 操作 | 実際 | 判定 |
|------|------|------|------|
| 見た目 | Play開始 | 夜・霧、懐中電灯、光る魂の灯、亡霊、枯れ木と墓の小島（H01、目視） | 確認済み |
| H1 追跡 | 無操作 | 亡霊が (0,9) から球へ z 6.1→1.2→−3.4 と接近、速さ2.0 | 確認済み |
| H1 加速 | 1個回収 | 速さ 2.0→2.5 | 確認済み |
| H2 つかまる | 無操作で接近させる→W1.0s | 距離0.90で caught=True、「つかまった…」表示、W でも動かず、時間22.5で停止（H02） | 確認済み |
| H4 R | つかまった後にR | 亡霊 (0,9) へ戻り、時間30から再開、W で移動 | 確認済み |
| H4 落下 | S1.0sで海へ | 球 z=−11.9 まで落下→球・亡霊・時間が初期化 | 確認済み |
| H3 クリア | 全回収ルートを予約入力 | 5/5でCLEAR、亡霊が消え、灯台点灯、時間15.6で停止（H03）。R で亡霊が戻る | 確認済み（**亡霊の移動をこのPlay中だけ0にして確認**。シーンの値 2 / 0.5 は変更なし） |
| EditMode | GameManagerTests | 13件・成功13・失敗0・スキップ0（亡霊5件を追加） | 確認済み |
| 終了処理 | | Stop、isDirty=false、Consoleエラー0、Hierarchy に余計な物なし | 確認済み |

### 気づいたこと
- AI の操作はコマンド1回ごとに約1秒の遅れがあり、亡霊が動いている状態では全回収できなかった（中央の魂の灯付近で3回つかまった）。人が実時間で操作する場合の難しさは未確認。難しすぎる場合は亡霊の初速（2 m/s）や開始位置を調整する。
- Play開始直後に送った入力は失われることがある（開始後1秒ほど待つと届く）。
