using nadena.dev.ndmf;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;

namespace PaLASOLU
{
	public partial class LoweffortUploaderCore : Plugin<LoweffortUploaderCore>
	{
		private sealed class CurveAccumulator
		{
			public readonly List<Keyframe> Keys = new List<Keyframe>();
			public readonly HashSet<float> KeyTimes = new HashSet<float>();
			public readonly WrapMode PreWrapMode;
			public readonly WrapMode PostWrapMode;

			public CurveAccumulator(AnimationCurve sourceCurve)
			{
				PreWrapMode = sourceCurve.preWrapMode;
				PostWrapMode = sourceCurve.postWrapMode;
			}

			public void Add(Keyframe key)
			{
				if (KeyTimes.Add(key.time)) Keys.Add(key);
			}
		}

		public static AnimationClip BakeAnimationTrackToMergedClip(TrackAsset track)
		{
			if (track is not AnimationTrack animationTrack)
			{
				LogMessageSimplifier.PaLog(5, "Track is not an AnimationTrack!");
				return null;
			}

			TimelineClip[] timelineClips = animationTrack.GetClips().ToArray();
			if (timelineClips.Length == 0)
			{
				LogMessageSimplifier.PaLog(1, $"{track.name} トラックには、アニメーションデータがありません！");
				return null;
			}

			// Merge Clip
			AnimationClip mergedClip = new AnimationClip
			{
				name = $"{track.name}_Merged",
				legacy = false
			};

			Dictionary<EditorCurveBinding, CurveAccumulator> curveAccumulators = new Dictionary<EditorCurveBinding, CurveAccumulator>();
			Dictionary<EditorCurveBinding, List<ObjectReferenceKeyframe>> objectCurveAccumulators = new Dictionary<EditorCurveBinding, List<ObjectReferenceKeyframe>>();

			foreach (TimelineClip clip in timelineClips)
			{
				if (clip.asset is not AnimationPlayableAsset playableAsset)
				{
					LogMessageSimplifier.PaLog(4, $"TimelineClip {clip.displayName} is not an AnimationPlayableAsset.");
					continue;
				}

				AnimationClip sourceClip = playableAsset.clip;
				if (sourceClip == null)
				{
					LogMessageSimplifier.PaLog(1, $"TimelineClip {clip.displayName} に、 AnimationClip が設定されていません！");
					continue;
				}

				double startTime = clip.start;
				double endTime = clip.end;
				bool isLoop = IsLoopingTimelineClip(clip);
				double clipLength = sourceClip.length;
				double timelineLength = clip.duration;
				int loopCount = IsLoopingTimelineClip(clip) ? Mathf.CeilToInt((float)(timelineLength / clipLength)) : 1;

				EditorCurveBinding[] bindings = AnimationUtility.GetCurveBindings(sourceClip);
				EditorCurveBinding[] objBindings = AnimationUtility.GetObjectReferenceCurveBindings(sourceClip);

				foreach (EditorCurveBinding binding in bindings)
				{
					AnimationCurve curve = AnimationUtility.GetEditorCurve(sourceClip, binding);
					if (curve == null) continue;

					// キーフレームを開始時間分だけオフセットしてコピー
					AnimationCurve newCurve = new AnimationCurve();
					newCurve.preWrapMode = curve.preWrapMode;
					newCurve.postWrapMode = curve.postWrapMode;

					List<Keyframe> allKeys = new List<Keyframe>();

					for (int loop = 0; loop < loopCount; loop++)
					{
						double loopedPart = loop * clipLength;

						foreach (Keyframe key in curve.keys)
						{
							float newTime = key.time + (float)startTime + (float)loopedPart;
							allKeys.Add(new Keyframe(newTime, key.value, key.inTangent, key.outTangent));
						}
					}

					if (timelineLength % clipLength > 0.0001) //ちょうどループしない時
					{
						float evalTime = (float)endTime;
						if (evalTime > sourceClip.length) evalTime = evalTime % sourceClip.length;

						float value = curve.Evaluate(evalTime);
						/*
						 * TODO : Tangent補完
						float tangent = 0;

						if (curve.length >= 2)
						{
							for (int i = 0; i < curve.length - 1; i++)
							{
								if (curve.keys[i].time <= evalTime && evalTime <= curve.keys[i + 1].time)
								{
									var k0 = curve.keys[i];
									var k1 = curve.keys[i + 1];
									tangent = (k1.value - k0.value) / (k1.time - k0.time);
									break;
								}
							}
						}*/

						allKeys.Add(new Keyframe((float)endTime, value));
					}

					List<Keyframe> validKeys = allKeys.Where(k => k.time <= endTime).ToList();
					newCurve.keys = validKeys.ToArray();

					if (!curveAccumulators.TryGetValue(binding, out CurveAccumulator accumulator))
					{
						accumulator = new CurveAccumulator(curve);
						curveAccumulators.Add(binding, accumulator);
					}

					foreach (Keyframe key in newCurve.keys)
					{
						accumulator.Add(key);
					}
				}

				foreach (EditorCurveBinding binding in objBindings)
				{
					ObjectReferenceKeyframe[] curve = AnimationUtility.GetObjectReferenceCurve(sourceClip, binding);
					if (curve == null) continue;

					List<ObjectReferenceKeyframe> newKeys = new List<ObjectReferenceKeyframe>();

					foreach (ObjectReferenceKeyframe originalKey in curve)
					{
						for (int loop = 0; loop < loopCount; loop++)
						{
							float newTime = originalKey.time + (float)startTime + (float)clipLength * loop;
							if (newTime > endTime) continue;

							newKeys.Add(new ObjectReferenceKeyframe
							{
								time = newTime,
								value = originalKey.value
							});
						}
					}

					if (!objectCurveAccumulators.TryGetValue(binding, out List<ObjectReferenceKeyframe> accumulator))
					{
						accumulator = new List<ObjectReferenceKeyframe>();
						objectCurveAccumulators.Add(binding, accumulator);
					}

					accumulator.AddRange(newKeys);
				}

			}

			EditorCurveBinding[] mergedBindings = new EditorCurveBinding[curveAccumulators.Count];
			AnimationCurve[] mergedCurves = new AnimationCurve[curveAccumulators.Count];
			int curveIndex = 0;
			foreach (KeyValuePair<EditorCurveBinding, CurveAccumulator> pair in curveAccumulators)
			{
				CurveAccumulator accumulator = pair.Value;
				AnimationCurve curve = new AnimationCurve
				{
					keys = accumulator.Keys.ToArray(),
					preWrapMode = accumulator.PreWrapMode,
					postWrapMode = accumulator.PostWrapMode
				};

				mergedBindings[curveIndex] = pair.Key;
				mergedCurves[curveIndex] = curve;
				curveIndex++;
			}
			if (mergedBindings.Length > 0)
			{
				AnimationUtility.SetEditorCurves(mergedClip, mergedBindings, mergedCurves);
			}

			EditorCurveBinding[] mergedObjectBindings = new EditorCurveBinding[objectCurveAccumulators.Count];
			ObjectReferenceKeyframe[][] mergedObjectCurves = new ObjectReferenceKeyframe[objectCurveAccumulators.Count][];
			int objectCurveIndex = 0;
			foreach (KeyValuePair<EditorCurveBinding, List<ObjectReferenceKeyframe>> pair in objectCurveAccumulators)
			{
				mergedObjectBindings[objectCurveIndex] = pair.Key;
				mergedObjectCurves[objectCurveIndex] = pair.Value.OrderBy(key => key.time).ToArray();
				objectCurveIndex++;
			}
			if (mergedObjectBindings.Length > 0)
			{
				AnimationUtility.SetObjectReferenceCurves(mergedClip, mergedObjectBindings, mergedObjectCurves);
			}

			LogMessageSimplifier.PaLog(0, $"MergedClip generated: {mergedClip.name}");
			return mergedClip;
		}

		public static AnimatorControllerLayer SetupNewLayerAndState(AnimationClip addClip)
		{
			AnimatorControllerLayer newLayer = new AnimatorControllerLayer();
			newLayer.name = addClip.name;
			newLayer.defaultWeight = 1.0f;
			newLayer.blendingMode = AnimatorLayerBlendingMode.Override;
			newLayer.stateMachine = new AnimatorStateMachine();
			newLayer.stateMachine.name = addClip.name;

			AnimatorState newState = newLayer.stateMachine.AddState(addClip.name);
			newState.motion = addClip;

			return newLayer;
		}

		public void GenerateAndBindActivateCurve(AnimationClip mergedClip, TimelineClip clip, string objectName)
		{
			EditorCurveBinding binding = AnimationEditExtension.CreateIsActiveBinding(objectName);

			AnimationCurve curve = new AnimationCurve();
			curve.AddKeySingleOnOff((float)clip.start, (float)clip.end);

			AnimationUtility.SetEditorCurve(mergedClip, binding, curve);

			return;
		}

		public static bool IsLoopingTimelineClip(TimelineClip clip)
		{
			PlayableAsset playableAsset = clip.asset as PlayableAsset;
			AnimationPlayableAsset animPlayable = playableAsset as AnimationPlayableAsset;
			AudioPlayableAsset audioPlayable = playableAsset as AudioPlayableAsset;
			if ((playableAsset is not AnimationPlayableAsset) && (playableAsset is not AudioPlayableAsset))
			{
				return false;
			}

			bool capsAllowLoop = (clip.clipCaps & ClipCaps.Looping) != 0;
			bool assetAllowLoop = false;

			if (playableAsset is AnimationPlayableAsset)
			{
				bool sourceAssetLoop = animPlayable.clip.isLooping;
				bool assetLoop = (animPlayable.loop == AnimationPlayableAsset.LoopMode.On) || (animPlayable.loop == AnimationPlayableAsset.LoopMode.UseSourceAsset && sourceAssetLoop);

				assetAllowLoop = assetLoop;
			}

			if (playableAsset is AudioPlayableAsset)
			{
				assetAllowLoop = audioPlayable.loop;
			}

			return capsAllowLoop && assetAllowLoop;
		}

	}
}
