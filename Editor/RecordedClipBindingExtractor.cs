using UnityEngine;
using UnityEditor;
using UnityEngine.Timeline;
using System.IO;
using System.Collections.Generic;
using UnityEngine.Playables;
using UnityEngine.UIElements;

namespace PaLASOLU
{
	public class RecordedClipBindingExtractor : EditorWindow
	{
		public static void ExportRecordedClipBindings(PlayableDirector director)
		{
			if (director == null)
			{
				Debug.LogWarning("[PaLASOLU] 警告 : PlayableDirectorがありません。処理はスキップされます。");
				return;
			}

			TimelineAsset timeline = director.playableAsset as TimelineAsset;
			if (timeline == null)
			{
				Debug.LogWarning("[PaLASOLU] 警告 : PlayableDirectorにTimelineがありません。処理はスキップされます。");
				return;
			}

			string timelinePath = AssetDatabase.GetAssetPath(timeline);
			if (timeline == null)
			{
				Debug.LogWarning("[PaLASOLU] 警告 : Timelineのパスが取得できません。処理はスキップされます。");
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
				Debug.LogWarning("[PaLASOLU] 警告 : TimelineにRecorded Clipがありません。処理はスキップされます。");
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
				Debug.LogWarning("[PaLASOLU] 警告 : Recorded Clipに紐づいたAnimatorがありません。処理はスキップされます。");
				return;
			}


			//Outstream
			string outputPath = $"Packages/info.glintfraulein.palasolu/Generated/RecordedClipBindingMap_{director.gameObject.name}.json";
			Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
			string json = JsonUtility.ToJson(new ClipBindingInfoList { bindings = bindings }, true);
			File.WriteAllText(outputPath, json);
			AssetDatabase.Refresh();

			Debug.Log($"[PaLASOLU] Recorded Clip Bindings を出力しました: {outputPath}");
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