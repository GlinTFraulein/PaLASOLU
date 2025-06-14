using UnityEngine;
using nadena.dev.ndmf;
using PaLASOLU;
using UnityEngine.Timeline;
using System.Linq;
using UnityEditor.Animations;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using nadena.dev.ndmf.fluent;
using UnityEngine.Playables;

[assembly: ExportsPlugin(typeof(LoweffortUploaderCore))]

namespace PaLASOLU
{
    public class LoweffortUploaderCore : Plugin<LoweffortUploaderCore>
    {
        LoweffortUploader obj;
        PlayableDirector director;
        TimelineAsset timeline;
        string timelinePath;
        Object[] subAssets;
        List<AnimationClip> recordedClips;
        Dictionary<string, string> bindings;
        List<AnimationClip> playAudioClips;


        protected override void Configure()
        {
            Sequence preProcess = InPhase(BuildPhase.Resolving);
            preProcess.BeforePlugin("nadena.dev.modular-avatar");
            preProcess.Run("PaLASOLU LfUploader Pre Process", ctx =>
            {
                obj = ctx?.AvatarRootObject.GetComponentInChildren<LoweffortUploader>();

                director = obj?.director;
                if (director == null)
                {
                    Debug.LogError("[PaLASOLU] �G���[ : PaLASOLU Low-effort Uploader �� PlayableDirector �R���|�[�l���g���ݒ肳��Ă��܂���ILow-effort Uploader�̏����̓X�L�b�v����܂��B\nPaLASOLU Setup Optimization ����Z�b�g�A�b�v���s�����ꍇ�A\"[�y�Ȗ�]_ParticleLive/WorldFixed/ParticleLive\" GameObject �́A PaLASOLU Low-eoofrt Uploader �R���|�[�l���g���́A�u���x�Ȑݒ�v���� Playable Director ��None�łȂ����Ƃ��m�F���Ă��������B");
                    return;
                }

                //RecordedClip Binding
                timeline = director?.playableAsset as TimelineAsset;
                if (timeline == null)
                {
                    Debug.LogError("[PaLASOLU] �G���[ : PlayableDirector �� Timeline Asset �A�Z�b�g���ݒ肳��Ă��܂���ILow-effort Uploader�̏����̓X�L�b�v����܂��B\nPaLASOLU Setup Optimization ����Z�b�g�A�b�v���s�����ꍇ�A\"[�y�Ȗ�]_ParticleLive/WorldFixed/ParticleLive\" GameObject �́A PlayableDirector �R���|�[�l���g���́A Playable �� None �łȂ����Ƃ��m�F���Ă��������B");
                    return;
                }

                //"Recorded" clips extraction
                timelinePath = AssetDatabase.GetAssetPath(timeline);
                subAssets = AssetDatabase.LoadAllAssetsAtPath(timelinePath);
                recordedClips = new List<AnimationClip>();
                foreach (var asset in subAssets)
                {
                    if (asset is AnimationClip clip && clip.name.StartsWith("Recorded"))
                    {
                        recordedClips.Add(clip);
                    }
                }

                //Animation Handling
                bindings = new Dictionary<string, string>();
                playAudioClips = new List<AnimationClip>();

                foreach (var track in timeline.GetOutputTracks())
                {
                    if (track is AnimationTrack)
                    {
                        AnimationTrack animationTrack = track as AnimationTrack;
                        var animator = director.GetGenericBinding(track) as Animator;
                        if (animator == null) continue;

                        var infiniteClip = animationTrack.infiniteClip;
                        if (infiniteClip == null || !infiniteClip.name.StartsWith("Recorded")) continue;

                        //animatorPath��obj.gameObject����̑��΃p�X�����
                        string animatorPath = GetRelativePath(GetGameObjectPath(animator.gameObject), GetGameObjectPath(obj.gameObject));
                        bindings[infiniteClip.name] = animatorPath;
                    }
                }
            });

            Sequence coreProcess = InPhase(BuildPhase.Transforming);
            coreProcess.Run("PaLASOLU LfUploder Core Process", ctx =>
            {
                foreach (var track in timeline.GetOutputTracks())
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
                                Debug.LogWarning("[PaLASOLU] �x�� : " + nowClip.displayName + " �ɃI�[�f�B�I�N���b�v�����݂��܂���B");
                                continue;
                            }

                            string uniqueId = System.Guid.NewGuid().ToString("N").Substring(0, 8);
                            string uniqueName = $"{audioClip.name}_{uniqueId}";
                            GameObject audioObject = new GameObject(uniqueName);
                            audioObject.transform.parent = obj.transform;
                            audioObject.SetActive(false);

                            AudioSource audioSource = audioObject.AddComponent<AudioSource>();
                            audioSource.clip = audioClip;

                            //AnimationClip Generate
                            AnimationClip playAudioClip = new AnimationClip();
                            playAudioClip.name = audioObject.name;

                            EditorCurveBinding binding = new EditorCurveBinding();
                            binding.path = audioObject.name;
                            binding.type = typeof(GameObject);
                            binding.propertyName = "m_IsActive";

                            AnimationCurve curve = new AnimationCurve();
                            Keyframe off = new Keyframe(0f, 0);
                            off.outTangent = float.PositiveInfinity;
                            Keyframe on = new((float)nowClip.start, 1);
                            on.inTangent = float.PositiveInfinity;
                            on.outTangent = float.PositiveInfinity;
                            Keyframe off2 = new Keyframe((float)nowClip.end, 0);
                            off2.inTangent = float.NegativeInfinity;

                            if ((float)nowClip.start != 0f) curve.AddKey(off);
                            curve.AddKey(on);
                            curve.AddKey(off2);

                            AnimationUtility.SetEditorCurve(playAudioClip, binding, curve);
                            playAudioClip.legacy = false;

                            playAudioClips.Add(playAudioClip);
                        }
                    }
                }
                
                //Animator Setup (for AnimationClips)
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

                    AnimationClip clip = recordedClips.FirstOrDefault(c => c.name == clipName);  //�O���疼�̈�v�ŒT���Ă���
                    if (clip == null)
                    {
                        Debug.LogWarning($"[PaLASOLU] Recorded clip��������܂���B: {clipName}");
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

                foreach (AnimationClip playAudioClip in playAudioClips)
                {
                    if (playAudioClip == null) continue;
                    rootController.AddLayer(SetupNewLayerAndState(playAudioClip));
                }

                //PlayableDirector Delete
                if (director != null)
                {
                    if (PrefabUtility.IsPartOfPrefabInstance(director))
                    {
                        PrefabUtility.RecordPrefabInstancePropertyModifications(director);
                        Object.DestroyImmediate(director, true);
                        Debug.Log("[PaLASOLU] PlayableDirector �� Prefab ����폜���܂����B");
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