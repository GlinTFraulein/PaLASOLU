using System.Collections.Generic;
using UnityEngine;

namespace PaLASOLU
{
	public static class FixParticleSystemAllowRoll
	{
		/// <summary>
		/// ParticleSystemのAllow RollをOFFにする
		/// </summary>
		/// <param name="systems">対象ParticleSystem</param>
		/// <param name="destructive">
		/// true  : 破壊的変更（Editor用）
		/// false : 非破壊（NDMF用・Clone前提）
		/// </param>
		public static void FixAllowRoll(IEnumerable<ParticleSystem> systems, bool destructive)
		{
			if (systems == null) return;

			// 重複排除
			HashSet<ParticleSystem> uniqueSystems = new HashSet<ParticleSystem>();

			foreach (var ps in systems)
			{
				if (ps == null) continue;
				uniqueSystems.Add(ps);
			}

			foreach (var ps in uniqueSystems)
			{
				var renderer = ps.GetComponent<ParticleSystemRenderer>();
				if (renderer == null) continue;

				// すでにOFFならスキップ
				if (!renderer.allowRoll) continue;

				if (destructive)
				{
					UnityEditor.Undo.RecordObject(renderer, "Fix ParticleSystem Allow Roll");
				}
				renderer.allowRoll = false;

			}
		}
	}
}