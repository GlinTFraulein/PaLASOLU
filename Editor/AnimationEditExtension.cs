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

		public static void AddKeySingleOnOff(this AnimationCurve self, float startTime, float endTime)
		{
			if (startTime != 0f) self.AddKeySetActive(0f, false);
			self.AddKeySetActive(startTime, true);
			self.AddKeySetActive(endTime, false);
		}

        public static void CopyCurveWithOffset(AnimationClip sourceClip, AnimationClip destClip, EditorCurveBinding binding, double offset)
        {
            var sourceCurve = AnimationUtility.GetEditorCurve(sourceClip, binding);
            if (sourceCurve == null) return;

            var newCurve = new AnimationCurve();
            foreach (var key in sourceCurve.keys)
            {
                float newTime = key.time + (float)offset;
                newCurve.AddKey(newTime, key.value);
            }

            AnimationUtility.SetEditorCurve(destClip, binding, newCurve);
        }
    }
}