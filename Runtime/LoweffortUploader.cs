using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.UIElements;
using VRC.SDKBase;
using PaLASOLU;

namespace PaLASOLU
{
	[AddComponentMenu("PaLASOLU/PaLASOLU Low-effort Uploader")]
	[DisallowMultipleComponent]
	public class LoweffortUploader : MonoBehaviour, IEditorOnly
	{
		public PlayableDirector director;
		public bool generateAudioObject = true;
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
				EditorGUILayout.HelpBox("このスクリプトとPlayable Directorコンポーネントが同じGameObjectに付いている場合、適切な処理をしてアップロードを行います。", MessageType.Info);

				if (advancedSettings = EditorGUILayout.Foldout(advancedSettings, "高度な設定"))
				{
					uploader.director = EditorGUILayout.ObjectField("PlayableDirector", uploader.director, typeof(PlayableDirector), true) as PlayableDirector;
					uploader.generateAudioObject = EditorGUILayout.Toggle("Generate Audio object", uploader.generateAudioObject);
				}

				if (GUI.changed)
				{
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