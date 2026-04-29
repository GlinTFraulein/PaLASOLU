using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using VRC.SDKBase;

namespace PaLASOLU
{
	[AddComponentMenu("PaLASOLU/PaLASOLU Low-effort Uploader")]
	[HelpURL("https://glintfraulein.info/PaLASOLU/Document/LoweffortUploader")]
	[DisallowMultipleComponent]
	public class LoweffortUploader : MonoBehaviour, IEditorOnly
	{
		public PlayableDirector director;
		public TimelineAsset timeline;
		public bool generateAvatarMenu = false;
		public bool generateAudioObject = true;
		public bool isAffectedAudioVolume = true;
		public bool isFixedAllowRoll = true;

		//private Dictionary<int, double> audioVolume;	将来的にやる

#if UNITY_EDITOR
		private void Reset()
		{
			director = GetComponent<PlayableDirector>();
			if (director == null)
			{
				Debug.LogWarning("[PaLASOLU] 警告 : Low-effort UploaderをアタッチしたGameObjectには、PlayableDirectorコンポーネントが存在しません！ 後から手動で追加する場合は、高度な設定から追加してください。");
			}
		}

		[CustomEditor(typeof(LoweffortUploader))]
		public class LfUploaderEditor : Editor
		{
			bool advancedSettings = false;


			public override void OnInspectorGUI()
			{
				PaLASOLURuntimeUtility.DrawBanner();

				LoweffortUploader uploader = (LoweffortUploader)target;
				EditorGUILayout.HelpBox("このスクリプトとPlayable Directorコンポーネントが同じGameObjectに付いている場合、適切な処理をしてアップロードを行います。\n誤操作防止のために、このスクリプトが付いている場合は、Playable Directorを削除できません。", MessageType.Info);

				if (advancedSettings = EditorGUILayout.Foldout(advancedSettings, "高度な設定"))
				{
					uploader.generateAvatarMenu = EditorGUILayout.Toggle("Generate Avatar Menu", uploader.generateAvatarMenu);
					uploader.director = EditorGUILayout.ObjectField("PlayableDirector", uploader.director, typeof(PlayableDirector), true) as PlayableDirector;
					uploader.generateAudioObject = EditorGUILayout.Toggle("Generate Audio object", uploader.generateAudioObject);
					uploader.isAffectedAudioVolume = EditorGUILayout.Toggle("Affect AudioTrack Volume ", uploader.isAffectedAudioVolume);
					uploader.isFixedAllowRoll = EditorGUILayout.Toggle("Fix Allow Roll", uploader.isFixedAllowRoll);
				}

				if (GUI.changed)
				{
					if (uploader.director != null) uploader.timeline = uploader.director.playableAsset as TimelineAsset;
					EditorUtility.SetDirty(uploader);
				}
			}
		}
#endif

	}
}