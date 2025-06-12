# PaLASOLU - Particle Live Avatar Setup Optimization & Low-effort Uploader
for VRChat

## Setup Optimization
3クリック程度で、パーティクルライブ向けの設定済みPrefabを生成します。

## Low-effort Uploader
Playable Directorをつけたままでもパーティクルライブ付アバターを正常にアップロードできるようになります。


## 既知の不具合

### Setup Optimization

### Low-effort Uploader
- Timelineが複数のAnimationTrackを使っている場合に正しくアップロードされない
  - 将来的に対応予定
- AudioTrackが無視される
  - 将来的に対応するかも


(自分向け 更新手順)
1. package.jsonのバージョン番号を変更（例：1.0.0→1.1.0）
2. CHANGELOG.mdを書く
3. GitHub Actionsでリリースを行う
4. ドラフトからリリースを生やす ここでタグも付ける
5. template-package-listingのリポジトリのワークフローを実行