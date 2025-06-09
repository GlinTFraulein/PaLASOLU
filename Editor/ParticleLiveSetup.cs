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
            GUILayout.Label("パーティクルライブ用フォルダの新規作成", EditorStyles.boldLabel);
            rootFolderName = EditorGUILayout.TextField("フォルダ名(楽曲名を推奨)", rootFolderName);

            if (GUILayout.Button("フォルダを作成"))
            {
                if (rootFolderName == string.Empty)
                {
                    Debug.LogError("[PaLASOLU] エラー : フォルダ名がありません。");
                    return;
                }

                CreateFolders(rootFolderName);
            }

            IsShowAdvancedSettings = EditorGUILayout.Foldout(IsShowAdvancedSettings, "高度な設定");
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
                Debug.Log("[PaLASOLU] ログ(正常) : " + savePath + "フォルダを作りました。");
            }
            else
            {
                Debug.LogWarning("[PaLASOLU] 警告 : " + savePath + "フォルダは既に存在します。新しいフォルダは作られません。");
            }

            string assetPath = Path.Combine(savePath, rootFolderName);

            if (File.Exists(assetPath + "_timeline.playable"))
            {
                Debug.LogWarning("[PaLASOLU] 警告 : " + assetPath + "_timeline.playable ファイルは既に存在します。新しいファイルは作られません。");
            }
            else
            {
                var playable = ScriptableObject.CreateInstance<TimelineAsset>();
                Debug.Log("[PaLASOLU] テストログ " + (playable == null).ToString());
                AssetDatabase.CreateAsset(playable, assetPath + "_timeline.playable");
                Debug.Log("[PaLASOLU] ログ(正常) : " + assetPath + "_timeline.playable を作りました。");
            }

            AssetDatabase.Refresh();
        }
    }
}
#endif