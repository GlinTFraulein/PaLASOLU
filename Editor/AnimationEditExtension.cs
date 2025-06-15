#if UNITY_EDITOR
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
                newKey.outTangent= float.PositiveInfinity;
            }

            return self.AddKey(newKey);
        }
    }
}
#endif