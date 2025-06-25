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
				Debug.LogWarning("[PaLASOLU] �x�� : Low-effort Uploader���A�^�b�`����GameObject�ɂ́APlayableDirector�R���|�[�l���g�����݂��܂���I �ォ��蓮�Œǉ�����ꍇ�́A���x�Ȑݒ肩��ǉ����Ă��������B");
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
				EditorGUILayout.HelpBox("���̃X�N���v�g��Playable Director�R���|�[�l���g������GameObject�ɕt���Ă���ꍇ�A�K�؂ȏ��������ăA�b�v���[�h���s���܂��B", MessageType.Info);

				if (advancedSettings = EditorGUILayout.Foldout(advancedSettings, "���x�Ȑݒ�"))
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
				float displayWidth = Mathf.Min(inspectorWidth - 20f, maxWidth); // -20f�̓}�[�W����
				
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