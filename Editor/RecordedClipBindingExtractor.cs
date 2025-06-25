using UnityEngine;
using UnityEditor;
using UnityEngine.Timeline;
using System.IO;
using System.Collections.Generic;
using UnityEngine.Playables;

namespace PaLASOLU
{
	public class RecordedClipBindingExtractor : EditorWindow
	{
		public static void ExportRecordedClipBindings(PlayableDirector director)
		{
			if (director == null)
			{
				LogMessageSimplifier.PaLog(1, "PlayableDirector������܂���B�����̓X�L�b�v����܂��B");
				return;
			}

			TimelineAsset timeline = director.playableAsset as TimelineAsset;
			if (timeline == null)
			{
                LogMessageSimplifier.PaLog(1, "PlayableDirector��Timeline������܂���B�����̓X�L�b�v����܂��B");
				return;
			}

			string timelinePath = AssetDatabase.GetAssetPath(timeline);
			if (timeline == null)
			{
				LogMessageSimplifier.PaLog(1, "Timeline�̃p�X���擾�ł��܂���B�����̓X�L�b�v����܂��B");
				return;
			}

			var subAssets = AssetDatabase.LoadAllAssetsAtPath(timelinePath);
			var recordedClips = new List<AnimationClip>();

			foreach (var asset in subAssets)
			{
				if (asset is AnimationClip clip && clip.name.StartsWith("Recorded"))
				{
					recordedClips.Add(clip);
				}
			}

			if (recordedClips.Count == 0)
			{
				LogMessageSimplifier.PaLog(1, "Timeline��Recorded Clip������܂���B�����̓X�L�b�v����܂��B");
				return;
			}

			//Animator Binding & Link
			var bindings = new List<ClipBindingInfo>();

			foreach (var track in timeline.GetOutputTracks())
			{
				if (track is AnimationTrack)
				{
					AnimationTrack animationTrack = track as AnimationTrack;
					var animator = director.GetGenericBinding(track) as Animator;
					if (animator == null) continue;

					var infiniteClip = animationTrack.infiniteClip;
					if (infiniteClip == null || !infiniteClip.name.StartsWith("Recorded")) continue;

					bindings.Add(new ClipBindingInfo
					{
						directorName = director.gameObject.name,
						animatorPath = GetGameObjectPath(animator.gameObject),
						recordedClipName = infiniteClip.name,
						timelineAssetPath = timelinePath
					});
					
				}
			}

			if (recordedClips.Count == 0)
			{
				LogMessageSimplifier.PaLog(1, "Recorded Clip�ɕR�Â���Animator������܂���B�����̓X�L�b�v����܂��B");
				return;
			}


			//Outstream
			string outputPath = $"Packages/info.glintfraulein.palasolu/Generated/RecordedClipBindingMap_{director.gameObject.name}.json";
			Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
			string json = JsonUtility.ToJson(new ClipBindingInfoList { bindings = bindings }, true);
			File.WriteAllText(outputPath, json);
			AssetDatabase.Refresh();

            LogMessageSimplifier.PaLog(0, $"[PaLASOLU] Recorded Clip Bindings ���o�͂��܂���: {outputPath}");
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


		[System.Serializable]
		public class ClipBindingInfo
		{
			public string directorName;
			public string animatorPath;
			public string recordedClipName;
			public string timelineAssetPath;
		}

		[System.Serializable]
		public class ClipBindingInfoList
		{
			public List<ClipBindingInfo> bindings;
		}
	}
}