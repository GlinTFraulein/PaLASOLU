using nadena.dev.ndmf;
using nadena.dev.ndmf.ui;
using UnityEditor.PackageManager;
using UnityEngine;
using UnityEngine.UIElements;

namespace PaLASOLU
{
	public class LogMessageSimplifier
	{
		private static string version;

		private static string Version
		{
			get
			{
				if (version == null)
				{
					var packageInfo = PackageInfo.FindForAssembly(typeof(LogMessageSimplifier).Assembly);
					version = packageInfo?.version ?? "Unknown";
				}

				return version;
			}
		}

		//const string version = "2.5.0-a";
		public static void PaLog(int num, string message, BuildContext context = null)
		{
			string returnMessage = $"[PaLASOLU {Version}]";
			if (num == 0) returnMessage += "ログ(正常) : ";
			else if (num == 1) returnMessage += "警告 : ";
			else if (num == 2) returnMessage += "エラー : ";
			else
			{
				returnMessage += "(これを見つけたら作者 GlinTFraulein に報告！) ";

				if (num == 3) returnMessage += "InternalLog : ";
				else if (num == 4) returnMessage += "InternalWarning : ";
				else if (num == 5) returnMessage += "InternalError : ";
			}

			returnMessage += message;

			if (num % 3 == 0) Debug.Log(returnMessage);
			else if (num % 3 == 1) Debug.LogWarning(returnMessage);
			else if (num % 3 == 2) Debug.LogError(returnMessage);

			if (context != null) PaNDMFLog(num, returnMessage);

			return;
		}

		public static void PaNDMFLog(int num, string message)
		{
			ErrorSeverity severity;
			switch (num)
			{
				case 0:
				case 3:
					severity = ErrorSeverity.Information;
					break;
				case 1:
				case 4:
					severity = ErrorSeverity.NonFatal;
					break;
				case 2:
					severity = ErrorSeverity.Error;
					break;
				default:
					severity = ErrorSeverity.InternalError;
					break;
			}

			ErrorReport.ReportError(new PaNDMFError(severity, message));
		}
	}

	public class PaNDMFError : IError
	{
		private ErrorSeverity severity;
		private string message;

		public ErrorSeverity Severity => severity;

		public PaNDMFError(ErrorSeverity severity, string message)
		{
			this.severity = severity;
			this.message = message;
		}

		public string ToMessage()
		{
			return message;
		}

		public void AddReference(ObjectReference obj)
		{
		}

		public VisualElement CreateVisualElement(ErrorReport report)
		{
			VisualElement container = new VisualElement();
			ErrorIcon icon = new ErrorIcon { Severity = severity };
			Label label = new Label(message);

			container.Add(icon);
			container.Add(label);

			return container;
		}
	}
}