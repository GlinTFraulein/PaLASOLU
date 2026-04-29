using UnityEditor;
using UnityEngine;
using VRC.SDKBase;

namespace PaLASOLU
{
	[AddComponentMenu("PaLASOLU/PaLASOLU PresetApplier for lilToon")]
	public class PresetApplierforlilToon : MonoBehaviour, IEditorOnly
	{
		public lilToonPreset lilToonPreset;

#if UNITY_EDITOR
		[CustomEditor(typeof(PresetApplierforlilToon))]
		public class PresetApplierforlilToonEditor : Editor
		{
			public override void OnInspectorGUI()
			{
				PaLASOLURuntimeUtility.DrawBanner();

				PresetApplierforlilToon pafliltoon = (PresetApplierforlilToon)target;
				EditorGUILayout.HelpBox("このスクリプトにlilToonのプリセットを設定すると、アップロード時にアバター内のlilToonが用いられているマテリアルに対して、プリセットを適用した状態に設定します。", MessageType.Info);
				EditorGUILayout.HelpBox("このスクリプトは現在仮実装です！うまく動作しない場合などは作者 GlinTFraulein に報告してください。", MessageType.Warning);

				pafliltoon.lilToonPreset = EditorGUILayout.ObjectField("lilToon Preset", pafliltoon.lilToonPreset, typeof(lilToonPreset), true) as lilToonPreset; //ScriptableObject
			}
		}

#endif
	}
}