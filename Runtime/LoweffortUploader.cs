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
        public GameObject directorObject;
        
#if UNITY_EDITOR
        [CustomEditor(typeof(LoweffortUploader))]
        public class LfUploaderEditor : Editor
        {
            bool advancedSettings = false;

            public override void OnInspectorGUI()
            {
                LoweffortUploader uploader = (LoweffortUploader)target;
                EditorGUILayout.HelpBox("���̃X�N���v�g��Playable Director�R���|�[�l���g������GameObject�ɕt���Ă���ꍇ�A�K�؂ȏ��������ăA�b�v���[�h���s���܂��B", MessageType.Info);

                if (advancedSettings = EditorGUILayout.Foldout(advancedSettings, "���x�Ȑݒ�"))
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