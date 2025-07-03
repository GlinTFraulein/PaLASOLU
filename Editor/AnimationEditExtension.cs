#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

namespace PaLASOLU
{
	public static class AnimationEditExtension
	{
		public static int AddKeySetActive(this AnimationCurve self, float keyTime, bool keyType)
		{
			Keyframe newKey;
			if (keyType)
			{
				newKey = new Keyframe(keyTime, 1);
				newKey.inTangent = float.PositiveInfinity;
				newKey.outTangent = float.PositiveInfinity;
			}
			else
			{
				newKey = new Keyframe(keyTime, 0);
				newKey.inTangent = float.NegativeInfinity;
				newKey.outTangent = float.PositiveInfinity;
			}

			return self.AddKey(newKey);
		}

		public static EditorCurveBinding CreateIsActiveBinding(string path)
		{
			EditorCurveBinding binding = new EditorCurveBinding();
			binding.path = path;
			binding.type = typeof(GameObject);
			binding.propertyName = "m_IsActive";

			return binding;
		}
	}
}
#endif