# Changelog
PaLASOLUの主な変更点をこのファイルで記録しています。

この形式は [Keep a Changelog](https://keepachangelog.com/ja/1.0.0/) に基づいており、
このプロジェクトは [Semantic Versioning](https://semver.org/lang/ja/) に従っています。

## [Unreleased & Planned]

### 追加
- Animation, Audio以外のトラックに対応
- AudioTrackの途中再生に対応 (できるのか？)
- TimelineExtension
  - BPM基準でタイムラインを触れるようにする
  - GameObjectの名称変更にAnimationが追従するようにする
  - 「0打ち」をしなくていいようにする
  - その他、Timelineを使いやすくする様々な機能
- Timelineチュートリアル
- サンプルPrefabを置けるようにする
- MeshCollider削除
- Export Prefab without Timeline
- AudioTrackの音量がアップロードに反映されるか選べるようにする設定

### 変更
- AudioClip.Create()を用いて、元のAudioClipのLoadInBackgroundを変更しないようにする
- ResolvingフェーズではTimelineの持つAnimator Componentへの参照が正しく取れるらしいのでそれを使うようにする

### 修正

### 削除

### 非推奨

### 脆弱性

## [1.0.5] - [2025-07-01]
### 修正
- 一部環境で文字化けが発生していた問題の修正 #1
- Setup OptimizationがReload Domain後にエラーを吐いていたのを修正 #2
- その他、軽微な修正

## [1.0.4] - [2025-06-26]
### 修正
- Low-effort Uploaderのnullチェックをもうちょっと強化
- その他リファクタ等、内部的な修正

## [1.0.3] - [2025-06-24]
### 修正
- Low-effort Uploaderが英語以外の環境で作られたTimelineでも正しく動作するように
- Low-effort Uploaderは複数存在するAudioClipに対しても1Clip、1Layerのみを生成するように

## [1.0.2] - [2025-06-24]
### 修正
- Low-effort Uploader CoreProcessのNullチェックを強化
- 日本語でTimelineを作った場合にTimelineAssetのAnimationClipの名称が"Recorded"でない問題への暫定対応

## [1.0.1] - [2025-06-21]
### 修正
- Low-effort Uploaderがアバター内にない場合にエラーが出る不具合を修正

## [1.0.0] - [2025-06-19]
### 重大
- Major Versionを1に上げ、正式リリースを発表

### 追加
- Setup Optimization, Low-effort Uploaderにバナーを追加

### 修正
- Setup OptimizationのUI表示を改善
- package.jsonで指定するVRCSDKのバージョンを3.8.1以上に修正 (3.8.0はBuild & Publishが押せなかったため)

## [0.7.0] - [2025-06-18]
### 追加
- Low-effort Uploaderにロゴアイコンを追加

## [0.6.1] - [2025-06-17]
### 変更
- Setup Optimizationは、Timelineウィンドウを表示するように変更
- TimelineウィンドウをLockするよう促すウィンドウが表示されるように変更

### 追加(Internal)
- LogMessageSimplifierを追加
  - これは将来の拡張のために追加されており、現バージョンでは使用されていません！

## [0.6.0] - [2025-06-15]
### 追加
- Low-effort Uploaderに、AudioClipのLoad in Backgroundを修正する機能を追加
- Low-effort Uploaderの将来的な拡張のために、AnimationEditExtensionを追加

### 変更
- Setup OptimizationのPrefabの内容を変更し、ワールド固定ボタンとギミック起動ボタンを分離
- Low-effort Uploaderの中身を、AnimationEditExtensionに対応するように変更

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