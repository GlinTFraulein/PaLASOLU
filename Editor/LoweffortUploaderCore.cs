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
		public Dictionary<string, string> bindings;
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
					LogMessageSimplifier.PaLog(2, "PaLASOLU Low-effort Uploader �� PlayableDirector �R���|�[�l���g���ݒ肳��Ă��܂���ILow-effort Uploader�̏����̓X�L�b�v����܂��B\nPaLASOLU Setup Optimization ����Z�b�g�A�b�v���s�����ꍇ�A\"[�y�Ȗ�]_ParticleLive/WorldFixed/ParticleLive\" GameObject �́A PaLASOLU Low-eoofrt Uploader �R���|�[�l���g���́A�u���x�Ȑݒ�v���� Playable Director ��None�łȂ����Ƃ��m�F���Ă��������B");
					return;
				}

				//RecordedClip Binding
				lfuState.timeline = director?.playableAsset as TimelineAsset;
				TimelineAsset timeline = lfuState.timeline;
				if (timeline == null)
				{
					LogMessageSimplifier.PaLog(2, "PlayableDirector �� Timeline Asset �A�Z�b�g���ݒ肳��Ă��܂���ILow-effort Uploader�̏����̓X�L�b�v����܂��B\nPaLASOLU Setup Optimization ����Z�b�g�A�b�v���s�����ꍇ�A\"[�y�Ȗ�]_ParticleLive/WorldFixed/ParticleLive\" GameObject �́A PlayableDirector �R���|�[�l���g���́A Playable �� None �łȂ����Ƃ��m�F���Ă��������B");
					return;
				}

				//"Recorded" clips extraction
				string timelinePath = AssetDatabase.GetAssetPath(timeline);
				Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(timelinePath);
				lfuState.recordedClips = new List<AnimationClip>();
				List<AnimationClip> recordedClips = lfuState.recordedClips;
				foreach (var asset in subAssets)
				{
					if (asset is AnimationClip clip /*&& clip.name.StartsWith("Recorded")*/)
					{
						recordedClips.Add(clip);
					}
				}

				//Animation Handling
				lfuState.bindings = new Dictionary<string, string>();
				Dictionary<string, string> bindings = lfuState.bindings;

				foreach (var track in timeline.GetOutputTracks())
				{
					if (track is AnimationTrack)
					{
						AnimationTrack animationTrack = track as AnimationTrack;
						var animator = director.GetGenericBinding(track) as Animator;
						if (animator == null) continue;

						var infiniteClip = animationTrack.infiniteClip;
						if (infiniteClip == null /*|| !infiniteClip.name.StartsWith("Recorded")*/) continue;

						//animatorPath��obj.gameObject����̑��΃p�X�����
						string animatorPath = GetRelativePath(GetGameObjectPath(animator.gameObject), GetGameObjectPath(obj.gameObject));
						bindings[infiniteClip.name] = animatorPath;
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

				AnimationClip mergedAudioClip = new AnimationClip();
				mergedAudioClip.name = "mergedAudioClip";

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
								LogMessageSimplifier.PaLog(1, $"{nowClip.displayName} �ɃI�[�f�B�I�N���b�v�����݂��܂���B");
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

							//AnimationClip Generate
							EditorCurveBinding binding = new EditorCurveBinding();
							binding.path = audioObject.name;
							binding.type = typeof(GameObject);
							binding.propertyName = "m_IsActive";

							AnimationCurve curve = new AnimationCurve();
							if ((float)nowClip.start != 0f) curve.AddKeySetActive(0f, false);
							curve.AddKeySetActive((float)nowClip.start, true);
							curve.AddKeySetActive((float)nowClip.end, false);

							AnimationUtility.SetEditorCurve(mergedAudioClip, binding, curve);
							mergedAudioClip.legacy = false;
						}
					}
				}

				//Animator Setup (for AnimationClips)
				Dictionary<string, string> bindings = lfuState.bindings;
				if (bindings == null) return;

				if (lfuState.recordedClips == null) return;

				foreach (var binding in bindings)
				{
					//Get Animator
					string clipName = binding.Key;
					string animatorPath = binding.Value;

					Transform animatorTransform = obj.transform.Find(animatorPath == "." ? "" : animatorPath);
					Animator animator = animatorTransform?.GetComponent<Animator>();

					//Generate
					AnimatorController controller = animator.runtimeAnimatorController as AnimatorController;
					if (controller == null)
					{
						controller = new AnimatorController();
						animator.runtimeAnimatorController = controller;
					}

					AnimationClip clip = lfuState.recordedClips.FirstOrDefault(c => c.name == clipName);  //�O���疼�̈�v�ŒT���Ă���
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

				//Animator Setup 2 (for AudioSource GameObjects)
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
				 
				rootController.AddLayer(SetupNewLayerAndState(mergedAudioClip));

				//PlayableDirector Delete
				PlayableDirector director = lfuState.director;

				if (director != null)
				{
					if (PrefabUtility.IsPartOfPrefabInstance(director))
					{
						PrefabUtility.RecordPrefabInstancePropertyModifications(director);
						Object.DestroyImmediate(director, true);
						LogMessageSimplifier.PaLog(0, "PlayableDirector �� Prefab ����폜���܂����B");
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
				return "."; // ���g

			return fullPath.Substring(rootPath.Length + 1); // '/'���΂�
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

	}
}