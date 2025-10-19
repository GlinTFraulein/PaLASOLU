using System.Collections.Generic;
using UnityEngine;

namespace PaLASOLU
{
	public class AudioTrackVolumeData : ScriptableObject
	{
		public List<AudioTrackVolumeEntity> entities = new();
	}

	[System.Serializable]
	public class AudioTrackVolumeEntity
	{
		public int instanceID;
		public float volume;
	}
}