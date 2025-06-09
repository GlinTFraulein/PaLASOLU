#if UNITY_EDITOR
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using System.IO;
using System;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace PaLASOLU
{
    public class ParticleLiveSetup : EditorWindow
    {
        string rootFolderName = string.Empty;
        static bool IsShowAdvancedSettings = false;
        bool selectFolder = false;

        [MenuItem("Tools/PaLASOLU/ParticleLive Setup")]
        static void Init()
        {
            ParticleLiveSetup window = (ParticleLiveSetup)GetWindow(typeof(ParticleLiveSetup));
            window.Show();
        }

        private void OnGUI()
        {
            GUILayout.Label("�p�[�e�B�N�����C�u�p�t�H���_�̐V�K�쐬", EditorStyles.boldLabel);
            rootFolderName = EditorGUILayout.TextField("�t�H���_��(�y�Ȗ��𐄏�)", rootFolderName);

            if (GUILayout.Button("�t�H���_���쐬"))
            {
                if (rootFolderName == string.Empty)
                {
                    Debug.LogError("[PaLASOLU] �G���[ : �t�H���_��������܂���B");
                    return;
                }

                CreateFolders(rootFolderName);
            }

            IsShowAdvancedSettings = EditorGUILayout.Foldout(IsShowAdvancedSettings, "���x�Ȑݒ�");
            if (IsShowAdvancedSettings)
            {
                EditorGUI.indentLevel = 1;
                selectFolder = EditorGUILayout.Toggle("Select Folder Directory", selectFolder);
            }
        }

        void CreateFolders(string rootFolderName)
        {
            string savePath = Path.Combine("Assets\\ParticleLive", rootFolderName);

            if (selectFolder)
            {
                savePath = EditorUtility.OpenFolderPanel("Select Folder Directory", Application.dataPath, "ParticleLive");
                savePath = Path.Combine(savePath, rootFolderName);
            }
            

            if (!Directory.Exists(savePath))
            {
                Directory.CreateDirectory(savePath);
                Debug.Log("[PaLASOLU] ���O(����) : " + savePath + "�t�H���_�����܂����B");
            }
            else
            {
                Debug.LogWarning("[PaLASOLU] �x�� : " + savePath + "�t�H���_�͊��ɑ��݂��܂��B�V�����t�H���_�͍���܂���B");
            }

            string assetPath = Path.Combine(savePath, rootFolderName);

            if (File.Exists(assetPath + "_timeline.playable"))
            {
                Debug.LogWarning("[PaLASOLU] �x�� : " + assetPath + "_timeline.playable �t�@�C���͊��ɑ��݂��܂��B�V�����t�@�C���͍���܂���B");
            }
            else
            {
                var playable = ScriptableObject.CreateInstance<TimelineAsset>();
                Debug.Log("[PaLASOLU] �e�X�g���O " + (playable == null).ToString());
                AssetDatabase.CreateAsset(playable, assetPath + "_timeline.playable");
                Debug.Log("[PaLASOLU] ���O(����) : " + assetPath + "_timeline.playable �����܂����B");
            }

            AssetDatabase.Refresh();
        }
    }
}
#endif