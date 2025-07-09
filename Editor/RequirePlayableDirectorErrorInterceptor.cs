#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;
using PaLASOLU;

namespace PaLASOLU
{
	[InitializeOnLoad]
	public static class RequireComponentErrorInterceptor
	{
		static RequireComponentErrorInterceptor()
		{
			Application.logMessageReceived += OnLogMessage;
		}

		static void OnLogMessage(string condition, string stackTrace, LogType type)
		{
			if (type == LogType.Error && condition.Contains("Can't remove PlayableDirector because LoweffortUploader"))
			{
				LogMessageSimplifier.PaLog(2, "あなたは Playable Director を削除しようとしましたが、削除対象の PlayableDirector は PaLASOLU によって保護されています！もし VRChat へのアップロードであれば、 VRCSDK のエラーを無視してアップロードしてください。\nもし、 Playable Director を削除する必要がある場合は、「PaLASOLU Low-effot Uploader」コンポーネントを先に削除してください。");
				EditorUtility.DisplayDialog(
					"[PaLASOLU] Low-effort Uploader Alert",
					"もし VRChat へのアップロードであれば、 VRCSDK のエラーを無視してアップロードしてください！\n\n" + 
					"あなたは Playable Director を削除しようとしましたが、削除対象の Playable Director は PaLASOLU によって保護されています。",
					"OK"
				);
			}
		}
	}
}
#endif
