# Changelog
PaLASOLUの主な変更点をこのファイルで記録しています。

この形式は [Keep a Changelog](https://keepachangelog.com/ja/1.0.0/) に基づいており、
このプロジェクトは [Semantic Versioning](https://semver.org/lang/ja/) に従っています。

## [Unreleased & Planned]

### 追加
- UIをいい感じにしたり、ロゴデザインを作ったりする
- TimelineExtension
  - BPM基準でタイムラインを触れるようにする
  - GameObjectの名称変更にAnimationが追従するようにする
  - その他、Timelineを使いやすくする様々な機能

### 変更

### 修正
- Low-effort UploaderがTimeline上のAudioClipの数だけLayerを生成する(優先度低)

### 削除

### 非推奨

### 脆弱性

## [0.5.0] - [2025-06-14]
### 追加
- Low-effort Uploaderの複数AnimationTrack対応
  - AnimationTrackに直接キーを打った場合にのみ対応しています(AnimationClipが配置された状況には未対応)

### 修正
- SetupOptimizationは"(PaLASOLU)"フォルダ歩生成しないように修正(使わないことが確定したので)
- LICENCEファイルがなぜか.mdだったので修正

## [0.4.1] - [2025-06-14]
### 修正
- Low-effort UploaderのNullチェックを強化し、PlayableDirectorもしくはTimelineAssetがNullだった場合に処理をスキップするように(PlayableDirectorが削除されないので、アップロードに失敗します)

## [0.4.0] - [2025-06-14]
### 追加
- Low-Effort Uploaderは、AudioTrackを完全に正しくアップロードするようになりました。
  - 現状は、AudioClipの数だけ新規にAnimatorLayerを生成します。これはパーティクルライブ用のAnimatorに対して生やしているのでアバター本体への影響はありませんが、レイヤーが増えまくるのは気持ち悪くはあるので、いつか修正するかもしれません。(修正する場合、オーディオ用の1Layerが全てのAudioClipに対応したAnimationを持つようにする予定です)

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