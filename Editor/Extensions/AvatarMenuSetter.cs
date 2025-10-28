using UnityEditor;
using UnityEngine;


namespace PaLASOLU
{
	public class WorldFixedSetter : EditorWindow
	{
		const string avatarMenuPath = "Packages/info.glintfraulein.palasolu/Runtime/Prefab/PaLASOLU_v2_Prefab.prefab";

		[MenuItem("Tools/PaLASOLU/Extensions/PaLASOLU Avatar Menu (World Fixed)", priority = 210)]
		static void SetPaLASOLUAvatarMenu()
		{
			GameObject avatarMenuPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(avatarMenuPath);
			GameObject avatarMenuObject = PrefabUtility.InstantiatePrefab(avatarMenuPrefab) as GameObject;
			avatarMenuObject.name = "PaLASOLU_AvatarMenu";

			Selection.activeGameObject = avatarMenuObject;
			EditorUtility.SetDirty(avatarMenuObject);
		}
	}
}