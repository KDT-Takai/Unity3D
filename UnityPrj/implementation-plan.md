# BallCollect — 作業順

各段階で実物（Hierarchy・状態・画面）を確認してから次へ進む。

## 段階1 シーン
- `Assets/Scenes/BallCollect.unity` を作成
- 既存の Main Camera / Directional Light を再利用（重複させない）
- Floor・Player・Item_1〜5・マテリアル3色を配置、カメラを固定視点へ
- 確認: Hierarchy 再取得、Game View 画像、保存されたシーン

## 段階2 移動
- `PlayerController.cs`（Input System の `Keyboard.current` でWASD）
- Player へ追加、Rigidbody 設定
- 確認: compile error 0 → 保存 → Play → `isPlaying` → W 1秒 → 前後の座標で +Z を確認 → Stop

## 段階3 ルール
- `GameManager.cs`（回収数、CLEAR、落下とRの初期化）、`Item.cs`（二重加算防止）
- Canvas・CountText・ClearText を作成し GameManager へ参照を設定
- 確認: 1個回収で 1/5、全回収で CLEAR、途中R・落下で全初期化

## 検証
- B1〜B6 をAIの入力で確認し `verification.md` に記録（成立・未成立・未確認）
- 根拠画像を `Evidence/` に2枚以上

## 追加機能（承認済み）30秒の時間制限 A1〜A5
- GameManager に `timeLimit` / `remainingTime` / `isTimeUp`、時間切れで `PlayerController.canMove=false`
- `TimerText`・`TimeUpText` を作成し参照を設定
- 影響: B4（クリアで時計停止・回収優先）、B5・B6（時間も初期化）
- 確認: 実30秒で時間切れ、時間内クリア、途中R・落下、B2〜B6 の回帰
