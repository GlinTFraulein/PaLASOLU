using System.Collections.Generic;
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
		public List<AnimationClip> recordedClips;
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

				//RecordedClip Binding
				lfuState.timeline = director?.playableAsset as TimelineAsset;
				TimelineAsset timeline = lfuState.timeline;
				if (timeline == null)
				{
					LogMessageSimplifier.PaLog(2, "PlayableDirector に Timeline Asset アセットが設定されていません！Low-effort Uploaderの処理はスキップされます。\nPaLASOLU Setup Optimization からセットアップを行った場合、\"[楽曲名]_ParticleLive/WorldFixed/ParticleLive\" GameObject の、 PlayableDirector コンポーネント内の、 Playable が None でないことを確認してください。");
					return;
				}

				//"Recorded" clips extraction
				string timelinePath = AssetDatabase.GetAssetPath(timeline);
				Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(timelinePath);
				lfuState.recordedClips = new List<AnimationClip>();
				List<AnimationClip> recordedClips = lfuState.recordedClips;
				foreach (var asset in subAssets)
				{
					if (asset is AnimationClip clip)
					{
						recordedClips.Add(clip);
					}
				}

				//Animation Handling
				lfuState.bindings = new Dictionary<string, GameObject>();
				Dictionary<string, GameObject> bindings = lfuState.bindings;

				foreach (var track in timeline.GetOutputTracks())
				{
					if (track is AnimationTrack)
					{
						AnimationTrack animationTrack = track as AnimationTrack;
						var animator = director.GetGenericBinding(track) as Animator;
						if (animator == null) continue;

						var infiniteClip = animationTrack.infiniteClip;
						if (infiniteClip == null) continue;

						//animatorObjectは紐付けられているGameObjectを取る(AnimatorはNDMFで一度削除されるため)
						GameObject animatorObject = animator.gameObject;
						bindings[infiniteClip.name] = animatorObject;
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

				AnimationClip mergedClip = new AnimationClip();
				mergedClip.name = "mergedClip";
				mergedClip.legacy = false;

				foreach (var track in lfuState.timeline.GetOutputTracks())
				{
					//Audio Handling
					if (track is AudioTrack && obj.generateAudioObject)
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
							if (lfuState.lfUploader.isAffectedAudioVolume) audioSource.volume = GetTrackVolume(track);

							//AnimationClip Generate
							EditorCurveBinding binding = AnimationEditExtension.CreateIsActiveBinding(audioObject.name);

							AnimationCurve curve = new AnimationCurve();
							if ((float)nowClip.start != 0f) curve.AddKeySetActive(0f, false);
							curve.AddKeySetActive((float)nowClip.start, true);
							curve.AddKeySetActive((float)nowClip.end, false);

							AnimationUtility.SetEditorCurve(mergedClip, binding, curve);
						}
					}

					//Activate Handling
					else if (track is ActivationTrack)
					{
						List<TimelineClip> clips = track.GetClips().ToList();

						GameObject activateObject = director.GetGenericBinding(track) as GameObject;
						if (activateObject != null)
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
							if ((float)nowClip.start != 0f) curve.AddKeySetActive(0f, false);
							curve.AddKeySetActive((float)nowClip.start, true);
							curve.AddKeySetActive((float)nowClip.end, false);							
						}

						AnimationUtility.SetEditorCurve(mergedClip, binding, curve);
					}
				}

				//Animator Setup (for AnimationClips)
				Dictionary<string, GameObject> bindings = lfuState.bindings;
				if (bindings == null) return;

				if (lfuState.recordedClips == null) return;

				foreach (var binding in bindings)
				{
					//Get Animator
					string clipName = binding.Key;
					GameObject animatorObject = binding.Value;

					Transform animatorTransform = animatorObject.transform;
					Animator animator = animatorTransform?.GetComponent<Animator>();

					//Generate
					AnimatorController controller = animator.runtimeAnimatorController as AnimatorController;
					if (controller == null)
					{
						controller = new AnimatorController();
						animator.runtimeAnimatorController = controller;
					}

					AnimationClip clip = lfuState.recordedClips.FirstOrDefault(c => c.name == clipName);  //前から名称一致で探している
					if (clip == null)
					{
						LogMessageSimplifier.PaLog(4, $"[PaLASOLU] Recorded clip is not found.: {clipName}");
						continue;
					}
					
					bool layerExists = controller.layers.Any(layer => layer.name == clipName);
					if (!layerExists)
					{
						controller.AddLayer(SetupNewLayerAndState(clip));
					}
				}

				//Animator Setup 2 (for mergedClip)
				Animator rootAnimator = obj?.gameObject.GetComponent<Animator>();
				if (rootAnimator == null)
				{
					rootAnimator = obj.gameObject.AddComponent<Animator>();
				}
				AnimatorController rootController = rootAnimator?.runtimeAnimatorController as AnimatorController;
				if (rootController == null)
				{
					rootController = new AnimatorController();
					rootAnimator.runtimeAnimatorController = rootController;
				}
				 
				rootController.AddLayer(SetupNewLayerAndState(mergedClip));

				//PlayableDirector Delete
				if (director != null)
				{
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

		public static Keyframe SetTangentIO(Keyframe keyframe)
		{
			keyframe.outTangent = float.PositiveInfinity;
			keyframe.inTangent = float.PositiveInfinity;

			return keyframe;
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

		//WARNING : Using Internal API!!
		float GetTrackVolume(TrackAsset track)
		{
			SerializedProperty properties = new SerializedObject(track).GetIterator();

			while (properties.NextVisible(true))
			{
				if (properties.name == "m_TrackProperties")
				{
					var propertiesItr = properties.Copy();
					var endProperties = propertiesItr.GetEndProperty();

					while (propertiesItr.NextVisible(true) && !SerializedProperty.EqualContents(propertiesItr, endProperties))
					{
						if (propertiesItr.name == "volume")
						{
							return propertiesItr.floatValue;
						}
					}
					LogMessageSimplifier.PaLog(4, $"Cannot find \"Volume\" property iterator.");
				}
			}

			LogMessageSimplifier.PaLog(4, $"Cannot find \"m_TrackProperties\" property.");
			return 1.0f;
		}

	}
}