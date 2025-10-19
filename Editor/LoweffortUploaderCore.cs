using nadena.dev.ndmf;
using nadena.dev.ndmf.fluent;
using PaLASOLU;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

[assembly: ExportsPlugin(typeof(LoweffortUploaderCore))]

namespace PaLASOLU
{
	internal class LoweffortUploaderState
	{
		public List<LoweffortUploaderContext> Uploaders { get; set; } = new();
	}

	internal class LoweffortUploaderContext
	{
		public LoweffortUploader lfUploader;
		public PlayableDirector director;
		public TimelineAsset timeline;
		public Dictionary<string, GameObject> bindings;
	}

	internal class ProcessContext
	{
		public LoweffortUploaderContext lfuCtx;
		public LoweffortUploader lfUploader;
		public PlayableDirector director;
		public TimelineAsset timeline;
		public Dictionary<string, GameObject> bindings;
		public List<(AnimationClip, GameObject)> addClips;
		public AnimationClip mergedClip;
		public AudioTrackVolumeData volumeData;
	}

	public partial class LoweffortUploaderCore : Plugin<LoweffortUploaderCore>
	{
		protected override void Configure()
		{
			Sequence preProcess = InPhase(BuildPhase.Resolving);
			preProcess.BeforePlugin("nadena.dev.modular-avatar");
			preProcess.Run("PaLASOLU LfUploader Pre Process", ctx =>
			{
				LoweffortUploaderState lfuState = ctx.GetState<LoweffortUploaderState>();
				HashSet<TimelineAsset> seenTimelines = new HashSet<TimelineAsset>();

				foreach (LoweffortUploader lfUploader_finded in ctx?.AvatarRootObject.GetComponentsInChildren<LoweffortUploader>(true))
				{
					string lfUploader_ObjectName = lfUploader_finded.gameObject.name;

					PlayableDirector director_finded = lfUploader_finded.director;
					if (director_finded == null)
					{
						LogMessageSimplifier.PaLog(2, $"{lfUploader_ObjectName} の PaLASOLU Low-effort Uploader に PlayableDirector コンポーネントが設定されていません！Low-effort Uploaderの処理はスキップされます。\nPaLASOLU Setup Optimization からセットアップを行った場合、{lfUploader_ObjectName} GameObject の、 PaLASOLU Low-eoofrt Uploader コンポーネント内の、「高度な設定」から Playable Director がNoneでないことを確認してください。");
						continue;
					}

					TimelineAsset timeline_finded = lfUploader_finded.timeline;
					if (timeline_finded == null)
					{
						LogMessageSimplifier.PaLog(2, $"{lfUploader_ObjectName} の PlayableDirector に Timeline Asset アセットが設定されていません！Low-effort Uploaderの処理はスキップされます。\nPaLASOLU Setup Optimization からセットアップを行った場合、{lfUploader_ObjectName} GameObject の、 PlayableDirector コンポーネント内の、 Playable が None でないことを確認してください。");
						continue;
					}

					if (!seenTimelines.Add(timeline_finded))
					{
						LogMessageSimplifier.PaLog(1, $"Timeline {timeline_finded.name} は複数の LoweffortUploader から参照されています。 {lfUploader_finded.name} の処理はスキップされます。");
						continue;
					}

					LoweffortUploaderContext uploaderCtx = new LoweffortUploaderContext
					{
						lfUploader = lfUploader_finded,
						director = director_finded,
						timeline = timeline_finded,
						bindings = new Dictionary<string, GameObject>()
					};

					lfuState.Uploaders.Add(uploaderCtx);

					if (lfUploader_finded.generateAvatarMenu)
					{
						GameObject basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ParticleLiveSetup.basePrefabPath);
						GameObject prefabInstance = PrefabUtility.InstantiatePrefab(basePrefab) as GameObject;
						PrefabUtility.UnpackPrefabInstance(prefabInstance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
						prefabInstance.name = lfUploader_finded.gameObject.name + "_Base";
						prefabInstance.transform.parent = ctx.AvatarRootTransform;

						lfUploader_finded.transform.parent = prefabInstance.transform.Find("WorldFixed");
					}

					lfUploader_finded.gameObject.name = "ParticleLive";
				}

				foreach (LoweffortUploaderContext lfuCtx in lfuState.Uploaders)
				{
					//Animator Handling
					foreach (var track in lfuCtx.timeline.GetOutputTracks().OfType<AnimationTrack>())
					{
						var animator = lfuCtx.director.GetGenericBinding(track) as Animator;
						if (animator == null)
						{
							LogMessageSimplifier.PaLog(1, $"トラック {track.name} にAnimatorが設定されていません！Timelineが正しく再生されない可能性があります。");
							continue;
						}
						//animatorObjectは紐付けられているGameObjectを取る(AnimatorはNDMFで一度削除されるため)
						lfuCtx.bindings.Add(track.name, animator.gameObject);
					}
				}
			});

			Sequence coreProcess = InPhase(BuildPhase.Transforming);
			coreProcess.Run("PaLASOLU LfUploder Core Process", ctx =>
			{
				LoweffortUploaderState lfuState = ctx.GetState<LoweffortUploaderState>();

				foreach (LoweffortUploaderContext lfuCtx in lfuState.Uploaders)
				{
					if (lfuCtx.lfUploader == null) return;

					LoweffortUploader lfUploader = lfuCtx?.lfUploader;
					if (lfUploader == null) return;

					if (lfuCtx.timeline == null) return;

					PlayableDirector director = lfuCtx.director;
					if (director == null) return;

					TimelineAsset timeline = lfuCtx.timeline;
					if (timeline == null) return;

					Dictionary<string, GameObject> bindings = lfuCtx.bindings;
					if (bindings == null) return;

					AnimationClip mergedClip = new AnimationClip();
					mergedClip.name = "mergedClip";
					mergedClip.legacy = false;

					List<(AnimationClip addAnim, GameObject addObject)> addClips = new List<(AnimationClip, GameObject)>();

					AudioVolumeManager.CleanUpVolumeData(lfuCtx.timeline);
					AudioTrackVolumeData volumeData = AudioVolumeManager.GetOrCreateVolumeData(lfuCtx.timeline);

					ProcessContext processCtx = new ProcessContext
					{
						lfuCtx = lfuCtx,
						lfUploader = lfUploader,
						director = director,
						timeline = timeline,
						bindings = bindings,
						addClips = addClips,
						mergedClip = mergedClip,
						volumeData = volumeData
					};

					foreach (TrackAsset track in lfuCtx.timeline.GetOutputTracks())
					{
						ProcessTrack(track, processCtx);

					}

					addClips.Add((mergedClip, lfUploader.gameObject));

					//Animator Setup
					foreach (var addClip in addClips)
					{
						AnimationClip addAnimation = addClip.addAnim;
						GameObject addGameObject = addClip.addObject;

						Animator addAnimator = addGameObject?.GetComponent<Animator>();
						if (addAnimator == null) addAnimator = addGameObject.AddComponent<Animator>();

						AnimatorController addController = addAnimator?.runtimeAnimatorController as AnimatorController;
						if (addController == null)
						{
							addController = new AnimatorController();
							addAnimator.runtimeAnimatorController = addController;
						}

						if (addAnimation != null) addController.AddLayer(SetupNewLayerAndState(addAnimation));
					}

					// GameObject inactivate
					GameObject parent = director.gameObject.transform.parent.gameObject;
					if (parent.name == "WorldFixed") parent.SetActive(false);
				}
			});

			Sequence postProcess = InPhase(BuildPhase.Optimizing);
			postProcess.BeforePlugin("com.anatawa12.avatar-optimizer");
			postProcess.Run("PaLASOLU LfUploader Post Process", ctx =>
			{
				LoweffortUploaderState lfuState = ctx.GetState<LoweffortUploaderState>();

				foreach (LoweffortUploaderContext lfuCtx in lfuState.Uploaders)
				{
					if (lfuCtx.lfUploader == null) return;

					PlayableDirector director = lfuCtx.director;
					if (director == null) return;

					LoweffortUploader lfUploader = lfuCtx.lfUploader;
					if (lfUploader == null) return;

					//PlayableDirector Delete
					if (PrefabUtility.IsPartOfPrefabInstance(director))
					{
						PrefabUtility.RecordPrefabInstancePropertyModifications(director);
						Object.DestroyImmediate(director, true);
						LogMessageSimplifier.PaLog(0, "PlayableDirector を Prefab から削除しました。");
					}
					else
					{
						Object.DestroyImmediate(director);
					}

					//Delete LfUploader (for AAO Compatible)
					if (PrefabUtility.IsPartOfPrefabInstance(lfUploader))
					{
						PrefabUtility.RecordPrefabInstancePropertyModifications(lfUploader);
						Object.DestroyImmediate(lfUploader, true);
						LogMessageSimplifier.PaLog(0, "Low-effort Uploader を Prefab から削除しました。");
					}
					else
					{
						Object.DestroyImmediate(lfUploader);
					}
				}
			});
		}

		void ProcessTrack(TrackAsset track, ProcessContext processCtx)
		{
			//Track Group Handling
			foreach (TrackAsset child in track.GetChildTracks())
			{
				ProcessTrack(child, processCtx);
			}

			if (track.muted) return;

			LoweffortUploader lfUploader = processCtx.lfUploader;
			AnimationClip mergedClip = processCtx.mergedClip;

			//Animation Handling
			if (track is AnimationTrack)
			{
				AnimationClip sumOfClip = (track as AnimationTrack).infiniteClip;
				if (sumOfClip == null) sumOfClip = BakeAnimationTrackToMergedClip(track);
				if (!processCtx.bindings.TryGetValue(track.name, out var clip) || clip == null)
				{
					LogMessageSimplifier.PaLog(1, $"{track.name} トラックに紐づく GameObject が見つかりません。");
					return;
				}

				processCtx.addClips.Add((sumOfClip, clip));
			}

			//Audio Handling
			else if (track is AudioTrack && lfUploader.generateAudioObject)
			{
				List<TimelineClip> clips = track.GetClips().ToList();

				foreach (TimelineClip nowClip in clips)
				{
					AudioPlayableAsset audioPlayableAsset = nowClip?.asset as AudioPlayableAsset;
					AudioClip audioClip = audioPlayableAsset?.clip;

					if (audioClip == null)
					{
						LogMessageSimplifier.PaLog(1, $"{nowClip.displayName} にオーディオクリップが存在しません。");
						continue;
					}

					//Audio Modify - 何もしない場合はメソッド側でそのままaudioClipを返す
					audioClip = ModifyClip(audioClip, nowClip, processCtx.timeline);

					if (audioClip.loadInBackground == false)
					{
						string audioClipPath = AssetDatabase.GetAssetPath(audioClip);
						AudioImporter audioImporter = AssetImporter.GetAtPath(audioClipPath) as AudioImporter;
						audioImporter.loadInBackground = true;
						audioImporter.SaveAndReimport();
					}

					string uniqueName = SetUniqueName(audioClip.name);
					GameObject audioObject = new GameObject(uniqueName);
					audioObject.transform.parent = lfUploader.transform;
					audioObject.SetActive(false);

					AudioSource audioSource = audioObject.AddComponent<AudioSource>();
					audioSource.clip = audioClip;
					audioSource.loop = audioPlayableAsset.loop;

					//Volume Affect
					if (processCtx.lfuCtx.lfUploader.isAffectedAudioVolume)
					{
						int instanceID = audioPlayableAsset.GetInstanceID();

						AudioTrackVolumeEntity entity = processCtx.volumeData.entities.Find(e => e.instanceID == instanceID);
						float volume = entity != null ? entity.volume : 1.0f;

						audioSource.volume = volume;
					}

					GenerateAndBindActivateCurve(mergedClip, nowClip, audioObject.name);
				}
			}

			//Activate Handling
			else if (track is ActivationTrack)
			{
				List<TimelineClip> clips = track.GetClips().ToList();

				GameObject activateObject = processCtx.director.GetGenericBinding(track) as GameObject;
				if (activateObject == null)
				{
					LogMessageSimplifier.PaLog(1, $"{track.name} にGameObjectが存在しません。");
					return;
				}

				string uniqueName = SetUniqueName(activateObject.name);
				activateObject.name = uniqueName;

				string activateObjectPath = GetGameObjectPath(activateObject);
				string rootObjectPath = GetGameObjectPath(lfUploader.gameObject);
				EditorCurveBinding binding = AnimationEditExtension.CreateIsActiveBinding(GetRelativePath(activateObjectPath, rootObjectPath));

				AnimationCurve curve = new AnimationCurve();

				foreach (TimelineClip nowClip in clips)
				{
					curve.AddKeySingleOnOff((float)nowClip.start, (float)nowClip.end);
				}

				AnimationUtility.SetEditorCurve(mergedClip, binding, curve);
			}

			//Control Handling
			else if (track is ControlTrack)
			{
				List<TimelineClip> clips = track.GetClips().ToList();

				foreach (TimelineClip nowClip in clips)
				{
					ControlPlayableAsset controlPlayableAsset = nowClip?.asset as ControlPlayableAsset;
					GameObject prefab = controlPlayableAsset.prefabGameObject;

					GameObject prefabObject;
					PlayableDirector director = processCtx.director;

					if (prefab != null)
					{
						prefabObject = GameObject.Instantiate(prefab);
						GameObject parentObject = controlPlayableAsset.sourceGameObject.Resolve(director);
						prefabObject.transform.SetParent(parentObject == null ? director.gameObject.transform : parentObject.transform);

						GameObject transformObject = controlPlayableAsset.sourceGameObject.Resolve(director);
						if (transformObject != null)
						{
							prefabObject.transform.SetParent(transformObject.transform);
							prefabObject.transform.localPosition = Vector3.zero;
							prefabObject.transform.localRotation = Quaternion.identity;
							prefabObject.transform.localScale = Vector3.one;
						}
					}
					else
					{
						prefabObject = controlPlayableAsset.sourceGameObject.Resolve(director);
					}

					string uniqueName = SetUniqueName(prefabObject.name);
					prefabObject.name = uniqueName;
					prefabObject.SetActive(false);

					string prefabObjectPath = GetGameObjectPath(prefabObject);
					string rootObjectPath = GetGameObjectPath(lfUploader.gameObject);
					GenerateAndBindActivateCurve(mergedClip, nowClip, GetRelativePath(prefabObjectPath, rootObjectPath));
				}
			}
		}

		private static string GetRelativePath(string fullPath, string rootPath)
		{
			if (!fullPath.StartsWith(rootPath))
				return null; // invalid

			if (fullPath == rootPath)
				return "."; // 自身

			return fullPath.Substring(rootPath.Length + 1); // '/'を飛ばす
		}

		public static string GetGameObjectPath(GameObject wantPassObject)
		{
			string path = wantPassObject.name;
			Transform current = wantPassObject.transform;

			while (current.parent != null)
			{
				current = current.parent;
				path = current.name + "/" + path;
			}

			return path;
		}

		public static AnimatorControllerLayer SetupNewLayerAndState(AnimationClip addClip)
		{
			AnimatorControllerLayer newLayer = new AnimatorControllerLayer();
			newLayer.name = addClip.name;
			newLayer.defaultWeight = 1.0f;
			newLayer.blendingMode = AnimatorLayerBlendingMode.Override;
			newLayer.stateMachine = new AnimatorStateMachine();
			newLayer.stateMachine.name = addClip.name;

			AnimatorState newState = newLayer.stateMachine.AddState(addClip.name);
			newState.motion = addClip;

			return newLayer;
		}

		public void GenerateAndBindActivateCurve(AnimationClip mergedClip, TimelineClip clip, string objectName)
		{
			EditorCurveBinding binding = AnimationEditExtension.CreateIsActiveBinding(objectName);

			AnimationCurve curve = new AnimationCurve();
			curve.AddKeySingleOnOff((float)clip.start, (float)clip.end);

			AnimationUtility.SetEditorCurve(mergedClip, binding, curve);

			return;
		}

		public static AnimationClip BakeAnimationTrackToMergedClip(TrackAsset track)
		{
			if (track is not AnimationTrack animationTrack)
			{
				LogMessageSimplifier.PaLog(5, "Track is not an AnimationTrack!");
				return null;
			}

			TimelineClip[] timelineClips = animationTrack.GetClips().ToArray();
			if (timelineClips.Length == 0)
			{
				LogMessageSimplifier.PaLog(1, $"{track.name} トラックには、アニメーションデータがありません！");
				return null;
			}

			// Merge Clip
			AnimationClip mergedClip = new AnimationClip
			{
				name = $"{track.name}_Merged",
				legacy = false
			};

			foreach (TimelineClip clip in timelineClips)
			{
				if (clip.asset is not AnimationPlayableAsset playableAsset)
				{
					LogMessageSimplifier.PaLog(4, $"TimelineClip {clip.displayName} is not an AnimationPlayableAsset.");
					continue;
				}

				AnimationClip sourceClip = playableAsset.clip;
				if (sourceClip == null)
				{
					LogMessageSimplifier.PaLog(1, $"TimelineClip {clip.displayName} に、 AnimationClip が設定されていません！");
					continue;
				}

				double startTime = clip.start;
				double endTime = clip.end;
				bool isLoop = IsLoopingTimelineClip(clip);
				double clipLength = sourceClip.length;
				double timelineLength = clip.duration;
				int loopCount = IsLoopingTimelineClip(clip) ? Mathf.CeilToInt((float)(timelineLength / clipLength)) : 1;

				EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(sourceClip);
				EditorCurveBinding[] objBindings = AnimationUtility.GetObjectReferenceCurveBindings(sourceClip);

				foreach (EditorCurveBinding binding in bindings)
				{
					AnimationCurve curve = AnimationUtility.GetEditorCurve(sourceClip, binding);
					if (curve == null) continue;

					// キーフレームを開始時間分だけオフセットしてコピー
					AnimationCurve newCurve = new AnimationCurve();
					newCurve.preWrapMode = curve.preWrapMode;
					newCurve.postWrapMode = curve.postWrapMode;

					List<Keyframe> allKeys = new List<Keyframe>();

					for (int loop = 0; loop < loopCount; loop++)
					{
						double loopedPart = loop * clipLength;

						foreach (Keyframe key in curve.keys)
						{
							float newTime = key.time + (float)startTime + (float)loopedPart;
							allKeys.Add(new Keyframe(newTime, key.value, key.inTangent, key.outTangent));
						}
					}

					if (timelineLength % clipLength > 0.0001) //ちょうどループしない時
					{
						float evalTime = (float)endTime;
						if (evalTime > sourceClip.length) evalTime = evalTime % sourceClip.length;

						float value = curve.Evaluate(evalTime);
						/*
						 * TODO : Tangent補完
						float tangent = 0;

						if (curve.length >= 2)
						{
							for (int i = 0; i < curve.length - 1; i++)
							{
								if (curve.keys[i].time <= evalTime && evalTime <= curve.keys[i + 1].time)
								{
									var k0 = curve.keys[i];
									var k1 = curve.keys[i + 1];
									tangent = (k1.value - k0.value) / (k1.time - k0.time);
									break;
								}
							}
						}*/

						allKeys.Add(new Keyframe((float)endTime, value));
					}

					List<Keyframe> validKeys = allKeys.Where(k => k.time <= endTime).ToList();
					newCurve.keys = validKeys.ToArray();

					AnimationCurve existing = AnimationUtility.GetEditorCurve(mergedClip, binding);
					if (existing != null)
					{
						foreach (var key in newCurve.keys)
						{
							existing.AddKey(key);
						}
						AnimationUtility.SetEditorCurve(mergedClip, binding, existing);
					}
					else
					{
						AnimationUtility.SetEditorCurve(mergedClip, binding, newCurve);
					}
				}

				foreach (EditorCurveBinding binding in objBindings)
				{
					ObjectReferenceKeyframe[] curve = AnimationUtility.GetObjectReferenceCurve(sourceClip, binding);
					if (curve == null) continue;

					List<ObjectReferenceKeyframe> newKeys = new List<ObjectReferenceKeyframe>();

					foreach (ObjectReferenceKeyframe originalKey in curve)
					{
						for (int loop = 0; loop < loopCount; loop++)
						{
							float newTime = originalKey.time + (float)startTime + (float)clipLength * loop;
							if (newTime > endTime) continue;

							newKeys.Add(new ObjectReferenceKeyframe
							{
								time = newTime,
								value = originalKey.value
							});
						}
					}

					ObjectReferenceKeyframe[] existing = AnimationUtility.GetObjectReferenceCurve(mergedClip, binding);
					if (existing != null && existing.Length > 0)
					{
						//Sort to Time
						var merged = existing.Concat(newKeys).OrderBy(kf => kf.time).ToArray();
						AnimationUtility.SetObjectReferenceCurve(mergedClip, binding, merged);
					}
					else
					{
						AnimationUtility.SetObjectReferenceCurve(mergedClip, binding, newKeys.ToArray());
					}
				}

			}

			LogMessageSimplifier.PaLog(0, $"MergedClip generated: {mergedClip.name}");
			return mergedClip;
		}

		public static bool IsLoopingTimelineClip(TimelineClip clip)
		{
			PlayableAsset playableAsset = clip.asset as PlayableAsset;
			AnimationPlayableAsset animPlayable = playableAsset as AnimationPlayableAsset;
			AudioPlayableAsset audioPlayable = playableAsset as AudioPlayableAsset;
			if ((playableAsset is not AnimationPlayableAsset) && (playableAsset is not AudioPlayableAsset))
			{
				return false;
			}

			bool capsAllowLoop = (clip.clipCaps & ClipCaps.Looping) != 0;
			bool assetAllowLoop = false;

			if (playableAsset is AnimationPlayableAsset)
			{
				bool sourceAssetLoop = animPlayable.clip.isLooping;
				bool assetLoop = (animPlayable.loop == AnimationPlayableAsset.LoopMode.On) || (animPlayable.loop == AnimationPlayableAsset.LoopMode.UseSourceAsset && sourceAssetLoop);

				assetAllowLoop = assetLoop;
			}

			if (playableAsset is AudioPlayableAsset)
			{
				assetAllowLoop = audioPlayable.loop;
			}

			return capsAllowLoop && assetAllowLoop;
		}

		public static string SetUniqueName(string name)
		{
			string uniqueId = System.Guid.NewGuid().ToString("N").Substring(0, 8);
			string uniqueName = $"{name}_{uniqueId}";

			return uniqueName;
		}
	}
}