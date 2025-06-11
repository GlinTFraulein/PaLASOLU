# PaLASOLU - Particle Live Avatar Setup Optimization & Low-effort Uploader
for VRChat

## Setup Optimization
3クリック程度で、パーティクルライブ向けの設定済みPrefabを生成します。

## Low-effort Uploader
Playable Directorをつけたままでもパーティクルライブ付アバターを正常にアップロードできるようになります。


## 既知の不具合

### Setup Optimization
- 高度な設定を使った場合に正しくアセットが生成されない
  - 高度な設定の使用を控えてください。なんでまだv0.2.0なので……

### Low-effort Uploader
- Timelineが複数のAnimationTrackを使っている場合に正しくアップロードされない
  - 将来的に対応予定
- AudioTrackが無視される
  - 将来的に対応するかも


(自分向け 更新手順)
1. パッケージのコードを更新し、package.jsonのバージョン番号を変更（例：1.0.0→1.1.0）
2. GitHub Actionsでリリースを行う
3. ドラフトからリリースを生やす ここでタグも付ける
4. template-package-listingのリポジトリのワークフローを実行