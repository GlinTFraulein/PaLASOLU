using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEditor;
using UnityEngine;
using UnityEngine.Playables;

namespace PaLASOLU
{
    [InitializeOnLoad]
    internal static class AvatarValidationHarmonyPatch
    {
        static AvatarValidationHarmonyPatch()
        {
            EditorApplication.delayCall += ApplyHarmonyPatch;
        }

        private static void ApplyHarmonyPatch()
        {
            var harmony = new Harmony(typeof(AvatarValidationHarmonyPatch).FullName);
            var avatarValidationType = AccessTools.TypeByName("VRC.SDK3.Validation.AvatarValidation");

            if (avatarValidationType == null)
            {
                LogMessageSimplifier.PaLog(5, "VRC.SDK3.Validation.AvatarValidation type not found. Harmony patch will not be applied.");
                return;
            }

            var findIllegalComponents = AccessTools.Method(avatarValidationType, "FindIllegalComponents", new[] { typeof(UnityEngine.GameObject) });
            if (findIllegalComponents == null)
            {
                LogMessageSimplifier.PaLog(5, "FindIllegalComponents method not found in VRC.SDK3.Validation.AvatarValidation. Harmony patch will not be applied.");
                return;
            }

            var patchMethod = AccessTools.Method(typeof(AvatarValidationHarmonyPatch), nameof(FindIllegalComponentsPostfix));

            harmony.Patch(findIllegalComponents, postfix: new HarmonyMethod(patchMethod));
        }

        private static void FindIllegalComponentsPostfix(GameObject target, ref IEnumerable<Component> __result)
        {
            if (__result == null) return;

            // Filter out PlayableDirector with LoweffortUploader component attached
            __result = __result.Where(c =>
            {
                if (c is PlayableDirector director)
                {
                    return !director.gameObject.GetComponent<LoweffortUploader>();
                }

                return true; // Keep other components
            });
        }
    }
}