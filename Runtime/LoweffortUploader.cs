using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using VRC.SDKBase;

namespace PaLASOLU
{
    [AddComponentMenu("PaLASOLU/PaLASOLU Low-effort Uploader")]
    [DisallowMultipleComponent]
    public class LoweffortUploader : MonoBehaviour, IEditorOnly
    {
        public PlayableDirector director;
        public bool generateAudioObject = true;

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
        }
#endif

    }
}