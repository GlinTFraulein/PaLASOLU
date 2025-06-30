#if UNITY_EDITOR

using System;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using PaLASOLU;

namespace PaLASOLU
{
	public class ParticleLiveSetup : EditorWindow
	{
		const string basePrefabPath = "Packages/info.glintfraulein.palasolu/Runtime/Prefab/PaLASOLU_Prefab.prefab";
		const string bannerPath = "Packages/info.glintfraulein.palasolu//Image/PaLASOLU_Banner.png";

		AudioClip particleLiveAudio = null;
		string rootFolderName = string.Empty;
		bool IsShowAdvancedSettings = false;
		bool selectFolder = false;
		bool moveAudioClip = false;
		bool existTimeline = false;
		bool timelineLockNotice = true;
		static Texture banner = null;

		[MenuItem("Tools/PaLASOLU/ParticleLive Setup")]
		static void Init()
		{
			GetWindow<ParticleLiveSetup>("Particle Live Setup");
		}

		private void OnEnable()
		{
			banner = AssetDatabase.LoadAssetAtPath<Texture>(bannerPath);
		}

		private void OnGUI()
		{
			GUILayout.Space(4);

			float windowWidth = position.width;
			float maxWidth = 1024f;
			float displayWidth = Mathf.Min(windowWidth - 10f, maxWidth);

			float aspect = (float)banner.height / banner.width;
			float displayHeight = displayWidth * aspect;

			float xOffset = (windowWidth - displayWidth) * 0.5f;
			Rect bannerRect = new Rect(xOffset, GUILayoutUtility.GetRect(0, displayHeight).y, displayWidth, displayHeight);
			
			GUI.DrawTexture(bannerRect, banner, ScaleMode.ScaleToFit);

			GUILayout.Space(8);

			GUILayout.Label("�p�[�e�B�N�����C�u�p�t�H���_�̐V�K�쐬", EditorStyles.boldLabel);
			rootFolderName = EditorGUILayout.TextField("�t�H���_��(�y�Ȗ��𐄏�)", rootFolderName);
			particleLiveAudio = EditorGUILayout.ObjectField("�y�ȃt�@�C��(�Ȃ��Ă���)", particleLiveAudio, typeof(AudioClip), false) as AudioClip;

			if (GUILayout.Button("�Z�b�g�A�b�v�I"))
			{
				if (rootFolderName == string.Empty)
				{
					LogMessageSimplifier.PaLog(2, "�t�H���_��������܂���B");
					return;
				}

				OptimizedSetup(rootFolderName);
			}

			IsShowAdvancedSettings = EditorGUILayout.Foldout(IsShowAdvancedSettings, "���x�Ȑݒ�");
			if (IsShowAdvancedSettings)
			{
				EditorGUI.indentLevel = 1;
				selectFolder = DrawResponsiveToggle("Select Folder Directory", selectFolder);
				moveAudioClip = DrawResponsiveToggle("Move AudioClip File to Particle Live Directory", moveAudioClip);
				timelineLockNotice = DrawResponsiveToggle("Timeline Lock Notice", timelineLockNotice);
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

				//���΃p�X�ϊ�
				string[] spritPath = Regex.Split(savePath, "/Assets/");
				savePath = "Assets/" + spritPath[1];
			}

			CreateDirectory(savePath);

			string timelinePath = Path.Combine(savePath, rootFolderName) + "_timeline.playable";

			if (File.Exists(timelinePath))
			{
				LogMessageSimplifier.PaLog(1, $"{timelinePath} �t�@�C���͊��ɑ��݂��܂��B�V�����t�@�C���͍��ꂸ�A������Timeline�f�[�^�ɕύX�������܂���B");
				existTimeline = true;
			}
			else
			{
				var playable = ScriptableObject.CreateInstance<TimelineAsset>();
				AssetDatabase.CreateAsset(playable, timelinePath);
				LogMessageSimplifier.PaLog(0, $"{timelinePath} �����܂����B");
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

			//Open Timeline window
			//WARNING : Using Internal API!!
			Type typeTimelineWindow = null;
			foreach (var asm in AppDomain.CurrentDomain.GetAssemblies())
			{
				typeTimelineWindow = asm.GetType("UnityEditor.Timeline.TimelineWindow");
				if (typeTimelineWindow != null) break;
			}

			if (typeTimelineWindow == null)
			{
				LogMessageSimplifier.PaLog(1, "TimelineWindow ��������܂���ł����BTimeline �p�b�P�[�W�����[�h����Ă��邩�m�F���Ă��������B");
			}
			else
			{
				var timelineWindow = EditorWindow.GetWindow(typeTimelineWindow, false);
			}

			Selection.activeGameObject = director.gameObject;
			if (timelineLockNotice)
			{
				EditorUtility.DisplayDialog(
					"[PaLASOLU] ParticleLive Setup",
					"Timeline�ł̍�Ƃ��n�߂�O�ɁATimeline �E�B���h�E�̉E��ɂ��� ���}�[�N�uLock�v�{�^�����N���b�N���Ă��������B\n" +
					"����ɂ��I������ Timeline ���Œ肳��A�A�j���[�V�����̋L�^�Ȃǂ�����ɍs����悤�ɂȂ�܂��B",
					"OK"
					);
			}
		}

		bool CreateDirectory(string path)
		{
			if (Directory.Exists(path))
			{
				LogMessageSimplifier.PaLog(1, $"{path} �t�H���_�͊��ɑ��݂��܂��B�V�����t�H���_�͍���܂���B");
				return false;
				
			}
			else
			{
				Directory.CreateDirectory(path);
				LogMessageSimplifier.PaLog(0, $"{path} �t�H���_�����܂����B");
				return true;
			}
		}

		bool DrawResponsiveToggle(string label, bool value)
		{
			float toggleWidth = 18f;
			float indentPerLevel = 8f;

			float viewWidth = EditorGUIUtility.currentViewWidth;

			float indentOffset = EditorGUI.indentLevel * indentPerLevel;

			Rect fullRect = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight);
			Rect labelRect = new Rect(fullRect.x + indentOffset, fullRect.y, fullRect.width - toggleWidth - indentOffset, fullRect.height);
			Rect toggleRect = new Rect(fullRect.xMax - toggleWidth, fullRect.y, toggleWidth, fullRect.height);

			GUI.Label(labelRect, label);
			value = GUI.Toggle(toggleRect, value, GUIContent.none);

			return value;
		}


	}
}
#endif