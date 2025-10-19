using UnityEditor;
using UnityEngine;

namespace PaLASOLU
{
	public class BPMtoSecondAndFrameCalculator : EditorWindow
	{
		double bpm;
		double spb;
		double fpb;

		double oldbpm;
		double oldspb;
		double oldfpb;

		[MenuItem("Tools/PaLASOLU/Extensions/BPM to Second & Frame Calculator", priority = 301)]
		static void Init()
		{
			var window = GetWindow<BPMtoSecondAndFrameCalculator>("BPM to Second & Frame Calculator");
			window.minSize = new Vector2(400, 100);
		}

		private void OnGUI()
		{
			const float labelWidth = 80f;
			const float columnWidth = 80f;

			using (new EditorGUILayout.VerticalScope())
			{
				// ヘッダー行
				using (new EditorGUILayout.HorizontalScope())
				{
					bpm = EditorGUILayout.DoubleField(bpm, GUILayout.Width(columnWidth), GUILayout.Height(EditorGUIUtility.singleLineHeight));
					EditorGUILayout.SelectableLabel("1/1", GUILayout.Width(columnWidth), GUILayout.Height(EditorGUIUtility.singleLineHeight));
					EditorGUILayout.SelectableLabel("1/4", GUILayout.Width(columnWidth), GUILayout.Height(EditorGUIUtility.singleLineHeight));
					EditorGUILayout.SelectableLabel("1/8", GUILayout.Width(columnWidth), GUILayout.Height(EditorGUIUtility.singleLineHeight));
					EditorGUILayout.SelectableLabel("1/16", GUILayout.Width(columnWidth), GUILayout.Height(EditorGUIUtility.singleLineHeight));
				}

				// 秒表示行
				using (new EditorGUILayout.HorizontalScope())
				{
					EditorGUILayout.SelectableLabel("Second", GUILayout.Width(labelWidth), GUILayout.Height(EditorGUIUtility.singleLineHeight));
					EditorGUILayout.SelectableLabel((spb * 4).ToString("F4"), EditorStyles.textField, GUILayout.Width(columnWidth), GUILayout.Height(EditorGUIUtility.singleLineHeight));
					EditorGUILayout.SelectableLabel((spb).ToString("F4"), EditorStyles.textField, GUILayout.Width(columnWidth), GUILayout.Height(EditorGUIUtility.singleLineHeight));
					EditorGUILayout.SelectableLabel((spb / 2).ToString("F4"), EditorStyles.textField, GUILayout.Width(columnWidth), GUILayout.Height(EditorGUIUtility.singleLineHeight));
					EditorGUILayout.SelectableLabel((spb / 4).ToString("F4"), EditorStyles.textField, GUILayout.Width(columnWidth), GUILayout.Height(EditorGUIUtility.singleLineHeight));
				}

				// 1/秒表示行
				using (new EditorGUILayout.HorizontalScope())
				{
					EditorGUILayout.SelectableLabel("1/Second", GUILayout.Width(labelWidth), GUILayout.Height(EditorGUIUtility.singleLineHeight));
					EditorGUILayout.SelectableLabel((1 / (spb * 4)).ToString("F4"), EditorStyles.textField, GUILayout.Width(columnWidth), GUILayout.Height(EditorGUIUtility.singleLineHeight));
					EditorGUILayout.SelectableLabel((1 / (spb)).ToString("F4"), EditorStyles.textField, GUILayout.Width(columnWidth), GUILayout.Height(EditorGUIUtility.singleLineHeight));
					EditorGUILayout.SelectableLabel((1 / (spb / 2)).ToString("F4"), EditorStyles.textField, GUILayout.Width(columnWidth), GUILayout.Height(EditorGUIUtility.singleLineHeight));
					EditorGUILayout.SelectableLabel((1 / (spb / 4)).ToString("F4"), EditorStyles.textField, GUILayout.Width(columnWidth), GUILayout.Height(EditorGUIUtility.singleLineHeight));
				}

				// フレーム表示行
				using (new EditorGUILayout.HorizontalScope())
				{
					EditorGUILayout.SelectableLabel("Frame", GUILayout.Width(labelWidth), GUILayout.Height(EditorGUIUtility.singleLineHeight));
					EditorGUILayout.SelectableLabel((fpb * 4).ToString("F2"), EditorStyles.textField, GUILayout.Width(columnWidth), GUILayout.Height(EditorGUIUtility.singleLineHeight));
					EditorGUILayout.SelectableLabel((fpb).ToString("F2"), EditorStyles.textField, GUILayout.Width(columnWidth), GUILayout.Height(EditorGUIUtility.singleLineHeight));
					EditorGUILayout.SelectableLabel((fpb / 2).ToString("F2"), EditorStyles.textField, GUILayout.Width(columnWidth), GUILayout.Height(EditorGUIUtility.singleLineHeight));
					EditorGUILayout.SelectableLabel((fpb / 4).ToString("F2"), EditorStyles.textField, GUILayout.Width(columnWidth), GUILayout.Height(EditorGUIUtility.singleLineHeight));
				}
			}

			// 値の更新
			if (GUI.changed)
			{
				if (bpm != oldbpm)
				{
					spb = 60.0 / bpm;
					fpb = spb * 60.0;
				}
				else if (spb != oldspb)
				{
					bpm = 60.0 / spb;
					fpb = spb * 60.0;
				}
				else if (fpb != oldfpb)
				{
					spb = fpb / 60.0;
					bpm = 60.0 / spb;
				}

				oldbpm = bpm;
				oldspb = spb;
				oldfpb = fpb;

				Repaint();
			}
		}
	}
}
