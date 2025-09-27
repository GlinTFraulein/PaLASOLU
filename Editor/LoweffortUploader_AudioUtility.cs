using nadena.dev.ndmf;
using PaLASOLU;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;

[assembly: ExportsPlugin(typeof(LoweffortUploaderCore))]

namespace PaLASOLU
{
	public enum FadeDirection
	{
		In,
		Out
	}

	public class ClipFadeInfo
	{
		public FadeDirection Direction;
		public double Duration;
		public AnimationCurve Curve;
	}

	public partial class LoweffortUploaderCore : Plugin<LoweffortUploaderCore>
	{
		public AudioClip ModifyClip(AudioClip audio, TimelineClip nowClip, TimelineAsset timeline)
		{
			double startTime = nowClip.clipIn;
			double duration = nowClip.duration;
			double timeScale = nowClip.timeScale;

			int channels = audio.channels;
			int frequency = audio.frequency;
			int samples = audio.samples;

			float[] data = new float[audio.samples * channels];
			audio.GetData(data, 0);

			bool isModified = false;

			//CutClip
			if (startTime != 0 || System.Math.Abs(duration * timeScale - audio.length) > 0.001)
			{
				isModified = true;

				int startSample = Mathf.FloorToInt((float)startTime * frequency);
				int sampleLength = Mathf.FloorToInt((float)(duration * timeScale) * frequency);

				float[] slicedData = new float[sampleLength * channels];
				System.Array.Copy(data, startSample * channels, slicedData, 0, sampleLength * channels);

				data = slicedData;
				samples = data.Length / channels;
			}

			//SpeedClip
			if (timeScale != 1)
			{
				isModified = true;

				int speedSample = Mathf.FloorToInt(samples / (float)timeScale);
				float[] speedData = new float[speedSample * channels];

				for (int i = 0; i < speedSample; i++)
				{
					float srcIndex = (float)(i * timeScale);
					int idxFloor = (int)srcIndex;
					int idxCeil = Mathf.Min(idxFloor + 1, samples - 1);
					float t = srcIndex - idxFloor;

					for (int ch = 0; ch < channels; ch++)
					{
						float a = data[idxFloor * channels + ch];
						float b = data[idxCeil * channels + ch];
						speedData[i * channels + ch] = Mathf.Lerp(a, b, t); // 線形補間
					}
				}

				data = speedData;
				samples = data.Length / channels;
			}

			//FadeClip
			ClipFadeInfo fadeIn = GetFadeInfo(nowClip, FadeDirection.In);
			ClipFadeInfo fadeOut = GetFadeInfo(nowClip, FadeDirection.Out);

			if (fadeIn.Duration != -1 || fadeOut.Duration != -1)
			{
				isModified = true;

				int fadeInSamples = Mathf.RoundToInt((float)(fadeIn.Duration * frequency));
				int fadeOutSamples = Mathf.RoundToInt((float)(fadeOut.Duration * frequency));

				//fadein
				for (int i = 0; i < fadeInSamples && i < samples; i++)
				{
					float t = (float)i / Mathf.Max(1, fadeInSamples);
					float gain = fadeIn.Curve.Evaluate(t);
					for (int ch = 0; ch < channels; ch++)
					{
						int idx = i * channels + ch;
						data[idx] *= gain;
					}
				}

				//fadeout
				for (int i = 0; i < fadeOutSamples && i < samples; i++)
				{
					int sampleIndex = samples - fadeOutSamples + i;
					if (sampleIndex < 0 || sampleIndex >= samples) continue;

					float t = (float)i / Mathf.Max(1, fadeOutSamples);
					float gain = fadeOut.Curve.Evaluate(t);
					for (int ch = 0; ch < channels; ch++)
					{
						int idx = sampleIndex * channels + ch;
						data[idx] *= gain;
					}
				}
			}

			if (isModified)
			{
				int modifiedLength = data.Length / channels;

				AudioClip modifiedClip = AudioClip.Create(
					"Modified_" + audio.name,
					modifiedLength,
					channels,
					frequency,
					false
				);
				modifiedClip.SetData(data, 0);//dataをなおす必要が出てくる

				return AudioSavebyAsset(modifiedClip, timeline);
			}
			else return audio;
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

		public static AudioClip AudioSavebyAsset(AudioClip audio, TimelineAsset timeline)
		{
			float[] buffer = new float[audio.samples * audio.channels];
			audio.GetData(buffer, 0);

			//相対パスを得る
			string timelinePath = AssetDatabase.GetAssetPath(timeline);
			string directoryPath = Path.GetDirectoryName(timelinePath);
			string saveDirectory = directoryPath + "/(PaLASOLU)";
			if (!Directory.Exists(saveDirectory)) Directory.CreateDirectory(saveDirectory);

			string uniqueName = SetUniqueName(audio.name);

			return ExportAndImport(buffer, audio.frequency, audio.channels, saveDirectory + $"/{uniqueName}.wav");
		}

		public static ClipFadeInfo GetFadeInfo(TimelineClip clip, FadeDirection dir)
		{
			double duration;
			AnimationCurve curve;

			if (dir == FadeDirection.In)
			{
				duration = clip.easeInDuration > 0 ? clip.easeInDuration : clip.blendInDuration;
				curve = ResolveCurve(clip, dir, clip.mixInCurve, true);
			}
			else
			{
				duration = clip.easeOutDuration > 0 ? clip.easeOutDuration : clip.blendOutDuration;
				curve = ResolveCurve(clip, dir, clip.mixOutCurve, false);
			}

			return new ClipFadeInfo
			{
				Direction = dir,
				Duration = duration,
				Curve = curve
			};
		}

		private static AnimationCurve ResolveCurve(TimelineClip clip, FadeDirection dir, AnimationCurve manualCurve, bool isIn)
		{
			bool isEaseInAuto = clip.mixInCurve == null;
			bool isEaseOutAuto = clip.mixOutCurve == null;

			bool isAuto = dir == FadeDirection.In ? isEaseInAuto : isEaseOutAuto;

			if (isAuto) return isIn ? AnimationCurve.EaseInOut(0, 0, 1, 1) : AnimationCurve.EaseInOut(0, 1, 1, 0);
			else return manualCurve ?? (isIn ? AnimationCurve.Linear(0, 0, 1, 1) : AnimationCurve.Linear(0, 1, 1, 0));
		}
	}
}