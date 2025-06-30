using UnityEngine;

namespace PaLASOLU
{
	public class LogMessageSimplifier
	{
		public static void PaLog(int num, string message)
		{
			string returnMessage = "[PaLASOLU] ";
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

			if (num % 3 == 0) Debug.Log(returnMessage + message);
			else if (num % 3 == 1) Debug.LogWarning(returnMessage + message);
			else if (num % 3 == 2) Debug.LogError(returnMessage + message);

			return;
		}
	}
}