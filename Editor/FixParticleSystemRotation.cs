using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

namespace PaLASOLU
{
	[InitializeOnLoad]
	public class FixParticleSystemRotation
	{
		private static HashSet<int> previousInstanceIDs;
		private static bool fixRotate = false;
		private static bool isQueued;
		private const string menuPath = "Tools/PaLASOLU/Extensions/Fix rotation for \"Create Particle System\"";

		private class StartUpData : ScriptableSingleton<StartUpData>
		{
			private int count;
			public bool IsStartUp() => count++ == 0;
		}

		static FixParticleSystemRotation()
		{
			previousInstanceIDs = GetCurrentHierarchyIDs();
			Menu.SetChecked(menuPath, fixRotate);
			EditorApplication.hierarchyChanged += OnHierarchyChanged;

			if (!StartUpData.instance.IsStartUp()) return;
			EditorApplication.delayCall += Load;
		}

		private static void Load()
		{
			fixRotate = !string.IsNullOrEmpty(EditorUserSettings.GetConfigValue(menuPath));
			Menu.SetChecked(menuPath, fixRotate);
			EditorApplication.delayCall -= Load;
		}

		[MenuItem(menuPath, priority = 210)]
		private static void FixRotationParticleSystem()
		{
			fixRotate = !fixRotate;
			Menu.SetChecked(menuPath, fixRotate);
			EditorUserSettings.SetConfigValue(menuPath, fixRotate ? "true" : null);
		}

		static void OnHierarchyChanged()
		{
			if (!fixRotate) return;
			if (isQueued) return;

			isQueued = true;
			EditorApplication.delayCall += ProcessHierarchyChanges;

		}
		private static void ProcessHierarchyChanges()
		{
			isQueued = false;

			HashSet<int> currentIDs = GetCurrentHierarchyIDs();
			HashSet<int> newIDs = currentIDs.Except(previousInstanceIDs).ToHashSet();

			foreach (int id in newIDs)
			{
				GameObject obj = EditorUtility.InstanceIDToObject(id) as GameObject;
				if (obj == null) continue;

				ParticleSystem ps = obj.GetComponent<ParticleSystem>();
				if (ps == null) continue;

				Regex regex = new Regex("Particle System(?: \\(\\d+\\))?");
				if (!regex.IsMatch(obj.name)) continue;

				ParticleSystemRenderer renderer = obj.GetComponent<ParticleSystemRenderer>();
				if (renderer != null && renderer.sharedMaterial != null)
				{
					string materialName = renderer.sharedMaterial.name;
					if (materialName != ("Default-ParticleSystem")) continue;
				}

				if (PrefabUtility.IsPartOfPrefabInstance(obj)) continue;

				// 条件をすべて通ったら修正
				Undo.RecordObject(obj.transform, "Fix ParticleSystem Rotation");

				Vector3 rot = obj.transform.localEulerAngles;
				rot.x = 0f;
				obj.transform.localEulerAngles = rot;
			}

			previousInstanceIDs = currentIDs;
		}

		private static HashSet<int> GetCurrentHierarchyIDs()
		{
			return GameObject.FindObjectsOfType<GameObject>()
				.Where(go => go.scene.IsValid())
				.Select(go => go.GetInstanceID())
				.ToHashSet();
		}
	}
}