using System;
using UnityEditor;
using UnityEngine;
using VRC.SDKBase;

namespace PaLASOLU
{
	[AddComponentMenu("PaLASOLU/PaLASOLU PresetApplier for lilToon")]
	public class PresetApplierforlilToon : MonoBehaviour, IEditorOnly
	{
#if UNITY_EDITOR

#if PaLASOLU_LILTOON
		public lilToonPreset lilToonPreset;
		public SpecialSetting[] specialSettings = Array.Empty<SpecialSetting>();
		public Renderer[] excludes = Array.Empty<Renderer>();

		[Serializable]
		public class SpecialSetting
		{
			public lilToonPreset specialPreset;
			public Renderer[] specialRenderers;
		}

		private SerializedObject _serializedObject = null;
		private SerializedProperty _specialsProperty = null;
		private SerializedProperty _excludesProperty = null;
#endif

		private void SetSerialized()
		{
			_serializedObject = new SerializedObject(this);
			_specialsProperty = _serializedObject.FindProperty(nameof(specialSettings));
			_excludesProperty = _serializedObject.FindProperty(nameof(excludes));
		}

		[CustomEditor(typeof(PresetApplierforlilToon))]
		public class PresetApplierforlilToonEditor : Editor
		{
			bool advancedSettings = false;
			GUIContent specialsName = new GUIContent("例外設定");
			GUIContent excludesName = new GUIContent("除外設定");

			public override void OnInspectorGUI()
			{
				PaLASOLURuntimeUtility.DrawBanner();

#if PaLASOLU_LILTOON
				PresetApplierforlilToon pafliltoon = (PresetApplierforlilToon)target;
				pafliltoon.SetSerialized();

				EditorGUILayout.HelpBox("このスクリプトにlilToonのプリセットを設定すると、アップロード時にアバター内のlilToonが用いられているマテリアルに対して、プリセットを適用した状態に設定します。", MessageType.Info);

				pafliltoon.lilToonPreset = EditorGUILayout.ObjectField("lilToon Preset", pafliltoon.lilToonPreset, typeof(lilToonPreset), true) as lilToonPreset; //ScriptableObject


				if (advancedSettings = EditorGUILayout.Foldout(advancedSettings, "高度な設定"))
				{
					EditorGUI.indentLevel++;

					pafliltoon._serializedObject.Update();
					EditorGUILayout.PropertyField(pafliltoon._specialsProperty, specialsName, true);
					EditorGUILayout.PropertyField(pafliltoon._excludesProperty, excludesName, true);
					pafliltoon._serializedObject.ApplyModifiedProperties();

					EditorGUI.indentLevel--;
				}

				GameObject[] excludes = new GameObject[0];
#else
				EditorGUILayout.HelpBox("プロジェクトにlilToonがインポートされていません！このコンポーネントは無効化されます。", MessageType.Warning);
#endif
			}
		}

#endif
	}
}