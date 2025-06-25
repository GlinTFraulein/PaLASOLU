using UnityEngine;

namespace PaLASOLU
{
	public class LogMessageSimplifier
	{
		public static void PaLog(int num, string message)
		{
			string returnMessage = "[PaLASOLU] ";
			if (num == 0) returnMessage += "���O(����) : ";
			else if (num == 1) returnMessage += "�x�� : ";
			else if (num == 2) returnMessage += "�G���[ : ";
			else
			{
				returnMessage += "(��������������� GlinTFraulein �ɕ񍐁I) ";

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