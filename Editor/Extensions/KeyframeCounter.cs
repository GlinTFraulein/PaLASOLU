/*
 * ---------------------------------------------------------
 * Sakuragear Animation Insight (ver.1.0)
 * Author : 来夢 (RaiM) / Sakuragear
 * 
 * キーフレーム数を自動解析し、しきい値に応じたメッセージを表示します。
 * 
 * 機能概要:
 *  - AnimationClip 内の全カーブを走査し、総キーフレーム数を集計
 *  - 閾値ごとに異なるセリフを返す
 *  - 結果を Console と Dialog で表示
 * 
 * 使用方法:
 *  1. メニューから [Tools → Sakuragear → Animation Insight] を実行
 *  2. 対象の AnimationClip を指定
 *  3. 「Count & Speak」ボタンを押して解析！
 * 
 * 補足:
 *  - ObjectReferenceCurve（Sprite切替など）もカウント対象
 * ---------------------------------------------------------
 * 
 * Modified by GlinTFraulein
 * Authorである来夢さんから、PaLASOLUへの搭載に関して直接許可を頂きました、ありがとうございます！
 * 
 */



using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;

namespace PaLASOLU
{
	public class KeyframeCounter : EditorWindow
	{
		Object clip;
		static string toolName = "Keyframe Counter";

		// 閾値
		const int T1 = 100;
		const int T2 = 300;
		const int T3 = 1000;
		const int T4 = 3000;
		const int T5 = 10000;
		const int T6 = 30000;
		const int T7 = 100000;

		// セリフ（自由に編集可）
		[SerializeField, TextArea(1, 3)] string line_lt100 = "まだまだこれから！";
		[SerializeField, TextArea(1, 3)] string line_lt300 = "いい感じ！この調子で製作を進めてみてね！";
		[SerializeField, TextArea(1, 3)] string line_lt1000 = "なかなかすごい！";
		[SerializeField, TextArea(1, 3)] string line_lt3000 = "パーティクルメインで演出しているすごい人達だと、だいたいこれくらいのキーフレーム数になるそうです。";
		[SerializeField, TextArea(1, 3)] string line_lt10000 = "PaLASOLUの作者です。一度そのパーティクルライブを見せてくれませんか？？";
		[SerializeField, TextArea(1, 3)] string line_lt30000 = "あなたは最強です。オブジェクトメインで演出しているすごい人たちだと、だいたいこれくらいのキーフレームになると思います。裏取りはありませんが……。";
		[SerializeField, TextArea(1, 3)] string line_lt100000 = "前バージョンではこれくらいのキーフレーム数で「異常！！！！！」ということにしていましたが、意外と出せるらしいです。何故なのか。";
		[SerializeField, TextArea(1, 3)] string line_ge100000 = "本当に異常！！！！！何をしたんですか？？？？？\n(もしこれが出て、バグっぽい場合は、報告していただけると助かります。そのレベルのキーフレーム数です……。)";

		[MenuItem("Tools/PaLASOLU/Extensions/Keyframe Counter", priority = 321)]
		static void Open() => GetWindow<KeyframeCounter>(toolName);

		void OnGUI()
		{
			EditorGUILayout.LabelField(toolName, EditorStyles.boldLabel);

			EditorGUILayout.Space(4);
			clip = EditorGUILayout.ObjectField("AnimationClip or Timeline", clip, typeof(Object), false);

			EditorGUILayout.Space(4);
			if (clip == null)
			{
				EditorGUILayout.HelpBox("AnimationClip または Timeline を指定してください！", MessageType.Warning);
			}
			else if (clip is AnimationClip)
			{
				EditorGUILayout.HelpBox("AnimationClipが指定されています。\n指定されたAnimationClip内のキーフレーム数を数えます。", MessageType.Info);
			}
			else if (clip is TimelineAsset)
			{
				EditorGUILayout.HelpBox("Timelineが指定されています。\n指定されたTimeline内の、\"Recorded\" AnimationClipのキーフレーム数を数えます。", MessageType.Info);
			}
			else
			{
				EditorGUILayout.HelpBox("AnimationClip または Timeline 以外が指定されています！", MessageType.Error);
			}

			bool canPushButton = clip is AnimationClip || clip is TimelineAsset;

			EditorGUILayout.Space(10);
			using (new EditorGUI.DisabledScope(!canPushButton))
			{
				if (GUILayout.Button("キーフレーム数を数える！", GUILayout.Height(30)))
				{
					int total = clip switch
					{
						AnimationClip animationClip => CountAllKeys(animationClip),
						TimelineAsset timeline => CountAllKeys(timeline),
						_ => 0
					};

					string line = PickLine(total);
					Debug.Log($"{toolName} {clip.name} : {total} keys → {line}");
					EditorUtility.DisplayDialog(toolName, $"クリップ名: {clip.name}\nキー総数: {total}\n\n{line}", "OK");
				}
			}
		}

		//AnimationClip
		static int CountAllKeys(AnimationClip c)
		{
			int total = 0;

			// Transform以外の通常カーブ
			Dictionary<string, HashSet<float>> otherCurveKeys = new Dictionary<string, HashSet<float>>();

			// Transform用
			Dictionary<string, HashSet<float>> transformPositionKeys = new Dictionary<string, HashSet<float>>();
			Dictionary<string, HashSet<float>> transformRotationKeys = new Dictionary<string, HashSet<float>>();
			Dictionary<string, HashSet<float>> transformScaleKeys = new Dictionary<string, HashSet<float>>();

			foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(c))
			{
				AnimationCurve curve = AnimationUtility.GetEditorCurve(c, binding);
				if (curve == null) continue;

				bool isTransform = binding.type == typeof(Transform);

				if (!isTransform)
				{
					string key = $"{binding.path}|{binding.propertyName}";

					if (!otherCurveKeys.TryGetValue(key, out HashSet<float> times))
					{
						times = new HashSet<float>();
						otherCurveKeys.Add(key, times);
					}

					foreach (Keyframe keyframe in curve.keys)
					{
						times.Add(keyframe.time);
					}

					continue;
				}

				Dictionary<string, HashSet<float>> target;

				if (binding.propertyName.StartsWith("m_LocalPosition."))
				{
					target = transformPositionKeys;
				}
				else if (binding.propertyName.StartsWith("m_LocalRotation."))
				{
					target = transformRotationKeys;
				}
				else if (binding.propertyName.StartsWith("m_LocalScale."))
				{
					target = transformScaleKeys;
				}
				else
				{
					// Position / Rotation / Scale以外のTransformカーブ
					string key = $"{binding.path}|{binding.propertyName}";

					if (!otherCurveKeys.TryGetValue(key, out HashSet<float> times))
					{
						times = new HashSet<float>();
						otherCurveKeys.Add(key, times);
					}

					foreach (Keyframe keyframe in curve.keys)
					{
						times.Add(keyframe.time);
					}

					continue;
				}

				// TransformのPosition / Rotation / ScaleはX,Y,Z等をまとめて時刻単位でカウント
				string transformPath = binding.path;

				if (!target.TryGetValue(transformPath, out HashSet<float> transformTimes))
				{
					transformTimes = new HashSet<float>();
					target.Add(transformPath, transformTimes);
				}

				foreach (Keyframe keyframe in curve.keys)
				{
					transformTimes.Add(keyframe.time);
				}
			}

			// Transform以外
			foreach (KeyValuePair<string, HashSet<float>> pair in otherCurveKeys)
			{
				total += pair.Value.Count;
			}

			// Position / Rotation / Scale
			foreach (KeyValuePair<string, HashSet<float>> pair in transformPositionKeys)
			{
				total += pair.Value.Count;
			}

			foreach (KeyValuePair<string, HashSet<float>> pair in transformRotationKeys)
			{
				total += pair.Value.Count;
			}

			foreach (KeyValuePair<string, HashSet<float>> pair in transformScaleKeys)
			{
				total += pair.Value.Count;
			}

			// オブジェクト参照カーブ
			foreach (EditorCurveBinding binding in AnimationUtility.GetObjectReferenceCurveBindings(c))
			{
				ObjectReferenceKeyframe[] keys = AnimationUtility.GetObjectReferenceCurve(c, binding);

				if (keys != null) total += keys.Length;
			}

			return total;
		}

		//Timeline
		static int CountAllKeys(TimelineAsset timeline)
		{
			int total = 0;

			foreach (TrackAsset track in timeline.GetOutputTracks())
			{
				total += CountTimelineKeys(track);
			}

			return total;
		}

		static int CountTimelineKeys(TrackAsset track)
		{
			int total = 0;

			foreach (TrackAsset child in track.GetChildTracks())
			{
				total += CountTimelineKeys(child);
			}

			if (track is AnimationTrack)
			{
				AnimationClip infiniteClip = (track as AnimationTrack).infiniteClip;
				if (infiniteClip != null)
				{
					total += CountAllKeys(infiniteClip);
				}
			}
			return total;
		}

		string PickLine(int total)
		{
			if (total < T1) return line_lt100;
			if (total < T2) return line_lt300;
			if (total < T3) return line_lt1000;
			if (total < T4) return line_lt3000;
			if (total < T5) return line_lt10000;
			if (total < T6) return line_lt30000;
			if (total < T7) return line_lt100000;
			return line_ge100000;
		}
	}
}
