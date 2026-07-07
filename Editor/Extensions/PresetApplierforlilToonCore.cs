#if PaLASOLU_LILTOON

using lilToon;
using nadena.dev.ndmf;
using nadena.dev.ndmf.fluent;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

[assembly: ExportsPlugin(typeof(PaLASOLU.PresetApplierforlilToonCore))]

namespace PaLASOLU
{
	public class PresetApplierforlilToonCore : Plugin<PresetApplierforlilToonCore>
	{
		private static System.Reflection.MethodInfo _applyMethod;
		private Dictionary<Material, Material> matCache = new Dictionary<Material, Material>();

		protected override void Configure()
		{
			PresetApplierforlilToon applier;

			InPhase(BuildPhase.Transforming).Run("Apply LilToon Preset", ctx =>
			{
				GameObject avatarRoot = ctx.AvatarRootObject;
				applier = avatarRoot.GetComponentsInChildren<PresetApplierforlilToon>(true).FirstOrDefault();

				if (applier == null /*|| applier.liltoonPreset == null*/) return;
				ApplyPreset(ctx, applier);
			});

			Sequence postProcess = InPhase(BuildPhase.Optimizing);
			postProcess.BeforePlugin("com.anatawa12.avatar-optimizer");
			postProcess.Run("Apply lilToon Preset Post Process", ctx =>
			{
				GameObject avatarRoot = ctx.AvatarRootObject;
				applier = avatarRoot.GetComponentsInChildren<PresetApplierforlilToon>(true).FirstOrDefault();
				UnityEngine.Object.DestroyImmediate(applier);
			});

		}

		private void ApplyPreset(BuildContext ctx, PresetApplierforlilToon applier)
		{
			Renderer[] renderers = ctx.AvatarRootObject.GetComponentsInChildren<Renderer>(true);
			List<Renderer> excludes = applier.excludes.ToList();

			//先に例外を処理し、例外を除外設定に入れる
			foreach (PresetApplierforlilToon.SpecialSetting setting in applier.specialSettings)
			{
				foreach (Renderer specialRenderer in setting.specialRenderers)
				{
					Material[] materials = specialRenderer.sharedMaterials;

					for (int i = 0; i < materials.Length; i++)
					{
						bool changed = TryApplyLiltoonPreset(ref materials[i], setting.specialPreset, false);
					}

					specialRenderer.sharedMaterials = materials;
					excludes.Add(specialRenderer);
				}
			}

			//除外以外
			foreach (Renderer renderer in renderers)
			{
				if (excludes.Contains(renderer)) continue;

				Material[] materials = renderer.sharedMaterials;
				bool changed = false;

				for (int i = 0; i < materials.Length; i++)
				{
					changed = TryApplyLiltoonPreset(ref materials[i], applier.lilToonPreset, true);
				}

				if (changed)
				{
					renderer.sharedMaterials = materials;
				}
			}
		}

		private bool TryApplyLiltoonPreset(ref Material mat, lilToonPreset preset, bool isCached)
		{
			if (mat == null) return false;
			if (!IsLiltoon(mat.shader)) return false;

			if (!matCache.TryGetValue(mat, out Material mappedMat))
			{

				mappedMat = new Material(mat);
				mappedMat.name = $"{mat.name}_applied";

				ApplyLiltoonPreset(ref mappedMat, preset);
				if (isCached) matCache.Add(mat, mappedMat);
			}

			mat = mappedMat;
			return true;
		}

		private bool IsLiltoon(Shader shader)
		{
			if (shader == null) return false;
			return shader.name.Contains("lilToon");
		}

		/* ApplyLiltoonPreset is used modified lilToon resorces. (lilToon/Assets/lilToon/Editor/lilToonEditorUtils.cs)
		* https://github.com/lilxyzw/lilToon/blob/master/Assets/lilToon/Editor/lilToonEditorUtils.cs
		* 
		*	MIT License
		*	
		*	Copyright (c) 2020-2024 lilxyzw
		*	
		*	Permission is hereby granted, free of charge, to any person obtaining a copy
		*	of this software and associated documentation files (the "Software"), to deal
		*	in the Software without restriction, including without limitation the rights
		*	to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
		*	copies of the Software, and to permit persons to whom the Software is
		*	furnished to do so, subject to the following conditions:
		*	
		*	The above copyright notice and this permission notice shall be included in all
		*	copies or substantial portions of the Software.
		*	
		*	THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
		*	IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
		*	FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
		*	AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
		*	LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
		*	OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
		*	SOFTWARE.
		*
		*/

		private void ApplyLiltoonPreset(ref Material material, lilToonPreset preset/*, bool ismulti*/)
		{
			if (material == null || preset == null) return;
			Undo.RecordObject(material, "Apply Preset");
			foreach (var f in preset.floats.Where(f => f.name == "_StencilPass"))
			{
				material.SetFloat(f.name, f.value);
			}
			if (preset.shader != null) material.shader = preset.shader;
			var shaderName = material.shader.name;
			bool isoutl = preset.outline == -1 ? lilShaderUtils.IsOutlineShaderName(shaderName) : (preset.outline == 1);
			bool istess = preset.tessellation == -1 ? lilShaderUtils.IsTessellationShaderName(shaderName) : (preset.tessellation == 1);

			bool islite = lilShaderUtils.IsLiteShaderName(shaderName);
			bool iscutout = lilShaderUtils.IsCutoutShaderName(shaderName);
			bool istransparent = lilShaderUtils.IsTransparentShaderName(shaderName);
			bool isrefr = lilShaderUtils.IsRefractionShaderName(shaderName);
			bool isblur = lilShaderUtils.IsBlurShaderName(shaderName);
			bool isfur = lilShaderUtils.IsFurShaderName(shaderName);
			bool isonepass = lilShaderUtils.IsOnePassShaderName(shaderName);
			bool istwopass = lilShaderUtils.IsTwoPassShaderName(shaderName);

			var renderingMode = RenderingMode.Opaque;

			//if(string.IsNullOrEmpty(preset.renderingMode) || !Enum.TryParse(preset.renderingMode, out renderingMode))
			if (string.IsNullOrEmpty(preset.renderingMode) || !Enum.IsDefined(typeof(RenderingMode), preset.renderingMode))
			{
				if (iscutout) renderingMode = RenderingMode.Cutout;
				if (istransparent) renderingMode = RenderingMode.Transparent;
				if (isrefr) renderingMode = RenderingMode.Refraction;
				if (isrefr && isblur) renderingMode = RenderingMode.RefractionBlur;
				if (isfur) renderingMode = RenderingMode.Fur;
				if (isfur && iscutout) renderingMode = RenderingMode.FurCutout;
				if (isfur && istwopass) renderingMode = RenderingMode.FurTwoPass;
			}
			else
			{
				renderingMode = (RenderingMode)Enum.Parse(typeof(RenderingMode), preset.renderingMode);
			}

			var transparentMode = TransparentMode.Normal;
			if (isonepass) transparentMode = TransparentMode.OnePass;
			if (!isfur && istwopass) transparentMode = TransparentMode.TwoPass;

			//lilMaterialUtils.SetupMaterialWithRenderingMode(material, renderingMode, transparentMode, isoutl, islite, istess, ismulti);
			if (preset.renderQueue != -2) material.renderQueue = preset.renderQueue;

			foreach (var c in preset.colors) material.SetColor(c.name, c.value);
			foreach (var v in preset.vectors) material.SetVector(v.name, v.value);
			foreach (var f in preset.floats) material.SetFloat(f.name, f.value);
			foreach (var t in preset.textures)
			{
				material.SetTexture(t.name, t.value);
				material.SetTextureOffset(t.name, t.offset);
				material.SetTextureScale(t.name, t.scale);
			}

			if (preset.outlineMainTex) material.SetTexture("_OutlineTex", material.GetTexture("_MainTex"));
		}
	}
}

#endif