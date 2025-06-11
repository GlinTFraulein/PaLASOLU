using UnityEngine;
using nadena.dev.ndmf;
using PaLASOLU;
using UnityEngine.Timeline;
using System.Linq;
using UnityEditor.Animations;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using GluonGui.WorkspaceWindow.Views.WorkspaceExplorer;

[assembly: ExportsPlugin(typeof(LoweffortUploaderCore))]

namespace PaLASOLU
{
    public class LoweffortUploaderCore : Plugin<LoweffortUploaderCore>
    {
        protected override void Configure()
        {
            InPhase(BuildPhase.Transforming).Run("PaLASOLU LfUploder Core Process", ctx =>
            {
                var obj = ctx.AvatarRootObject.GetComponentInChildren<PaLASOLU_LoweffortUploader>();

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
                    }

                    
                    //Animator Setup
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

                        AnimationClip clip = recordedClips.FirstOrDefault(c => c.name == clipName);  //ëOÇ©ÇÁñºèÃàÍívÇ≈íTÇµÇƒÇ¢ÇÈ
                        if (clip == null)
                        {
                            Debug.LogWarning($"[PaLASOLU] Recorded clipÇ™å©Ç¬Ç©ÇËÇ‹ÇπÇÒÅB: {clipName}");
                            continue;
                        }

                        
                        bool layerExists = controller.layers.Any(layer => layer.name == clipName);  //Object reference not set to an instance of an object
                        if (!layerExists)
                        {
                            //AddRecordedClipToAnimator(controller, clip, clipName);
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
                return "."; // é©êg

            return fullPath.Substring(rootPath.Length + 1); // '/'ÇîÚÇŒÇ∑
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