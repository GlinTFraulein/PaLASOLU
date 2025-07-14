using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace PaLASOLU
{
	[InitializeOnLoad]
	public class FixParticleSystemRotation
	{
		private static List<int> previousInstanceIDs;
		private static bool fixRotate = true;
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
			if (!fixRotate)
			{
				previousInstanceIDs = GetCurrentHierarchyIDs();
				return;
			}

			var currentIDs = GetCurrentHierarchyIDs();
			var newIDs = currentIDs.Except(previousInstanceIDs).ToList();

			foreach (var id in newIDs)
			{
				var obj = EditorUtility.InstanceIDToObject(id) as GameObject;
				if (obj == null) continue;

				var ps = obj.GetComponent<ParticleSystem>();
				if (ps == null) continue;
				if (!obj.name.StartsWith("Particle System")) continue;

				var renderer = obj.GetComponent<ParticleSystemRenderer>();
				if (renderer != null && renderer.sharedMaterial != null)
				{
                    var materialName = renderer.sharedMaterial.name;
					if (materialName != ("Default-ParticleSystem")) continue;
				}

				if (PrefabUtility.IsPartOfPrefabInstance(obj)) continue;

				// 条件をすべて通ったら修正
				Undo.RecordObject(obj.transform, "Fix ParticleSystem Rotation");

				var rot = obj.transform.localEulerAngles;
				rot.x = 0f;
				obj.transform.localEulerAngles = rot;
			}

			previousInstanceIDs = currentIDs;
		}

		static List<int> GetCurrentHierarchyIDs()
		{
			return GameObject.FindObjectsOfType<GameObject>()
				.Where(go => go.scene.IsValid())
				.Select(go => go.GetInstanceID())
				.ToList();
		}
	}
}