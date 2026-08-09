using UnityEditor;

namespace PaLASOLU
{
	public class ParticlePackImporter
	{
		private const string PARTICLE_PACK_GUID = "2a4b655fa5dd58d40b545ce5e4846b23";

		[MenuItem("Tools/PaLASOLU/Extensions/Import PaLASOLU Particle Pack", priority = 209)]
		static void Import()
		{
			string particlePackPath = AssetDatabase.GUIDToAssetPath(PARTICLE_PACK_GUID);
			AssetDatabase.ImportPackage(particlePackPath, true);
		}
	}
}
