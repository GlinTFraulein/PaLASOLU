﻿using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Timeline;
using UnityEngine.Playables;
using nadena.dev.ndmf;
using nadena.dev.ndmf.fluent;
using PaLASOLU;

[assembly: ExportsPlugin(typeof(LoweffortUploaderCore))]

namespace PaLASOLU
{
	internal class LoweffortUploaderState
	{
		public LoweffortUploader lfUploader { get; set; }

		public PlayableDirector director;
		public TimelineAsset timeline;
		public Dictionary<string, GameObject> bindings;
	}

	public class LoweffortUploaderCore : Plugin<LoweffortUploaderCore>
	{
		protected override void Configure()
		{
			Sequence preProcess = InPhase(BuildPhase.Resolving);
			preProcess.BeforePlugin("nadena.dev.modular-avatar");
			preProcess.Run("PaLASOLU LfUploader Pre Process", ctx =>
			{
				LoweffortUploaderState lfuState = ctx.GetState<LoweffortUploaderState>();
				lfuState.lfUploader = ctx?.AvatarRootObject.GetComponentInChildren<LoweffortUploader>(true);

				LoweffortUploader obj = lfuState.lfUploader;
				if (obj == null)
				{
					LogMessageSimplifier.PaLog(0, "PaLASOLU Low-effort Uploader is not found.");
					return;
				}

				lfuState.director = obj?.director;
				PlayableDirector director = lfuState.director;
				if (director == null)
				{
					LogMessageSimplifier.PaLog(2, "PaLASOLU Low-effort Uploader に PlayableDirector コンポーネントが設定されていません！Low-effort Uploaderの処理はスキップされます。\nPaLASOLU Setup Optimization からセットアップを行った場合、\"[楽曲名]_ParticleLive/WorldFixed/ParticleLive\" GameObject の、 PaLASOLU Low-eoofrt Uploader コンポーネント内の、「高度な設定」から Playable Director がNoneでないことを確認してください。");
					return;
				}

				lfuState.timeline = director?.playableAsset as TimelineAsset;
				TimelineAsset timeline = lfuState.timeline;
				if (timeline == null)
				{
					LogMessageSimplifier.PaLog(2, "PlayableDirector に Timeline Asset アセットが設定されていません！Low-effort Uploaderの処理はスキップされます。\nPaLASOLU Setup Optimization からセットアップを行った場合、\"[楽曲名]_ParticleLive/WorldFixed/ParticleLive\" GameObject の、 PlayableDirector コンポーネント内の、 Playable が None でないことを確認してください。");
					return;
				}

				//Animator Handling
				lfuState.bindings = new Dictionary<string, GameObject>();
				Dictionary<string, GameObject> bindings = lfuState.bindings;

				foreach (var track in timeline.GetOutputTracks())
				{
					if (track is AnimationTrack)
					{
						AnimationTrack animationTrack = track as AnimationTrack;
						var animator = director.GetGenericBinding(track) as Animator;
						if (animator == null)
						{
							LogMessageSimplifier.PaLog(1, $"トラック {track.name} にAnimatorが設定されていません！Timelineが正しく再生されない可能性があります。");
							continue;
						}

						//animatorObjectは紐付けられているGameObjectを取る(AnimatorはNDMFで一度削除されるため)
						GameObject animatorObject = animator.gameObject;
						bindings[track.name] = animatorObject;
					}
				}
			});

			Sequence coreProcess = InPhase(BuildPhase.Transforming);
			coreProcess.Run("PaLASOLU LfUploder Core Process", ctx =>
			{
				LoweffortUploaderState lfuState = ctx.GetState<LoweffortUploaderState>();
				if (lfuState.lfUploader == null) return;

				LoweffortUploader obj = lfuState?.lfUploader;
				if (obj == null) return;

				if (lfuState.timeline == null) return;

				PlayableDirector director = lfuState.director;
				if (director == null) return;

				Dictionary<string, GameObject> bindings = lfuState.bindings;
				if (bindings == null) return;

				AnimationClip mergedClip = new AnimationClip();
				mergedClip.name = "mergedClip";
				mergedClip.legacy = false;

				List<(AnimationClip addAnim, GameObject addObject)> addClips = new List<(AnimationClip, GameObject)>();
				
				//AudioVolumeManager.CleanUpVolumeData(lfuState.timeline);  //多分CleanUpがやりすぎるバグがあるので一旦消しておく
				AudioTrackVolumeData volumeData = AudioVolumeManager.GetOrCreateVolumeData(lfuState.timeline);

				foreach (TrackAsset track in lfuState.timeline.GetOutputTracks())
				{
					//Animation Handling
					if (track is AnimationTrack)
					{
						AnimationClip sumOfClip = (track as AnimationTrack).infiniteClip;
						if (sumOfClip == null) sumOfClip = BakeAnimationTrackToMergedClip(track);

						addClips.Add((sumOfClip, bindings[track.name]));
					}

					//Audio Handling
					else if (track is AudioTrack && obj.generateAudioObject)
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

							if (audioClip.loadInBackground == false)
							{
								string audioClipPath = AssetDatabase.GetAssetPath(audioClip);
								AudioImporter audioImporter = AssetImporter.GetAtPath(audioClipPath) as AudioImporter;
								audioImporter.loadInBackground = true;
								audioImporter.SaveAndReimport();
							}

							string uniqueId = System.Guid.NewGuid().ToString("N").Substring(0, 8);
							string uniqueName = $"{audioClip.name}_{uniqueId}";
							GameObject audioObject = new GameObject(uniqueName);
							audioObject.transform.parent = obj.transform;
							audioObject.SetActive(false);

							AudioSource audioSource = audioObject.AddComponent<AudioSource>();
							audioSource.clip = audioClip;

							//Volume Affect
							if (lfuState.lfUploader.isAffectedAudioVolume)
							{
								string trackName = track.name;
								string clipName = audioPlayableAsset.clip.name;
								double startTime = nowClip.start;

								AudioTrackVolumeEntity entity = volumeData.entities.Find(e => e.trackName == trackName && e.clipName == clipName && e.start == startTime);
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

						GameObject activateObject = director.GetGenericBinding(track) as GameObject;
						if (activateObject == null)
						{
							LogMessageSimplifier.PaLog(1, $"{track.name} にGameObjectが存在しません。");
						}

						string uniqueId = System.Guid.NewGuid().ToString("N").Substring(0, 8);
						string uniqueName = $"{activateObject.name}_{uniqueId}";
						activateObject.name = uniqueName;

						string activateObjectPath = GetGameObjectPath(activateObject);
						string rootObjectPath = GetGameObjectPath(obj.gameObject);
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

							if (prefab == null)
							{
								LogMessageSimplifier.PaLog(1, $"{nowClip.displayName} にPrefabが設定されていません。");
								continue;
							}

							GameObject prefabObject = GameObject.Instantiate(prefab);
							GameObject parentObject = controlPlayableAsset.sourceGameObject.Resolve(director);
							prefabObject.transform.SetParent(parentObject == null ? director.gameObject.transform : parentObject.transform);

							string uniqueId = System.Guid.NewGuid().ToString("N").Substring(0, 8);
							prefabObject.name = $"{prefab.name}_{uniqueId}";
							prefabObject.SetActive(false);

							string prefabObjectPath = GetGameObjectPath(prefabObject);
							string rootObjectPath = GetGameObjectPath(obj.gameObject);
							GenerateAndBindActivateCurve(mergedClip, nowClip, GetRelativePath(prefabObjectPath, rootObjectPath));
						}
					}
				}

				addClips.Add((mergedClip, obj.gameObject));

				//Animator Setup
				foreach(var addClip in addClips)
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
			});

			Sequence postProcess = InPhase(BuildPhase.Optimizing);
			postProcess.BeforePlugin("com.anatawa12.avatar-optimizer");
			postProcess.Run("PaLASOLU LfUploader Post Process", ctx =>
			{
				LoweffortUploaderState lfuState = ctx.GetState<LoweffortUploaderState>();
				if (lfuState.lfUploader == null) return;

				PlayableDirector director = lfuState.director;
				if (director == null) return;

				LoweffortUploader lfUploader = lfuState.lfUploader;

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
					LogMessageSimplifier.PaLog(0, "PlayableDirector を Prefab から削除しました。");
				}
				else
				{
					Object.DestroyImmediate(lfUploader);
				}
			});
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
	}
}