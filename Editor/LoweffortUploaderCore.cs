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
                var obj = ctx.AvatarRootObject.GetComponentInChildren<LoweffortUploader>();

                if (obj != null)
                {
                    //RecordedClip Binding
                    TimelineAsset timeline = obj.director.playableAsset as TimelineAsset;
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

                    foreach (var track in timeline.GetOutputTracks())
                    {
                        if (track is AnimationTrack)
                        {
                            AnimationTrack animationTrack = track as AnimationTrack;
                            var animator = obj.director.GetGenericBinding(track) as Animator;
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
                            TimelineClip firstClip = clips.FirstOrDefault();
                            AudioPlayableAsset audioPlayableAsset = firstClip?.asset as AudioPlayableAsset;
                            AudioClip audioClip = audioPlayableAsset?.clip;

                            if (audioClip == null)
                            {
                                Debug.LogWarning("[PaLASOLU] 警告 : " + track.name + " にオーディオクリップが存在しません。");
                                continue;
                            }

                            if (track.GetClips().Count() > 1)
                            {
                                Debug.LogWarning("[PaLASOLU] 警告 : " + track.name + " にオーディオクリップが複数存在します。オーディオソースの追加は、最初のクリップに対してのみ行われます。");
                            }
                            if (track.GetClips().FirstOrDefault().start != 0)
                            {
                                Debug.LogWarning("[PaLASOLU] 警告 : " + track.name + " は、再生開始タイミングが " + track.GetClips().FirstOrDefault().start + "秒です。現在のバージョンのPaLASOLUは、再生開始タイミングの変更に対応していないため、アップロードしたパーティクルライブの音声がズレることがあります。");
                            }

                            
                            GameObject audioObject = new GameObject(audioClip.name);
                            audioObject.transform.parent = obj.transform;
                            AudioSource audioSource = audioObject.AddComponent<AudioSource>();
                            audioSource.clip = audioClip;
                        }
                    }

                    
                    //Animator Setup
                    AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>("Packages/info.glintfraulein.palasolu/Runtime/Animation/PaLASOLU_ParticleLiveAnimator.controller");
                    obj.gameObject.GetComponent<Animator>().runtimeAnimatorController = controller;
                    List<ChildAnimatorState> states = controller.layers[0].stateMachine.states.ToList();
                    ChildAnimatorState childState = states.Find(s => s.state.name == "Recorded");
                    AnimationClip recordedClip = recordedClips.FirstOrDefault(c => c.name == "Recorded");
                    if (childState.state != null) childState.state.motion = recordedClip;

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

                            string controllerPath = saveDir + ($"{obj.director.gameObject.name}.controller");
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


                    if (obj.director != null)
                    {
                        if (PrefabUtility.IsPartOfPrefabInstance(obj.director))
                        {
                            PrefabUtility.RecordPrefabInstancePropertyModifications(obj.director);
                            Object.DestroyImmediate(obj.director, true);
                            Debug.Log("[PaLASOLU] PlayableDirector を Prefab から削除しました。");
                        }
                        else
                        {
                            Object.DestroyImmediate(obj.director);
                        }
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

    }
}