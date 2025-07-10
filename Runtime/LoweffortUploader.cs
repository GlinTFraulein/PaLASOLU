using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using VRC.SDKBase;
using PaLASOLU;

namespace PaLASOLU
{
	[AddComponentMenu("PaLASOLU/PaLASOLU Low-effort Uploader")]
	[HelpURL("https://glintfraulein.info/docs/PaLASOLU/Document/LoweffortUploader")]
	[DisallowMultipleComponent]
	public class LoweffortUploader : MonoBehaviour, IEditorOnly
	{
		public PlayableDirector director;
		public TimelineAsset timeline;
		public bool generateAudioObject = true;
		public bool isAffectedAudioVolume = false;
		const string bannerPath = "Packages/info.glintfraulein.palasolu//Image/PaLASOLU_Banner.png";
		static Texture banner = null;

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
			void OnEnable()
			{
				banner = AssetDatabase.LoadAssetAtPath<Texture>(bannerPath);
			}

			public override void OnInspectorGUI()
			{
				DrawBanner();

				GUILayout.Space(8);

				LoweffortUploader uploader = (LoweffortUploader)target;
				EditorGUILayout.HelpBox("このスクリプトとPlayable Directorコンポーネントが同じGameObjectに付いている場合、適切な処理をしてアップロードを行います。\n誤操作防止のために、このスクリプトが付いている場合は、Playable Directorを削除できません。", MessageType.Info);

				if (advancedSettings = EditorGUILayout.Foldout(advancedSettings, "高度な設定"))
				{
					uploader.director = EditorGUILayout.ObjectField("PlayableDirector", uploader.director, typeof(PlayableDirector), true) as PlayableDirector;
					uploader.generateAudioObject = EditorGUILayout.Toggle("Generate Audio object", uploader.generateAudioObject);
					uploader.isAffectedAudioVolume = EditorGUILayout.Toggle("Affect AudioTrack Volume ", uploader.isAffectedAudioVolume);
				}

				if (GUI.changed)
				{
					if (uploader.director != null) uploader.timeline = uploader.director.playableAsset as TimelineAsset;
					EditorUtility.SetDirty(uploader);
				}
			}

			private void DrawBanner()
			{
				if (banner == null)
				{
					Debug.Log("[PaLASOLU] Internal Error : banner is null.");
					return;
				}


				float inspectorWidth = EditorGUIUtility.currentViewWidth;
				float maxWidth = 512f;
				float displayWidth = Mathf.Min(inspectorWidth - 20f, maxWidth); // -20fはマージン分
				
				float aspect = (float)banner.height / banner.width;
				float displayHeight = displayWidth * aspect;

				float xOffset = (inspectorWidth - displayWidth) * 0.5f;
				Rect rect = GUILayoutUtility.GetRect(displayWidth, displayHeight, GUILayout.ExpandWidth(false));
				
				rect.x = xOffset;

				GUI.DrawTexture(rect, banner, ScaleMode.ScaleToFit);
			}
		}
#endif

	}
}