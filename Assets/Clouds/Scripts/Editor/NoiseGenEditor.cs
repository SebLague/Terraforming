using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(NoiseGenerator))]
public class NoiseGenEditor : Editor
{

	NoiseGenerator noiseGenerator;
	Editor noiseSettingsEditor;
	Material previewMat;

	public override void OnInspectorGUI()
	{
		DrawDefaultInspector();

		if (GUILayout.Button("Refresh"))
		{
			noiseGenerator.ManualUpdate();
			EditorApplication.QueuePlayerLoopUpdate();
		}

		if (noiseGenerator.ActiveSettings != null)
		{
			DrawSettingsEditor(noiseGenerator.ActiveSettings, ref noiseGenerator.showSettingsEditor, ref noiseSettingsEditor);
		}

		if (noiseGenerator.viewerEnabled)
		{
			if (previewMat == null)
			{
				previewMat = new Material(Shader.Find("Unlit/Preview"));
			}
			noiseGenerator.DebugView(previewMat);

			var e = Editor.CreateEditor(previewMat);
			e.OnPreviewGUI(GUILayoutUtility.GetRect(500, 500), EditorStyles.whiteLabel);
		}
	}


	void DrawSettingsEditor(Object settings, ref bool foldout, ref Editor editor)
	{
		if (settings != null)
		{
			foldout = EditorGUILayout.InspectorTitlebar(foldout, settings);
			using (var check = new EditorGUI.ChangeCheckScope())
			{
				if (foldout)
				{
					CreateCachedEditor(settings, null, ref editor);
					editor.OnInspectorGUI();
				}
				if (check.changed)
				{
					noiseGenerator.ActiveNoiseSettingsChanged();
				}
			}
		}
	}

	void OnEnable()
	{
		noiseGenerator = (NoiseGenerator)target;
	}

}