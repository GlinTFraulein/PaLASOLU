using System.IO;
using UnityEngine;

namespace PaLASOLU
{
	public class FileAndPathEditExtension
	{
		public static string GetRelativePath(string fullPath, string rootPath)
		{
			if (!fullPath.StartsWith(rootPath))
				return null; // invalid

			if (fullPath == rootPath)
				return "."; // 自身

			return fullPath.Substring(rootPath.Length + 1); // '/'を飛ばす
		}

		public static string GetGameObjectPath(GameObject wantPassObject)
		{
			string path = wantPassObject.name;
			Transform current = wantPassObject.transform;

			while (current.parent != null)
			{
				current = current.parent;
				path = current.name + "/" + path;
			}

			return path;
		}

		public static string SetUniqueName(string name)
		{
			string uniqueId = System.Guid.NewGuid().ToString("N").Substring(0, 8);
			string uniqueName = $"{name}_{uniqueId}";

			return uniqueName;
		}

		public static bool CreateDirectory(string path)
		{
			if (Directory.Exists(path))
			{
				LogMessageSimplifier.PaLog(1, $"{path} フォルダは既に存在します。新しいフォルダは作られません。");
				return false;

			}
			else
			{
				Directory.CreateDirectory(path);
				LogMessageSimplifier.PaLog(0, $"{path} フォルダを作りました。");
				return true;
			}
		}
	}
}
