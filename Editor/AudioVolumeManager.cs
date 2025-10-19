using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;

namespace PaLASOLU
{
	[CustomEditor(typeof(AudioPlayableAsset))]
	public class AudioVolumeManager : Editor
	{
		float volumeValue = 0.0f;
		public override void OnInspectorGUI()
		{
			serializedObject.Update();
			EditorGUI.BeginChangeCheck();

			base.OnInspectorGUI();

			if (EditorGUI.EndChangeCheck())
			{
				AudioPlayableAsset asset = (AudioPlayableAsset)target;
				TimelineAsset timeline = GetTimelineAsset(asset);
				TrackAsset track = FindParentTrack(asset);

				int instanceID = asset.GetInstanceID();

				LogMessageSimplifier.PaLog(3, $"instanceID : {instanceID}");

				//start time
				TimelineClip clip = FindClipManually(asset);
				double startTime = clip.start;

				//"volume" Property serch
				SerializedProperty clipProperties = serializedObject.FindProperty("m_ClipProperties");
				if (clipProperties != null)
				{
					SerializedProperty volumeProp = clipProperties.FindPropertyRelative("volume");
					if (volumeProp != null) volumeValue = volumeProp.floatValue;
					else LogMessageSimplifier.PaLog(4, "volumeProp is null");
				}
				else LogMessageSimplifier.PaLog(4, $"m_ClipProperties is not found");

				AudioTrackVolumeData volumeData = GetOrCreateVolumeData(timeline);


				var entity = volumeData.entities.Find(e => e.instanceID == instanceID);
				if (entity == null)
				{
					entity = new AudioTrackVolumeEntity
					{
						instanceID = instanceID,
						volume = volumeValue
					};
					volumeData.entities.Add(entity);
				}
				else
				{
					entity.volume = volumeValue;
				}

				EditorUtility.SetDirty(volumeData);
				AssetDatabase.SaveAssets();
			}
		}

		public static AudioTrackVolumeData GetOrCreateVolumeData(TimelineAsset timeline)
		{
			string timelinePath = AssetDatabase.GetAssetPath(timeline);
			string savePath = Path.GetDirectoryName(timelinePath);
			string timelineName = Path.GetFileNameWithoutExtension(timelinePath);

			string saveDirectory = savePath + "/(PaLASOLU)";
			if (!Directory.Exists(saveDirectory)) Directory.CreateDirectory(saveDirectory);
			string volumeDataPath = savePath + $"/(PaLASOLU)/{timelineName}_VolumeData.asset";

			AudioTrackVolumeData volumeData = AssetDatabase.LoadAssetAtPath<AudioTrackVolumeData>(volumeDataPath);
			if (volumeData == null)
			{
				volumeData = ScriptableObject.CreateInstance<AudioTrackVolumeData>();
				AssetDatabase.CreateAsset(volumeData, volumeDataPath);
				AssetDatabase.SaveAssets();
			}

			return volumeData;
		}

		TimelineAsset GetTimelineAsset(AudioPlayableAsset asset)
		{
			// ここは必ず渡せるわけではないので、もし必要なら PlayableDirector 経由で見つける
			return AssetDatabase.LoadAllAssetsAtPath(AssetDatabase.GetAssetPath(asset)).OfType<TimelineAsset>().FirstOrDefault();
		}

		TrackAsset FindParentTrack(AudioPlayableAsset asset)
		{
			if (TryFindClipAndTrack(asset, out TrackAsset track, out _)) return track;
			else return null;
		}

		TimelineClip FindClipManually(AudioPlayableAsset asset)
		{
			if (TryFindClipAndTrack(asset, out _, out TimelineClip clip)) return clip;
			else return null;
		}

		bool TryFindClipAndTrack(AudioPlayableAsset asset, out TrackAsset track, out TimelineClip clip)
		{
			track = null;
			clip = null;

			TimelineAsset timeline = GetTimelineAsset(asset);
			if (timeline == null) return false;

			foreach (var nowTrack in timeline.GetOutputTracks())
			{
				foreach (var nowClip in nowTrack.GetClips())
				{
					if (nowClip.asset == asset)
					{
						track = nowTrack;
						clip = nowClip;
						return true;
					}
				}
			}

			return false;
		}

		public static void CleanUpVolumeData(TimelineAsset timeline)
		{
			AudioTrackVolumeData volumeData = GetOrCreateVolumeData(timeline);
			var validKeys = new HashSet<int>();

			foreach (var track in timeline.GetOutputTracks())
			{
				if (track is AudioTrack)
				{
					foreach (var clip in track.GetClips().ToList())
					{
						AudioPlayableAsset asset = clip?.asset as AudioPlayableAsset;
						validKeys.Add(asset.GetInstanceID());
					}
				}
			}

			volumeData.entities.RemoveAll(e => !validKeys.Contains((e.instanceID)));
		}
	}
}