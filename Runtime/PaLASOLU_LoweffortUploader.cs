using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using VRC.SDKBase;

namespace PaLASOLU
{
    public class PaLASOLU_LoweffortUploader : MonoBehaviour, IEditorOnly
    {
        public PlayableDirector director;
        public GameObject directorObject;
        
#if UNITY_EDITOR
        [CustomEditor(typeof(PaLASOLU_LoweffortUploader))]
        public class LfUploaderEditor : Editor
        {
            bool advancedSettings = false;

            public override void OnInspectorGUI()
            {
                PaLASOLU_LoweffortUploader uploader = (PaLASOLU_LoweffortUploader)target;
                EditorGUILayout.HelpBox("このスクリプトとPlayable Directorコンポーネントが同じGameObjectに付いている場合、適切な処理をしてアップロードを行います。", MessageType.Info);

                if (advancedSettings = EditorGUILayout.Foldout(advancedSettings, "高度な設定"))
                {
                    uploader.director = EditorGUILayout.ObjectField("PlayableDirector", uploader.director, typeof(PlayableDirector), true) as PlayableDirector;
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