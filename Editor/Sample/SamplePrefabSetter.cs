using UnityEditor;
using UnityEngine;


namespace PaLASOLU
{
	public class SamplePrefabSetter : EditorWindow
	{
		const string introductionPath = "Packages/info.glintfraulein.palasolu/Runtime/Sample/PaLASOLU Introduction/PaLASOLU Introduction_ParticleLive.prefab";

		[MenuItem("Tools/PaLASOLU/Sample/PaLASOLU Introduction", priority = 310)]
		static void SetPaLASOLUIntroduction()
		{
			GameObject introductionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(introductionPath);
			GameObject introductionObject = PrefabUtility.InstantiatePrefab(introductionPrefab) as GameObject;

			Selection.activeGameObject = introductionObject;
			EditorUtility.SetDirty(introductionObject);
		}
	}
}