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

		const string attentionPath = "Packages/info.glintfraulein.palasolu/Runtime/Sample/ParticleLive_Attention/ParticleLive_Attention.prefab";

		[MenuItem("Tools/PaLASOLU/Sample/パーティクルライブを観る際の注意事項", priority = 311)]
		static void SetParticleLiveAttention()
		{
			GameObject introductionPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(attentionPath);
			GameObject introductionObject = PrefabUtility.InstantiatePrefab(introductionPrefab) as GameObject;

			Selection.activeGameObject = introductionObject;
			EditorUtility.SetDirty(introductionObject);
		}
	}
}