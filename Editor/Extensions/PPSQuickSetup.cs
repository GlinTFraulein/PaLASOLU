using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.SceneManagement;

namespace PaLASOLU
{
	public class PPSQuickSetup : EditorWindow
	{
		[MenuItem("Tools/PaLASOLU/Extensions/PostProcessing Quick Setup", priority = 220)]
		static void PPSSetup()
		{
			Scene scene = SceneManager.GetActiveScene();
			int waterLayer = LayerMask.NameToLayer("Water");

			if (!scene.IsValid())
			{
				LogMessageSimplifier.PaLog(5, "No active scene.");
				return;
			}

			//Camera

			Camera camera = Object.FindFirstObjectByType<Camera>();
			GameObject cameraObject;

			if (camera == null)
			{
				cameraObject = new GameObject("Camera");
				cameraObject.AddComponent<Camera>();
			}
			else cameraObject = camera.gameObject;


			//PPS Layer

			PostProcessLayer layer = cameraObject.GetComponent<PostProcessLayer>();
			if (layer == null)
			{
				layer = cameraObject.AddComponent<PostProcessLayer>();
			}

			layer.volumeTrigger = camera.transform;
			layer.volumeLayer = 1 << waterLayer;


			//PPS Volume

			PostProcessVolume volume = Object.FindFirstObjectByType<PostProcessVolume>();
			GameObject volumeObject;

			if (volume == null)
			{
				volumeObject = new GameObject("PostProcess");
				Undo.RegisterCreatedObjectUndo(volumeObject, "Create PostProcess");
				volume = volumeObject.AddComponent<PostProcessVolume>();
			}
			else
			{
				volumeObject = volume.gameObject;

				if (volume.profile != null)
				{
					LogMessageSimplifier.PaLog(1, $"Scene上に、既にProfileがセットされたPost Process Volumeが存在します！以降の処理はスキップされます。");
					return;
				}
			}

			volumeObject.layer = waterLayer;
			volume.isGlobal = true;
			volume.priority = 0f;
			volume.weight = 1f;


			// PPS Profile

			string SaveDirectory;
			string profilePath;

			LoweffortUploader[] lfUploaders = Object.FindObjectsByType<LoweffortUploader>(FindObjectsSortMode.None);

			if (lfUploaders.Length == 1)
			{
				string timelineName = lfUploaders[0].name;
				string timelinePath = AssetDatabase.GetAssetPath(lfUploaders[0].timeline);
				SaveDirectory = Path.GetDirectoryName(timelinePath);
				profilePath = $"{SaveDirectory}/{lfUploaders[0].gameObject.name}_PPS.asset";
			}
			else
			{
				SaveDirectory = "Assets/ParticleLive/PPS";
				profilePath = $"{SaveDirectory}/{scene.name}_PPS.asset";
			}

			if (!Directory.Exists(SaveDirectory))
			{
				Directory.CreateDirectory(SaveDirectory);
				AssetDatabase.Refresh();
			}



			PostProcessProfile profile = AssetDatabase.LoadAssetAtPath<PostProcessProfile>(profilePath);

			if (profile == null)
			{
				profile = ScriptableObject.CreateInstance<PostProcessProfile>();
				AssetDatabase.CreateAsset(profile, profilePath);
				AssetDatabase.SaveAssets();
			}

			volume.sharedProfile = profile;
			EditorSceneManager.MarkSceneDirty(scene);

			Bloom bloom;
			//if (volume.sharedProfile.TryGetSettings(out bloom))
			//{
			bloom = profile.AddSettings<Bloom>();
			//}
			bloom.active = true;
			bloom.intensity.overrideState = true;
			bloom.intensity.value = 1f;


		}
	}
}
