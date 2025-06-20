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
        public string timelinePath;
        public Object[] subAssets;
        public List<AnimationClip> recordedClips;
        public Dictionary<string, string> bindings;
        public List<AnimationClip> playAudioClips;
    }

    public class LoweffortUploaderCore : Plugin<LoweffortUploaderCore>
    {
        /*
        LoweffortUploader obj;
        PlayableDirector director;
        TimelineAsset timeline;
        string timelinePath;
        Object[] subAssets;
        List<AnimationClip> recordedClips;
        Dictionary<string, string> bindings;
        List<AnimationClip> playAudioClips;
        */

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
                    Debug.Log("[PaLASOLU] ログ : PaLASOLU Low-effort Uploaderが存在しません。");
                    return;
                }

                PlayableDirector director = obj?.director;
                if (director == null)
                {
                    Debug.LogError("[PaLASOLU] エラー : PaLASOLU Low-effort Uploader に PlayableDirector コンポーネントが設定されていません！Low-effort Uploaderの処理はスキップされます。\nPaLASOLU Setup Optimization からセットアップを行った場合、\"[楽曲名]_ParticleLive/WorldFixed/ParticleLive\" GameObject の、 PaLASOLU Low-eoofrt Uploader コンポーネント内の、「高度な設定」から Playable Director がNoneでないことを確認してください。");
                    return;
                }

                //RecordedClip Binding
                lfuState.timeline = director?.playableAsset as TimelineAsset;
                TimelineAsset timeline = lfuState.timeline;
                if (timeline == null)
                {
                    Debug.LogError("[PaLASOLU] エラー : PlayableDirector に Timeline Asset アセットが設定されていません！Low-effort Uploaderの処理はスキップされます。\nPaLASOLU Setup Optimization からセットアップを行った場合、\"[楽曲名]_ParticleLive/WorldFixed/ParticleLive\" GameObject の、 PlayableDirector コンポーネント内の、 Playable が None でないことを確認してください。");
                    return;
                }

                //"Recorded" clips extraction
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

                //Animation Handling
                Dictionary<string, string> bindings = new Dictionary<string, string>();
                List<AnimationClip> playAudioClips = new List<AnimationClip>();

                foreach (var track in timeline.GetOutputTracks())
                {
                    if (track is AnimationTrack)
                    {
                        AnimationTrack animationTrack = track as AnimationTrack;
                        var animator = director.GetGenericBinding(track) as Animator;
                        if (animator == null) continue;

                        var infiniteClip = animationTrack.infiniteClip;
                        if (infiniteClip == null || !infiniteClip.name.StartsWith("Recorded")) continue;

                        //animatorPathはobj.gameObjectからの相対パスを取る
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

                LoweffortUploader obj = lfuState.lfUploader;

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
                                Debug.LogWarning("[PaLASOLU] 警告 : " + nowClip.displayName + " にオーディオクリップが存在しません。");
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
                            AnimationClip playAudioClip = new AnimationClip();
                            playAudioClip.name = audioObject.name;

                            EditorCurveBinding binding = new EditorCurveBinding();
                            binding.path = audioObject.name;
                            binding.type = typeof(GameObject);
                            binding.propertyName = "m_IsActive";

                            AnimationCurve curve = new AnimationCurve();
                            if ((float)nowClip.start != 0f) curve.AddKeySetActive(0f, false);
                            curve.AddKeySetActive((float)nowClip.start, true);
                            curve.AddKeySetActive((float)nowClip.end, false);

                            AnimationUtility.SetEditorCurve(playAudioClip, binding, curve);
                            playAudioClip.legacy = false;

                            lfuState.playAudioClips.Add(playAudioClip);
                        }
                    }
                }

                //Animator Setup (for AnimationClips)
                Dictionary<string, string> bindings = lfuState.bindings;

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

                    AnimationClip clip = lfuState.recordedClips.FirstOrDefault(c => c.name == clipName);  //前から名称一致で探している
                    if (clip == null)
                    {
                        Debug.LogWarning($"[PaLASOLU] Recorded clipが見つかりません。: {clipName}");
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

                foreach (AnimationClip playAudioClip in lfuState.playAudioClips)
                {
                    if (playAudioClip == null) continue;
                    rootController.AddLayer(SetupNewLayerAndState(playAudioClip));
                }

                //PlayableDirector Delete
                PlayableDirector director = lfuState.director;

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