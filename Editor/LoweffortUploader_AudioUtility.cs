using nadena.dev.ndmf;
using PaLASOLU;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;

[assembly: ExportsPlugin(typeof(LoweffortUploaderCore))]

namespace PaLASOLU
{
	public partial class LoweffortUploaderCore : Plugin<LoweffortUploaderCore>
	{
		public AudioClip CutClipbyTime(AudioClip audio, double startTime, double duration, TimelineAsset timeline)
		{
			int channels = audio.channels;
			int frequency = audio.frequency;

			int startSample = Mathf.FloorToInt((float)startTime * frequency);
			int sampleLength = Mathf.FloorToInt((float)duration * frequency);

			float[] data = new float[audio.samples * channels];
			audio.GetData(data, 0);

			float[] slicedData = new float[sampleLength * channels];
			System.Array.Copy(data, startSample * channels, slicedData, 0, sampleLength * channels);

			AudioClip slicedClip = AudioClip.Create(
				"Sliced_" + audio.name,
				sampleLength,
				channels,
				frequency,
				false
			);

			slicedClip.SetData(slicedData, 0);
			float[] buffer = new float[slicedClip.samples * channels];
			slicedClip.GetData(buffer, 0);

			//相対パスを得る
			string timelinePath = AssetDatabase.GetAssetPath(timeline);
			string directoryPath = Path.GetDirectoryName(timelinePath);
			string saveDirectory = directoryPath + "/(PaLASOLU)";
			if (!Directory.Exists(saveDirectory)) Directory.CreateDirectory(saveDirectory);

			return ExportAndImport(buffer, slicedClip.frequency, slicedClip.channels, saveDirectory + $"/{slicedClip.name}.wav");
		}

		public static byte[] FromAudioClip(float[] samples, int sampleRate, int channels)
		{
			using MemoryStream stream = new MemoryStream();
			using BinaryWriter writer = new BinaryWriter(stream);

			int sampleCount = samples.Length;
			int byteRate = sampleRate * channels * 2; // 16bit = 2bytes

			// WAV Header
			writer.Write(System.Text.Encoding.ASCII.GetBytes("RIFF"));
			writer.Write(36 + sampleCount * 2); // ファイルサイズ - 8
			writer.Write(System.Text.Encoding.ASCII.GetBytes("WAVE"));

			// fmt chunk
			writer.Write(System.Text.Encoding.ASCII.GetBytes("fmt "));
			writer.Write(16); // PCM
			writer.Write((short)1); // フォーマットID = PCM
			writer.Write((short)channels);
			writer.Write(sampleRate);
			writer.Write(byteRate);
			writer.Write((short)(channels * 2)); // ブロックサイズ
			writer.Write((short)16); // ビット深度

			// data chunk
			writer.Write(System.Text.Encoding.ASCII.GetBytes("data"));
			writer.Write(sampleCount * 2);

			// sample data
			for (int i = 0; i < sampleCount; i++)
			{
				short intData = (short)(Mathf.Clamp(samples[i], -1f, 1f) * short.MaxValue);
				writer.Write(intData);
			}

			return stream.ToArray();
		}

		public static AudioClip ExportAndImport(float[] data, int sampleRate, int channels, string assetPath)
		{
			//save
			byte[] wavBytes = FromAudioClip(data, sampleRate, channels);
			File.WriteAllBytes(assetPath, wavBytes);

			// re-import
			AssetDatabase.ImportAsset(assetPath);
			AssetDatabase.Refresh();

			return AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
		}
	}
}