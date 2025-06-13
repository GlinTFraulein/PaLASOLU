# Changelog
PaLASOLUの主な変更点をこのファイルで記録しています。

この形式は [Keep a Changelog](https://keepachangelog.com/ja/1.0.0/) に基づいており、
このプロジェクトは [Semantic Versioning](https://semver.org/lang/ja/) に従っています。

## [Unreleased]

### 追加
- LowEffortUploaderの複数AnimationTrack対応
- LowEffortUploaderのAudioTrack完全対応 (Timeline上のオフセット、1AudioTrack上に複数AudioClipがある場合に対応する)
- UIをいい感じにしたり、ロゴデザインを作ったりする

### 変更

### 修正

### 削除

### 非推奨

### 脆弱性

## [0.3.0] - [2025-06-13]
### 追加
- Low-effort UploaderがAudioTrackのアップロードに暫定対応
  - AudioTrackが単一のAudioを持ち、それがTimeline上で「0秒」の位置に配置されている場合にのみ正しく動作します。正しく動作しない場合、警告を出力します。
  - AudioTrackが複数あっても、正しく動作します。(ただし、全てTimeline上で「0秒」の位置に配置されている場合にのみ正しく動作するため、音が重なると思います……。)
  - PaLASOLU Low-effort Uploader コンポーネントの高度な設定から「Generate Audio Object」のチェックを外すことで、AudioTrackのアップロードをしないようにできます。

## [0.2.1] - [2025-06-12]
### 変更
- Low-effort Uploaderのファイル名/クラス名変更(PaLASOLU_LoweffortUploader.cs => LowEffortUploader.cs)
- Low-effort UploaderのInspector上の表示名を改善
- Low-effort Uploaderは、アタッチしたタイミングで同GameObjectのPlayableDirectorを取得するようになりました

### 修正
- Setup OptimizationのSelect Folder Directoryが動かないバグを修正

## [0.2.0] - [2025-06-11]
### 追加
- PaLASOLU_LoweffortUploader Componentを追加
  - ただしこれは未完成で、不安定な可能性があります！
  - 現在は、Timelineが1つのAnimationTrackを持つ場合にのみ対応しています。

## [0.1.1] - [2025-06-10]
## 追加
- vpm対応

### 修正
- package.jsonを修正
- release.ymlを修正

## [0.1.0] - [2025-06-10]
### 追加
- Tools/PaLASOLU/ParticleLive Setup(SetupOptimization)を追加