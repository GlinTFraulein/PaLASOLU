using UnityEditor;
using UnityEngine;
using VRC.SDKBase;

namespace PaLASOLU
{
    public class PaLASOLU_LoweffortUploader : MonoBehaviour, IEditorOnly
    {

#if UNITY_EDITOR
        [CustomEditor(typeof(PaLASOLU_LoweffortUploader))]
        public class LfUploaderEditor : Editor
        {
            public override void OnInspectorGUI()
            {
                EditorGUILayout.HelpBox("このスクリプトとPlayable Directorコンポーネントが同じGameObjectに付いている場合、適切な処理をしてアップロードを行います。", MessageType.Info);
            }
        }
#endif

    }
}