#if UNITY_EDITOR
using nadena.dev.ndmf;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[assembly: ExportsPlugin(typeof(PaLASOLU.PresetApplierforlilToonCore))]

namespace PaLASOLU
{
	public class PresetApplierforlilToonCore : Plugin<PresetApplierforlilToonCore>
	{
		private static System.Reflection.MethodInfo _applyMethod;

		protected override void Configure()
		{
			InPhase(BuildPhase.Transforming).Run("Apply LilToon Preset", ctx =>
			{
				GameObject avatarRoot = ctx.AvatarRootObject;
				PresetApplierforlilToon applier = avatarRoot.GetComponentsInChildren<PresetApplierforlilToon>(true).FirstOrDefault();

				if (applier == null /*|| applier.liltoonPreset == null*/) return;
				ApplyPreset(ctx, applier);
			});
		}

		private void ApplyPreset(BuildContext ctx, PresetApplierforlilToon applier)
		{
			Renderer[] renderers = ctx.AvatarRootObject.GetComponentsInChildren<Renderer>(true);
			Dictionary<Material, Material> matCache = new Dictionary<Material, Material>();

			foreach (Renderer renderer in renderers)
			{
				Material[] materials = renderer.sharedMaterials;
				bool changed = false;

				for (int i = 0; i < materials.Length; i++)
				{
					Material mat = materials[i];
					if (mat == null) continue;
					if (!IsLiltoon(mat.shader)) continue;

					if (!matCache.TryGetValue(mat, out Material mappedMat))
					{
						mappedMat = new Material(mat);
						mappedMat.name = $"{mat.name}_applied";

						ApplyLiltoonPreset(mappedMat/*, applier.liltoonPreset*/);
						matCache.Add(mat, mappedMat);
					}

					materials[i] = mappedMat;
					changed = true;
				}

				if (changed)
				{
					renderer.sharedMaterials = materials;
				}
			}
		}

		private bool IsLiltoon(Shader shader)
		{
			if (shader == null) return false;
			return shader.name.Contains("lilToon");
		}

		private void ApplyLiltoonPreset(Material mat/*, Object preset*/)
		{/*
			if (_applyMethod == null)
			{
				_applyMethod = preset.GetType().GetMethod(
					"Apply",
					System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.Public
				);
			}

			if (_applyMethod != null)
			{
				_applyMethod.Invoke(preset, new object[] { mat });
			}
			else
			{
				LogMessageSimplifier.PaLog(4, $"lilToon preset ApplyToMaterial not found");
			}
			*/
			mat.SetFloat("_LightMinLimit", 0);
		}
	}
}
#endif
