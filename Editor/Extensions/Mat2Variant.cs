/*
 * Mat2Variant
 * 
 * Click `Tools/PaLASOLU/Extensions/Mat2Variant` to open this window.
 *
 * zlib License
 * 
 * Copyright (c) 2023 anatawa12
 * 
 */

#nullable enable

using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace PaLASOLU
{
	internal class Mat2Variant : EditorWindow
	{

		[MenuItem("Tools/PaLASOLU/Extensions/Mat2Variant", priority = 303)]
		static void Create() => GetWindow<Mat2Variant>("Material to Variant");

		public Vector2 scrollPos;
		public GameObject? referencePrefab;
		public Material?[] materials = Array.Empty<Material?>();
		public DefaultAsset? variantsFolder;

		private SerializedObject _serializedObject = null!;
		private SerializedProperty _referencePrefabProperty = null!;
		private SerializedProperty _materialsProperty = null!;
		private SerializedProperty _variantsFolderProperty = null!;

		private void OnEnable()
		{
			_serializedObject = new SerializedObject(this);
			_referencePrefabProperty = _serializedObject.FindProperty(nameof(referencePrefab));
			_materialsProperty = _serializedObject.FindProperty(nameof(materials));
			_variantsFolderProperty = _serializedObject.FindProperty(nameof(variantsFolder));
		}

		private void OnGUI()
		{
			EditorGUILayout.HelpBox("Material to Variant は、1つのPrefabを基に、マテリアルを差し替えた複数のPrefab Variantを生成します。", MessageType.None);

			scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
			_serializedObject.Update();
			EditorGUILayout.PropertyField(_referencePrefabProperty);
			EditorGUILayout.PropertyField(_materialsProperty);
			EditorGUILayout.PropertyField(_variantsFolderProperty);
			_serializedObject.ApplyModifiedProperties();

			var hasErrors = false;

			var variantsFolderPath = variantsFolder == null ? null : AssetDatabase.GetAssetPath(variantsFolder);
			if (variantsFolderPath == null || !AssetDatabase.IsValidFolder(variantsFolderPath))
			{
				EditorGUILayout.HelpBox("Variants Folder に、生成したPrefab Variantの保存先フォルダを設定してください！", MessageType.Error);
				hasErrors = true;
			}

			// check if the prefab is valid
			if (referencePrefab == null || PrefabUtility.GetPrefabAssetType(referencePrefab) == PrefabAssetType.NotAPrefab)
			{
				EditorGUILayout.HelpBox("Reference Prefab に、基準となるPrefabを設定してください！", MessageType.Error);
				hasErrors = true;
			}

			EditorGUI.BeginDisabledGroup(hasErrors);
			if (GUILayout.Button("Prefab Variant 生成！"))
			{
				CreateVariants();
			}
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndScrollView();
		}

		private void CreateVariants()
		{
			if (referencePrefab == null) throw new InvalidOperationException();
			if (variantsFolder == null) throw new InvalidOperationException();
			var variantsFolderPath = AssetDatabase.GetAssetPath(variantsFolder);
			if (string.IsNullOrEmpty(variantsFolderPath) || PrefabUtility.GetPrefabAssetType(referencePrefab) == PrefabAssetType.NotAPrefab)
				throw new InvalidOperationException();

			AssetDatabase.StartAssetEditing();
			try
			{
				if (EditorUtility.DisplayCancelableProgressBar("Material to Variant", "Creating variants...", 0f)) return;
				var existingNames = new HashSet<string>();

				for (var index = 0; index < materials.Length; index++)
				{
					var material = materials[index];
					if (material == null) continue;

					var baseName = material.name;
					baseName = baseName
						.Replace('\\', '_')
						.Replace('/', '_')
						.Replace(':', '_')
						.Replace('*', '_')
						.Replace('?', '_')
						.Replace('"', '_')
						.Replace('<', '_')
						.Replace('>', '_')
						.Replace('|', '_');

					var variantName = baseName;

					if (!existingNames.Add(variantName))
					{
						var increment = 1;
						for (; ; )
						{
							variantName = baseName + "_" + increment++;
							if (existingNames.Add(variantName)) break;
						}
					}

					var progress = (float)index / materials.Length;
					if (EditorUtility.DisplayCancelableProgressBar("Material to Variant", variantName, progress)) return;

					var instance = (GameObject)PrefabUtility.InstantiatePrefab(referencePrefab);
					var renderer = instance.GetComponentInChildren<Renderer>();
					if (renderer != null)
					{
						renderer.sharedMaterial = material;
						EditorUtility.SetDirty(renderer);
						PrefabUtility.RecordPrefabInstancePropertyModifications(renderer);
					}

					var variantPath = System.IO.Path.Combine(variantsFolderPath, variantName + ".prefab");
					PrefabUtility.SaveAsPrefabAsset(instance, variantPath);
					LogMessageSimplifier.PaLog(0, $"{variantPath} 内にPrefab Variantを生成しました。");
					DestroyImmediate(instance);
				}
			}
			finally
			{
				EditorUtility.ClearProgressBar();
				AssetDatabase.StopAssetEditing();
			}

			AssetDatabase.SaveAssets();
			AssetDatabase.Refresh();
		}
	}
}