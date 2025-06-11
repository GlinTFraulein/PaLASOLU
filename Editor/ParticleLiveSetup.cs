#if UNITY_EDITOR

using UnityEngine;
using UnityEditor;
using System.IO;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace PaLASOLU
{
    public class ParticleLiveSetup : EditorWindow
    {
        const string basePrefabPath = "Packages/info.glintfraulein.palasolu/Runtime/Prefab/PaLASOLU_Prefab.prefab";
        
        AudioClip particleLiveAudio = null;
        string rootFolderName = string.Empty;
        bool IsShowAdvancedSettings = false;
        bool selectFolder = false;
        bool moveAudioClip = false;
        bool existTimeline = false;

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
            particleLiveAudio = EditorGUILayout.ObjectField("�y�ȃt�@�C��(�Ȃ��Ă���)", particleLiveAudio, typeof(AudioClip), false) as AudioClip;

            if (GUILayout.Button("�Z�b�g�A�b�v�I"))
            {
                if (rootFolderName == string.Empty)
                {
                    Debug.LogError("[PaLASOLU] �G���[ : �t�H���_��������܂���B");
                    return;
                }

                OptimizedSetup(rootFolderName);
            }

            IsShowAdvancedSettings = EditorGUILayout.Foldout(IsShowAdvancedSettings, "���x�Ȑݒ�");
            if (IsShowAdvancedSettings)
            {
                EditorGUI.indentLevel = 1;
                selectFolder = EditorGUILayout.Toggle("Select Folder Directory", selectFolder);
                moveAudioClip = EditorGUILayout.Toggle("Move AudioClip File to Particle Live Directory", moveAudioClip);
            }
        }

        void OptimizedSetup(string rootFolderName)
        {
            // Create Folders and Files
            string savePath = Path.Combine("Assets/ParticleLive", rootFolderName);

            if (selectFolder)
            {
                savePath = EditorUtility.OpenFolderPanel("Select Folder Directory", Application.dataPath, "ParticleLive");
                savePath = Path.Combine(savePath, rootFolderName);
            }

            CreateDirectory(savePath);
            CreateDirectory(savePath + "/(PaLASOLU)");

            string timelinePath = Path.Combine(savePath, rootFolderName) + "_timeline.playable";

            if (File.Exists(timelinePath))
            {
                Debug.LogWarning("[PaLASOLU] �x�� : " + timelinePath + " �t�@�C���͊��ɑ��݂��܂��B�V�����t�@�C���͍��ꂸ�A������Timeline�f�[�^�ɕύX�������܂���B");
                existTimeline = true;
            }
            else
            {
                var playable = ScriptableObject.CreateInstance<TimelineAsset>();
                AssetDatabase.CreateAsset(playable, timelinePath);
                Debug.Log("[PaLASOLU] ���O(����) : " + timelinePath + " �����܂����B");
                existTimeline = false;
            }

            if (moveAudioClip)
            {
                var audioClipPath = AssetDatabase.GetAssetPath(particleLiveAudio);
                AssetDatabase.MoveAsset(audioClipPath, Path.Combine(savePath, Path.GetFileName(audioClipPath)));
            }

            AssetDatabase.Refresh();


            //Setup Prefab Instance
            GameObject basePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(basePrefabPath);
            GameObject plInstance = PrefabUtility.InstantiatePrefab(basePrefab) as GameObject;
            plInstance.name = rootFolderName + "_ParticleLive";

            GameObject playableTarget = plInstance.transform.Find("WorldFixed/ParticleLive").gameObject;
            TimelineAsset timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(timelinePath);
            PlayableDirector director = playableTarget.GetComponent<PlayableDirector>();
            director.playableAsset = timeline;

            if (!existTimeline)
            {
                var animationTrack = timeline.CreateTrack<AnimationTrack>();
                var audioTrack = timeline.CreateTrack<AudioTrack>();
                director.SetGenericBinding(animationTrack, playableTarget.GetComponent<Animator>());

                if (particleLiveAudio != null)
                {
                    TimelineClip audioClipOnTrack = audioTrack.CreateClip<AudioPlayableAsset>();
                    audioClipOnTrack.displayName = Path.GetFileNameWithoutExtension(particleLiveAudio.name);
                    audioClipOnTrack.duration = particleLiveAudio.length;

                    AudioPlayableAsset audioAsset = audioClipOnTrack.asset as AudioPlayableAsset;
                    audioAsset.clip = particleLiveAudio;
                }
            }
        }

        bool CreateDirectory(string path)
        {
            if (Directory.Exists(path))
            {
                Debug.LogWarning("[PaLASOLU] �x�� : " + path + "�t�H���_�͊��ɑ��݂��܂��B�V�����t�H���_�͍���܂���B");
                return false;
                
            }
            else
            {
                Directory.CreateDirectory(path);
                Debug.Log("[PaLASOLU] ���O(����) : " + path + "�t�H���_�����܂����B");
                return true;
            }
        }

    }
}
#endif