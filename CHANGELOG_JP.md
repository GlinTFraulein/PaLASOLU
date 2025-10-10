# Changelog
PaLASOLUの主な変更点をこのファイルで記録しています。

この形式は [Keep a Changelog](https://keepachangelog.com/ja/1.0.0/) に基づいており、
このプロジェクトは [Semantic Versioning](https://semver.org/lang/ja/) に従っています。

## [Unreleased & Planned]

### 追加
- TimelineExtension
  - BPM基準でタイムラインを触れるようにする
  - GameObjectの名称変更にAnimationが追従するようにする
  - 「0打ち」をしなくていいようにする
  - その他、Timelineを使いやすくする様々な機能
- Timelineチュートリアル
- Export Prefab without Timeline
- Infinite Clip Baker
- Avatar Root Motion(Action Layer / not-VRChatAvatar Animator)
- Particle Curve Inverter - ParticleCurveを上下/左右反転させる機能

### 変更
- AudioClip.Create()を用いて、元のAudioClipのLoadInBackgroundを変更しないようにする

### 修正
- Low-effort Uploader : AnimationClipをLoopさせる場合に、良い感じの法線を設定する
- Low-effort Uploader : AnimationClipがブレンドされる場合に対応する

### 削除

### 非推奨
- (PaLASOLUの非推奨ではなく、PaLASOLUからPlayableDirectorへの"非推奨の要求"として、)Playable Track, Signal Track対応はVRC AvatarにScriptを含めることができないので、対応予定がありません

### 脆弱性

## [2.0.0] - [2025-10-10] - 4th month Anniversary! (from first publish)
### 重大
- Setup Optimizationで生成されるPrefabに変更が入っています。**1.2.1以前で生成されるPrefabとはまったくの別物であり、互換性がありません！**
	- 影響があるのはセットアップ時のみなので、ほとんどの場合は気にしなくても大丈夫です。
- 旧バージョンのPrefabにも変更が入っています。こちらはおそらく互換性は保ったままの変更ではありますが、動作に変更が加わっています。必要に応じてバックアップを取ってから更新をしてください。

### 追加
- Extensions/BPM to Second & Frame Calculatorを追加 (暫定)
- Control TrackのSource Game Object, Parent Object対応を追加
- Track Group対応を追加
- エラー文にバージョン記載を追加

### 変更
- Setup Optimizationで生成されるPrefabを全面的に変更
	- デフォルト状態ではPlayableDirector、PaLASOLU Low-effort Uploaderのみがついた単一Prefabを生成するように
	- 旧バージョンのようにワールド固定やメニューをHierarchy上に出したい(=カスタムしたい)場合は、高度な設定=>Advanced Setup のチェックを入れると旧バージョンに近い形態のPrefabが展開されます。
	- アバターを視界に入れていなくてもワールド固定、ライブ再生が正しく動くように
- Fix rotation for "Create Particle System"はデフォルトでOFFに
- Low-effort UploaderはGameObject名が"ParticleLive"以外でも正しく動くように

### 修正
- "LocalOnly"GameObject("IsLocal"パラメーター)が想定通り動いていなかった問題を修正
- SampleのQRコードが正しくなかったのを修正

## [1.2.1] - [2025-10-06]
### 修正
- Activation Trackの対象がnoneだった場合にビルドできなかったバグを修正

## [1.2.0] - [2025-09-29] - Welcome to UniMagic 6th Update!
### 追加
- Low-effort Uploaderに、AudioTrackの各種パラメータ対応を追加
	- AudioClipの途中再生(Clip in)
	- フェードイン/アウト(Blend in/out, Ease in/out)
	- 再生速度変更(Speed Multiplier)
	- ループ再生(AudioPlayableAsset Loop)

### 変更
- Setup OptimizationはBase Prefabからの派生Prefab Variantを生成するように変更

### 修正
- HelpのURLを最新版に修正

## [1.1.3] - [2025-09-16]
### 修正
- Low-effort Uploaderが、Timeline上のmute状態に関わらず非muteとしてアップロードを行っていた問題を修正
- Low-effort Uploaderが、1つのアバターに2つ以上存在した場合にうまくアップロードできなかった問題を修正

## [1.1.2] - [2025-08-01]
### 修正
- Low-effort Uploaderが空のAnimationTrackに対してエラーを吐く場合があった問題を修正

## [1.1.1] - [2025-07-17]
### 修正
- Fix rotation for "Create Particle System"において、"Particle System"を生成した場合にうまく働かなかった問題を修正

## [1.1.0] - [2025-07-15] - GlinTFraulein's Birthday Update!
### 追加
- Low-effort UploaderがAnimationClipを置いた状態のAnimationTrackに対応(暫定)
  - 複雑なCurveを描いている場合に壊れる可能性があります！壊れたら是非報告してください。
  - 既知の不具合 : AnimationClipをLoopさせる場合、最後がおかしくなることがある
  - 既知の不具合 : AnimationClipがブレンドされる場合に対応していない
- Low-effort UploaderがActivation Trackに対応
- Low-effort UploaderがControl Trackに対応(暫定)
  - Parent Object、Prefabのみ対応。Control Activation、Post Playbackは対応していません(よくわからなかったので)。
- Low-effort Uploaderの高度な設定に「Affect AudioTrack Volume」を追加
- Tools/PaLASOLU/Extensions/Fix rotation for "Create Particle System" を追加
  - ParticleSystemを新規作成した時に、rotation X を自動的に0に再設定します。
  - できるだけ軽い動作をするように設計していますが、パフォーマンスの悪化が気になる人はOFFにしてください。
- Tools/PaLASOLU/Sample を追加

### 変更
- Low-effort Uploader内でTimelineに紐づけられたAnimatorの取得方法を変更

### 修正
- Setup Optimizationで生成されるPrefabのViewPositionを分かりやすくするよう修正
- アバターのデモ画面やセーフティーがかかっている状態でパーティクルライブが出ないように修正
- AAOと併用している状況で、AAOに未知のコンポーネントとして検出されないよう修正
- [#4] Low-effort Uploaderと一緒に付いているPlayableDirectorがVRCSDKにエラー判定されないよう修正 by [anatawa12](https://github.com/anatawa12)
- その他、各種軽微なバグを修正

## [1.0.5] - [2025-07-01]
### 修正
- [#1] 一部環境で文字化けが発生していた問題の修正
- [#2] Setup OptimizationがReload Domain後にエラーを吐いていたのを修正
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