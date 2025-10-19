/*
 * Img2Mat
 *
 * Click `Tools/PaLASOLU/Extensions/Img2Mat` to open this window.
 *
 * zlib License
 * 
 * Copyright (c) 2023 anatawa12
 * 
 */

#nullable enable

using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

namespace PaLASOLU
{
	internal class Img2Mat : EditorWindow
	{
		[MenuItem("Tools/PaLASOLU/Extensions/Img2Mat", priority = 302)]
		static void Create() => GetWindow<Img2Mat>("Image to Material");

		public Vector2 scrollPos;
		public Material? referenceMaterial;
		public Texture?[] textures = Array.Empty<Texture?>();
		public DefaultAsset? materialsFolder;

		private SerializedObject _serializedObject = null!;
		private SerializedProperty _referenceMaterialProperty = null!;
		private SerializedProperty _textureProperty = null!;
		private SerializedProperty _materialsFolderProperty = null!;

		private void OnEnable()
		{
			_serializedObject = new SerializedObject(this);
			_referenceMaterialProperty = _serializedObject.FindProperty(nameof(referenceMaterial));
			_textureProperty = _serializedObject.FindProperty(nameof(textures));
			_materialsFolderProperty = _serializedObject.FindProperty(nameof(materialsFolder));
		}

		private void OnGUI()
		{
			EditorGUILayout.HelpBox("Image to Materialは、1つのマテリアルを基に、テクスチャを差し替えた複数のマテリアルを生成します。", MessageType.None);

			scrollPos = EditorGUILayout.BeginScrollView(scrollPos);
			_serializedObject.Update();
			EditorGUILayout.PropertyField(_referenceMaterialProperty);
			EditorGUILayout.PropertyField(_textureProperty, true);
			EditorGUILayout.PropertyField(_materialsFolderProperty);
			_serializedObject.ApplyModifiedProperties();

			var hasErrors = false;

			// check if the folder is valid
			var materialsFolderPath = materialsFolder == null ? null : AssetDatabase.GetAssetPath(materialsFolder);
			if (string.IsNullOrEmpty(materialsFolderPath) || Directory.Exists(materialsFolderPath) == false)
			{
				EditorGUILayout.HelpBox("Materials Folder に、生成したマテリアルの保存先フォルダを設定してください！", MessageType.Error);
				hasErrors = true;
			}

			// check if _referenceMaterialProperty is valid
			if (referenceMaterial == null)
			{
				EditorGUILayout.HelpBox("Reference Matelial に、基準となるマテリアルを設定してください！", MessageType.Error);
				hasErrors = true;
			}

			EditorGUI.BeginDisabledGroup(hasErrors);
			if (GUILayout.Button("マテリアル生成！"))
			{
				CreateMaterials();
			}
			EditorGUI.EndDisabledGroup();
			EditorGUILayout.EndScrollView();
		}

		private void CreateMaterials()
		{
			if (materialsFolder == null) throw new InvalidOperationException("materialsFolder is null");
			if (referenceMaterial == null) throw new InvalidOperationException("referenceMaterial is null");
			var materialsFolderPath = AssetDatabase.GetAssetPath(materialsFolder);
			if (string.IsNullOrEmpty(materialsFolderPath) || Directory.Exists(materialsFolderPath) == false)
				throw new InvalidOperationException("materialsFolder is not a valid folder");

			AssetDatabase.StartAssetEditing();
			try
			{
				if (EditorUtility.DisplayCancelableProgressBar("Img2Mat", "Creating materials...", 0f)) return;
				var existingNames = new HashSet<string>();

				for (var index = 0; index < textures.Length; index++)
				{
					var texture = textures[index];
					if (texture == null) continue;
					var baseName = texture.name;

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

					var materialName = baseName;

					if (!existingNames.Add(materialName))
					{
						var increment = 1;
						for (; ; )
						{
							materialName = baseName + "_" + increment++;
							if (existingNames.Add(materialName)) break;
						}
					}

					var progress = (float)index / textures.Length;
					if (EditorUtility.DisplayCancelableProgressBar("Img2Mat", materialName, progress)) return;

					var material = new Material(referenceMaterial);
					material.name = materialName;
					material.mainTexture = texture;

					var materialPath = Path.Combine(materialsFolderPath, materialName + ".mat");
					AssetDatabase.CreateAsset(material, materialPath);
					LogMessageSimplifier.PaLog(0, $"{materialPath} 内にマテリアルを生成しました。");
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