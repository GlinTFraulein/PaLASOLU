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
                EditorGUILayout.HelpBox("���̃X�N���v�g��Playable Director�R���|�[�l���g������GameObject�ɕt���Ă���ꍇ�A�K�؂ȏ��������ăA�b�v���[�h���s���܂��B", MessageType.Info);
            }
        }
#endif

    }
}