using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor;
using UnityEngine;

namespace PaLASOLU
{
	public class MaterialSeparater : EditorWindow
	{
		GameObject prefabAsset;
		string outputFolderName = "DuplicatedMaterials";

		[MenuItem("Tools/PaLASOLU/Extensions/MaterialSeparater", priority = 304)]
		static void OpenWindow()
		{
			var w = GetWindow<MaterialSeparater>("Material Separator");
			w.minSize = new Vector2(420, 140);
		}

		void OnGUI()
		{
			EditorGUILayout.HelpBox("Material Separaterは、Prefab内のMaterialを複製して差し替えます。", MessageType.None);
			EditorGUILayout.Space();

			prefabAsset = (GameObject)EditorGUILayout.ObjectField("Prefab (Asset):", prefabAsset, typeof(GameObject), false);
			outputFolderName = EditorGUILayout.TextField("出力フォルダ名:", outputFolderName);

			EditorGUILayout.Space();
			EditorGUILayout.HelpBox("注意: Prefabアセットが変更されます。念のためバージョン管理の差分を確認できるようにしてください。", MessageType.Warning);

			EditorGUILayout.Space();
			using (new EditorGUI.DisabledScope(prefabAsset == null || string.IsNullOrEmpty(outputFolderName)))
			{
				if (GUILayout.Button("開始 (Duplicate & Replace)"))
				{
					DoProcess();
				}
			}
		}

		void DoProcess()
		{
			string prefabPath = AssetDatabase.GetAssetPath(prefabAsset);
			if (string.IsNullOrEmpty(prefabPath))
			{
				Debug.LogError("選択されたオブジェクトはPrefabアセットではありません。ProjectウィンドウからPrefabを選んでください。");
				return;
			}

			// Prefab と同階層に出力フォルダを作成
			string prefabDir = Path.GetDirectoryName(prefabPath);
			string folderPath = Path.Combine(prefabDir, outputFolderName);

			if (!AssetDatabase.IsValidFolder(folderPath))
			{
				string parent = Path.GetDirectoryName(prefabDir);
				string newFolderName = Path.GetFileName(folderPath);
				AssetDatabase.CreateFolder(prefabDir, newFolderName);
			}

			// Load prefab contents for editing
			GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
			if (root == null)
			{
				Debug.LogError("Prefabのロードに失敗しました: " + prefabPath);
				return;
			}

			try
			{
				var map = new Dictionary<Material, Material>();
				var allGameObjects = root.GetComponentsInChildren<Transform>(true).Select(t => t.gameObject);

				foreach (var go in allGameObjects)
				{
					// Renderer系を処理
					var renderers = go.GetComponents<Renderer>();
					foreach (var r in renderers)
					{
						if (r == null) continue;
						var sharedMats = r.sharedMaterials;
						bool changed = false;
						for (int i = 0; i < sharedMats.Length; i++)
						{
							var mat = sharedMats[i];
							if (mat == null) continue;
							if (!map.TryGetValue(mat, out var dup))
							{
								dup = DuplicateMaterialAsset(mat, folderPath);
								map[mat] = dup;
							}
							sharedMats[i] = map[mat];
							changed = true;
						}
						if (changed)
						{
							r.sharedMaterials = sharedMats;
						}
					}

					// その他のMaterial参照をReflectionで探索
					var comps = go.GetComponents<Component>();
					foreach (var comp in comps)
					{
						if (comp == null) continue;
						var type = comp.GetType();
						if (typeof(Renderer).IsAssignableFrom(type)) continue;

						var props = type.GetProperties(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
						foreach (var p in props)
						{
							if (!p.CanRead || !p.CanWrite || p.GetIndexParameters().Length > 0) continue;

							try
							{
								if (p.PropertyType == typeof(Material))
								{
									var m = p.GetValue(comp, null) as Material;
									if (m == null) continue;
									if (!map.TryGetValue(m, out var dup))
									{
										dup = DuplicateMaterialAsset(m, folderPath);
										map[m] = dup;
									}
									p.SetValue(comp, map[m], null);
								}
								else if (p.PropertyType == typeof(Material[]))
								{
									var arr = p.GetValue(comp, null) as Material[];
									if (arr == null) continue;
									bool any = false;
									for (int i = 0; i < arr.Length; i++)
									{
										var m = arr[i];
										if (m == null) continue;
										if (!map.TryGetValue(m, out var dup))
										{
											dup = DuplicateMaterialAsset(m, folderPath);
											map[m] = dup;
										}
										arr[i] = map[m];
										any = true;
									}
									if (any) p.SetValue(comp, arr, null);
								}
							}
							catch { }
						}

						var fields = type.GetFields(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
						foreach (var f in fields)
						{
							try
							{
								if (f.FieldType == typeof(Material))
								{
									var m = f.GetValue(comp) as Material;
									if (m == null) continue;
									if (!map.TryGetValue(m, out var dup))
									{
										dup = DuplicateMaterialAsset(m, folderPath);
										map[m] = dup;
									}
									f.SetValue(comp, map[m]);
								}
								else if (f.FieldType == typeof(Material[]))
								{
									var arr = f.GetValue(comp) as Material[];
									if (arr == null) continue;
									bool any = false;
									for (int i = 0; i < arr.Length; i++)
									{
										var m = arr[i];
										if (m == null) continue;
										if (!map.TryGetValue(m, out var dup))
										{
											dup = DuplicateMaterialAsset(m, folderPath);
											map[m] = dup;
										}
										arr[i] = map[m];
										any = true;
									}
									if (any) f.SetValue(comp, arr);
								}
							}
							catch { }
						}
					}
				}

				PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
				AssetDatabase.SaveAssets();
				AssetDatabase.Refresh();

				LogMessageSimplifier.PaLog(0, $"[MaterialSeparater] 完了: 複製したMaterial数 {map.Count} フォルダ: {folderPath}");
			}
			catch (Exception ex)
			{
				LogMessageSimplifier.PaLog(2, "[MaterialSeparater] エラー: " + ex);
			}
			finally
			{
				PrefabUtility.UnloadPrefabContents(root);
			}
		}

		string MakeSafeFolderName(string name)
		{
			foreach (var c in Path.GetInvalidFileNameChars()) name = name.Replace(c, '_');
			name = name.Replace(' ', '_');
			return name;
		}

		Material DuplicateMaterialAsset(Material src, string targetFolder)
		{
			if (src == null) return null;

			string baseName = MakeSafeFolderName(src.name);
			string assetPath = Path.Combine(targetFolder, baseName + ".mat").Replace("\\", "/");
			assetPath = AssetDatabase.GenerateUniqueAssetPath(assetPath);

			Material newMat = new Material(src);
			newMat.name = baseName + "_dup";
			AssetDatabase.CreateAsset(newMat, assetPath);
			EditorUtility.SetDirty(newMat);
			return newMat;
		}
	}
}