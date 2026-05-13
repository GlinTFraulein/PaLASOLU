using UnityEditor;
using UnityEngine;


namespace PaLASOLU
{
	public class WorldFixedSetter : EditorWindow
	{
		const string avatarMenuPath = "Packages/info.glintfraulein.palasolu/Runtime/Prefab/PaLASOLU_v2_Prefab.prefab";
		const string playablePath = "Packages/info.glintfraulein.palasolu/Runtime/Prefab/PaLASOLU_v2_Playable.prefab";

		[MenuItem("Tools/PaLASOLU/Extensions/PaLASOLU Avatar Menu (World Fixed)", priority = 210)]
		static void SetPaLASOLUAvatarMenu()
		{
			GameObject avatarMenuPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(avatarMenuPath);
			GameObject avatarMenuObject = PrefabUtility.InstantiatePrefab(avatarMenuPrefab) as GameObject;
			avatarMenuObject.name = "PaLASOLU_AvatarMenu";

			Selection.activeGameObject = avatarMenuObject;
			EditorUtility.SetDirty(avatarMenuObject);
		}

		[MenuItem("Tools/PaLASOLU/Extensions/PaLASOLU Prefab", priority = 211)]
		static void SetPaLASOLUPrefab()
		{
			GameObject palasoluPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(playablePath);
			GameObject palasoluObject = PrefabUtility.InstantiatePrefab(palasoluPrefab) as GameObject;
			palasoluObject.name = "PaLASOLU_Playable";

			Selection.activeGameObject = palasoluObject;
			EditorUtility.SetDirty(palasoluObject);
		}
	}
}