using UnityEditor;
using UnityEngine;

namespace PaLASOLU
{
	public class PaLASOLURuntimeUtility
	{
#if UNITY_EDITOR
		internal static void DrawBanner()
		{
			const string bannerPath = "Packages/info.glintfraulein.palasolu//Image/PaLASOLU_Banner.png";
			Texture banner = AssetDatabase.LoadAssetAtPath<Texture>(bannerPath);

			if (banner == null)
			{
				Debug.Log("[PaLASOLU] Internal Error : banner is null.");
				return;
			}


			float inspectorWidth = EditorGUIUtility.currentViewWidth;
			float maxWidth = 512f;
			float displayWidth = Mathf.Min(inspectorWidth - 20f, maxWidth); // -20fはマージン分

			float aspect = (float)banner.height / banner.width;
			float displayHeight = displayWidth * aspect;

			float xOffset = (inspectorWidth - displayWidth) * 0.5f;
			Rect rect = GUILayoutUtility.GetRect(displayWidth, displayHeight, GUILayout.ExpandWidth(false));

			rect.x = xOffset;

			GUI.DrawTexture(rect, banner, ScaleMode.ScaleToFit);

			GUILayout.Space(8);
		}
#endif
	}
}