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
        protected override void Configure()
        {/*
            Sequence preProcess = InPhase(BuildPhase.Resolving);
            preProcess.Run("PaLASOLU LfUploader Pre Process", ctx =>
            {
                Debug.Log("[PaLASOLU] testlog Resolving Process ");
            });*/

            Sequence coreProcess = InPhase(BuildPhase.Transforming);
            coreProcess.Run("PaLASOLU LfUploder Core Process", ctx =>
            {
                LoweffortUploader obj = ctx?.AvatarRootObject.GetComponentInChildren<LoweffortUploader>();

                PlayableDirector director = obj?.director;
                if (director == null)
                {
                    Debug.LogError("[PaLASOLU] エラー : PaLASOLU Low-effort Uploader に PlayableDirector コンポーネントが設定されていません！Low-effort Uploaderの処理はスキップされます。\nPaLASOLU Setup Optimization からセットアップを行った場合、\"[楽曲名]_ParticleLive/WorldFixed/ParticleLive\" GameObject の、 PaLASOLU Low-eoofrt Uploader コンポーネント内の、「高度な設定」から Playable Director がNoneでないことを確認してください。");
                    return;
                }

                //RecordedClip Binding
                TimelineAsset timeline = director?.playableAsset as TimelineAsset;
                if (timeline == null)
                {
                    Debug.LogError("[PaLASOLU] エラー : PlayableDirector に Timeline Asset アセットが設定されていません！Low-effort Uploaderの処理はスキップされます。\nPaLASOLU Setup Optimization からセットアップを行った場合、\"[楽曲名]_ParticleLive/WorldFixed/ParticleLive\" GameObject の、 PlayableDirector コンポーネント内の、 Playable が None でないことを確認してください。");
                    return;
                }

                string timelinePath = AssetDatabase.GetAssetPath(timeline);

                Object[] subAssets = AssetDatabase.LoadAllAssetsAtPath(timelinePath);
                List<AnimationClip> recordedClips = new List<AnimationClip>();
                foreach (var asset in subAssets)
                {
                    if (asset is AnimationClip clip && clip.name.StartsWith("Recorded"))
                    {
                        recordedClips.Add(clip);
                    }
                }

                var bindings = new Dictionary<string, string>();
                List<AnimationClip> playAudioClips = new List<AnimationClip>();

                foreach (var track in timeline.GetOutputTracks())
                {
                    //Animation Handling Reserve
                    if (track is AnimationTrack)
                    {
                        AnimationTrack animationTrack = track as AnimationTrack;
                        var animator = director.GetGenericBinding(track) as Animator;
                        if (animator == null) continue;

                        var infiniteClip = animationTrack.infiniteClip;
                        if (infiniteClip == null || !infiniteClip.name.StartsWith("Recorded")) continue;

                        string animatorPath = GetGameObjectPath(animator.gameObject);
                        bindings[infiniteClip.name] = animatorPath;
                    }

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
                                Debug.LogWarning("[PaLASOLU] 警告 : " + nowClip.displayName + " にオーディオクリップが存在しません。");
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


                //Animator Setup
                AnimatorController controller = new AnimatorController();
                controller.name = timeline.name;
                obj.gameObject.GetComponent<Animator>().runtimeAnimatorController = controller;
                AnimationClip recordedClip = recordedClips.FirstOrDefault(c => c.name == "Recorded");
                controller.AddLayer(SetupNewLayerAndState(recordedClip));

                //Animator Setup 2 (for AudioSource GameObjects)
                foreach (AnimationClip playAudioClip in playAudioClips)
                {
                    if (playAudioClip == null) continue;
                    controller.AddLayer(SetupNewLayerAndState(playAudioClip));
                }

                /*
                //FUTURE WORK: Multi Animation/Animator Handling
                foreach (var binding in bindings)
                {
                    //Get Animator
                    string clipName = binding.Key;
                    string animatorPath = binding.Value;
                    string relativeAnimatorPath = GetRelativePath(animatorPath, GetGameObjectPath(obj.gameObject));

                    Transform animatorTransform = obj.transform.Find(relativeAnimatorPath == "." ? "" : relativeAnimatorPath);
                    Animator animator = animatorTransform.GetComponent<Animator>();

                    //Generate
                    AnimatorController controller = animator.runtimeAnimatorController as AnimatorController;
                    if (controller != null)
                    {
                        string saveDir = Path.GetDirectoryName(AssetDatabase.GetAssetPath(timeline)) + "/(PaLASOLU)";
                        Directory.CreateDirectory(saveDir);

                        string controllerPath = saveDir + ($"{director.gameObject.name}.controller");
                        controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
                        animator.runtimeAnimatorController = controller;
                    }

                    AnimationClip clip = recordedClips.FirstOrDefault(c => c.name == clipName);  //前から名称一致で探している
                    if (clip == null)
                    {
                        Debug.LogWarning($"[PaLASOLU] Recorded clipが見つかりません。: {clipName}");
                        continue;
                    }

                    /*
                    bool layerExists = controller.layers.Any(layer => layer.name == clipName);  //Object reference not set to an instance of an object
                    if (!layerExists)
                    {
                        //AddRecordedClipToAnimator(controller, clip, clipName);
                    }
                }*/


                if (director != null)
                {
                    if (PrefabUtility.IsPartOfPrefabInstance(director))
                    {
                        PrefabUtility.RecordPrefabInstancePropertyModifications(director);
                        Object.DestroyImmediate(director, true);
                        Debug.Log("[PaLASOLU] PlayableDirector を Prefab から削除しました。");
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

        public static string GetGameObjectPath(GameObject obj)
        {
            string path = obj.name;
            Transform current = obj.transform;

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