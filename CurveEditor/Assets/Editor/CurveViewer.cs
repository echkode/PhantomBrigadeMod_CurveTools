using System;
using System.Collections.Generic;
using System.IO;

using UnityEditor;
using UnityEngine;

using YamlDotNet.Serialization;

public class CurveViewer : EditorWindow
{
	private static class Constants
	{
		internal const string InputType = "guidanceInputType";

		internal static class Panel
		{
			internal const string Damage = "damage";
			internal const string Guidance = "guidance";
		}

		internal static class Menu
		{
			internal const string Damage = "damage";
			internal const string Guidance = "guidance";
			internal const string GuidanceInputType = "guidanceInputType";
		}
	}
	private readonly Dictionary<string, string> curveSourceMap = new()
	{
		[Constants.Panel.Damage] = "O:\\damage",
		[Constants.Panel.Guidance] = "O:\\guidance",
	};
	private readonly Dictionary<string, string> curveAltMap = new()
	{
		[Constants.Panel.Damage] = "A:\\damage",
		[Constants.Panel.Guidance] = "A:\\guidance",
	};
	private readonly Dictionary<string, bool> showResult = new()
	{
		[Constants.Panel.Damage] = false,
		[Constants.Panel.Guidance] = false,
		[Constants.InputType] = false,
	};
	private readonly Dictionary<string, AnimationCurve> curveMap = new()
	{
		[Constants.Panel.Damage] = null,
		[Constants.Panel.Guidance] = null,
	};
	private readonly Dictionary<string, string> keyMap = new()
	{
		[Constants.Panel.Damage] = "Curves",
		[Constants.Panel.Guidance] = "Curves",
		[Constants.InputType] = "Input Types",
	};
	private readonly Dictionary<string, bool> curveMenuSource = new()
	{
		[Constants.Panel.Damage] = false,
		[Constants.Panel.Guidance] = false,
	};
	private readonly Dictionary<string, string> saveMap = new()
	{
		[Constants.Panel.Damage] = "",
		[Constants.Panel.Guidance] = "",
	};
	private readonly Dictionary<string, Rect> menuMap = new()
	{
		[Constants.Menu.Damage] = new Rect(),
		[Constants.Menu.Guidance] = new Rect(),
		[Constants.Menu.GuidanceInputType] = new Rect(),
	};

	private bool showSourcePathsPanel = false;
	private bool showSavePathsPanel = false;
	private bool showDamageCurvePanel = true;
	private bool showGuidanceCurvePanel = false;

	private string pathOptimalRangeTable = "O:\\misc\\optimal_range_table.tsv";

	private IDeserializer deserializer;
	private ISerializer serializer;

	[MenuItem("Window/View Curve")]
	public static void ShowWindow()
	{
		var window = GetWindow<CurveViewer>();
		var width = 800f;
		window.position = new Rect(200f, 75f, width, 600f);
		window.Show();
	}

	void OnGUI()
	{
		if (deserializer == null)
		{
			InitializeDeserializer();
			InitializeSerializer();
		}

		SourcePathsPanel();
		SavePathsPanel();
		EditorGUILayout.Space();
		DamageCurvesPanel();
		EditorGUILayout.Space();
		GuidanceCurvesPanel();
	}

	void SourcePathsPanel()
	{
		showSourcePathsPanel = EditorGUILayout.BeginFoldoutHeaderGroup(showSourcePathsPanel, "Original Curve Paths");
		if (showSourcePathsPanel)
		{
			GUILayout.BeginHorizontal();
			curveSourceMap[Constants.Panel.Damage] = EditorGUILayout.DelayedTextField("Damage Curves", curveSourceMap[Constants.Panel.Damage]);
			if (GUILayout.Button("Change", GUILayout.ExpandWidth(false)))
			{
				var pn = EditorUtility.OpenFolderPanel("Select Damage Curves Directory", curveSourceMap[Constants.Panel.Damage], "");
				curveSourceMap[Constants.Panel.Damage] = Path.GetFullPath(pn);
			}
			GUILayout.EndHorizontal();
			GUILayout.BeginHorizontal();
			curveSourceMap[Constants.Panel.Guidance] = EditorGUILayout.DelayedTextField("Guidance Curves", curveSourceMap[Constants.Panel.Guidance]);
			if (GUILayout.Button("Change", GUILayout.ExpandWidth(false)))
			{
				var pn = EditorUtility.OpenFolderPanel("Select Guidance Curves Directory", curveSourceMap[Constants.Panel.Guidance], "");
				curveSourceMap[Constants.Panel.Guidance] = Path.GetFullPath(pn);
			}
			GUILayout.EndHorizontal();
		}
		EditorGUILayout.EndFoldoutHeaderGroup();
	}

	void SavePathsPanel()
	{
		showSavePathsPanel = EditorGUILayout.BeginFoldoutHeaderGroup(showSavePathsPanel, "Save Paths");
		if (showSavePathsPanel)
		{
			GUILayout.BeginHorizontal();
			curveAltMap[Constants.Panel.Damage] = EditorGUILayout.DelayedTextField("Damage Curves", curveAltMap[Constants.Panel.Damage]);
			if (GUILayout.Button("Change", GUILayout.ExpandWidth(false)))
			{
				var pn = EditorUtility.OpenFolderPanel("Select Damage Curves Directory", curveAltMap[Constants.Panel.Damage], "");
				curveAltMap[Constants.Panel.Damage] = Path.GetFullPath(pn);
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			curveAltMap[Constants.Panel.Guidance] = EditorGUILayout.DelayedTextField("Guidance Curves", curveAltMap[Constants.Panel.Guidance]);
			if (GUILayout.Button("Change", GUILayout.ExpandWidth(false)))
			{
				var pn = EditorUtility.OpenFolderPanel("Select Guidance Curves Directory", curveAltMap[Constants.Panel.Guidance], "");
				curveAltMap[Constants.Panel.Guidance] = Path.GetFullPath(pn);
			}
			GUILayout.EndHorizontal();

			GUILayout.BeginHorizontal();
			pathOptimalRangeTable = EditorGUILayout.DelayedTextField("Optimal Range Table", pathOptimalRangeTable);
			if (GUILayout.Button("Change", GUILayout.ExpandWidth(false)))
			{
				var pn = EditorUtility.OpenFolderPanel("Select Destination for Optimal Range Table", pathOptimalRangeTable, "tsv");
				pathOptimalRangeTable = Path.Combine(Path.GetFullPath(pn), Path.GetFileName(pathOptimalRangeTable));
			}
			GUILayout.EndHorizontal();
		}
		EditorGUILayout.EndFoldoutHeaderGroup();
	}

	void DamageCurvesPanel()
	{
		showDamageCurvePanel = EditorGUILayout.BeginFoldoutHeaderGroup(showDamageCurvePanel, "Damage Curves");
		if (showDamageCurvePanel)
		{
			var useAlt = EditorGUILayout.Toggle("Use Alternate Directory", curveMenuSource[Constants.Panel.Damage]);
			if (useAlt != curveMenuSource[Constants.Panel.Damage])
			{
				curveMenuSource[Constants.Panel.Damage] = useAlt;
				keyMap[Constants.Panel.Damage] = "Curves";
				showResult[Constants.Panel.Damage] = false;
			}
			var sourcePath = curveMenuSource[Constants.Panel.Damage]
				? curveAltMap[Constants.Panel.Damage]
				: curveSourceMap[Constants.Panel.Damage];
			EditorGUILayout.LabelField("Curves path", sourcePath);

			GUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel("Choose a curve");
			if (EditorGUILayout.DropdownButton(new GUIContent(keyMap[Constants.Panel.Damage]), FocusType.Keyboard))
			{
				var menu = new GenericMenu();
				foreach (var pn in Directory.EnumerateFiles(sourcePath, "*.yaml"))
				{
					AddCurveKey(Constants.Panel.Damage, menu, pn);
				}
				menu.DropDown(menuMap[Constants.Menu.Damage]);
			}
			if (Event.current.type == EventType.Repaint)
			{
				menuMap[Constants.Menu.Damage] = GUILayoutUtility.GetLastRect();
			}
			GUILayout.EndHorizontal();

			if (showResult[Constants.Panel.Damage])
			{
				var curve = curveMap[Constants.Panel.Damage];
				EditorGUILayout.Space();
				GUILayout.BeginHorizontal();
				foreach (var t in new[] { 0f, 0.25f, 0.5f, 0.75f, 1f })
				{
					GUILayout.Label($"{t}: {curve.Evaluate(t)}");
				}
				GUILayout.EndHorizontal();
				EditorGUILayout.CurveField(curve, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));

				EditorGUILayout.Space();
				EditorGUILayout.LabelField("Curve save directory", curveAltMap[Constants.Panel.Damage]);
				GUILayout.BeginHorizontal();
				saveMap[Constants.Panel.Damage] = EditorGUILayout.DelayedTextField("Save Curve As", saveMap[Constants.Panel.Damage]);
				if (GUILayout.Button("Save", GUILayout.ExpandWidth(false)))
				{
					var pn = Path.Combine(curveAltMap[Constants.Panel.Damage], saveMap[Constants.Panel.Damage] + ".yaml");
					SaveCurve(Constants.Panel.Damage, pn);
				}
				GUILayout.EndHorizontal();

				if (useAlt)
				{
					EditorGUILayout.Space();
					GUILayout.BeginHorizontal();
					EditorGUILayout.PrefixLabel("Optimal Range Table");
					GUILayout.BeginVertical();
					EditorGUILayout.LabelField("Save Directory", Path.GetDirectoryName(pathOptimalRangeTable));
					GUILayout.BeginHorizontal();
					var name = EditorGUILayout.DelayedTextField("Save Range Table As", Path.GetFileNameWithoutExtension(pathOptimalRangeTable));
					pathOptimalRangeTable = Path.Combine(Path.GetDirectoryName(pathOptimalRangeTable), name + ".tsv");
					if (GUILayout.Button("Save", GUILayout.ExpandWidth(false)))
					{
						SaveOptimalTable();
					}
					GUILayout.EndHorizontal();
					GUILayout.EndVertical();
					GUILayout.EndHorizontal();
				}
			}
		}
		EditorGUILayout.EndFoldoutHeaderGroup();
	}

	void GuidanceCurvesPanel()
	{
		showGuidanceCurvePanel = EditorGUILayout.BeginFoldoutHeaderGroup(showGuidanceCurvePanel, "Guidance Curves");
		if (showGuidanceCurvePanel)
		{
			var useAlt = EditorGUILayout.Toggle("Use Alternate Directory", curveMenuSource[Constants.Panel.Guidance]);
			if (useAlt != curveMenuSource[Constants.Panel.Guidance])
			{
				curveMenuSource[Constants.Panel.Guidance] = useAlt;
				keyMap[Constants.InputType] = "Input Types";
				showResult[Constants.InputType] = false;
				keyMap[Constants.Panel.Guidance] = "Curves";
				showResult[Constants.Panel.Guidance] = false;
			}
			var sourcePath = curveMenuSource[Constants.Panel.Guidance]
				? curveAltMap[Constants.Panel.Guidance]
				: curveSourceMap[Constants.Panel.Guidance];
			EditorGUILayout.LabelField("Input types path", sourcePath);

			GUILayout.BeginHorizontal();
			EditorGUILayout.PrefixLabel("Choose an input type");
			if (EditorGUILayout.DropdownButton(new GUIContent(keyMap[Constants.InputType]), FocusType.Keyboard))
			{
				var menu = new GenericMenu();
				foreach (var pn in Directory.EnumerateDirectories(sourcePath, "*", SearchOption.TopDirectoryOnly))
				{
					AddInputTypeKey(menu, pn);
				}
				menu.DropDown(menuMap[Constants.Menu.GuidanceInputType]);
			}
			if (Event.current.type == EventType.Repaint)
			{
				menuMap[Constants.Menu.GuidanceInputType] = GUILayoutUtility.GetLastRect();
			}
			GUILayout.EndHorizontal();

			if (showResult[Constants.InputType])
			{
				var curveSource = Path.Combine(sourcePath, keyMap[Constants.InputType]);
				EditorGUILayout.LabelField("Curves path", curveSource);

				GUILayout.BeginHorizontal();
				EditorGUILayout.PrefixLabel("Choose a curve");
				if (EditorGUILayout.DropdownButton(new GUIContent(keyMap[Constants.Panel.Guidance]), FocusType.Keyboard))
				{
					var menu = new GenericMenu();
					foreach (var pn in Directory.EnumerateFiles(curveSource, "*.yaml"))
					{
						AddCurveKey(Constants.Panel.Guidance, menu, pn);
					}
					menu.DropDown(menuMap[Constants.Menu.Guidance]);
				}
				if (Event.current.type == EventType.Repaint)
				{
					menuMap[Constants.Menu.Guidance] = GUILayoutUtility.GetLastRect();
				}
				GUILayout.EndHorizontal();
				if (showResult[Constants.Panel.Guidance])
				{
					var curve = curveMap[Constants.Panel.Guidance];
					GUILayout.BeginHorizontal();
					foreach (var t in new[] { 0f, 0.25f, 0.5f, 0.75f, 1f })
					{
						GUILayout.Label($"{t}: {curve.Evaluate(t)}");
					}
					GUILayout.EndHorizontal();
					EditorGUILayout.CurveField(curve, GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));

					EditorGUILayout.Space();
					var altPath = Path.Combine(curveAltMap[Constants.Panel.Guidance], keyMap[Constants.InputType]);
					EditorGUILayout.LabelField("Curve save directory", altPath);
					GUILayout.BeginHorizontal();
					saveMap[Constants.Panel.Guidance] = EditorGUILayout.DelayedTextField("Save Curve As", saveMap[Constants.Panel.Guidance]);
					if (GUILayout.Button("Save", GUILayout.ExpandWidth(false)))
					{
						var pn = Path.Combine(altPath, saveMap[Constants.Panel.Guidance] + ".yaml");
						SaveCurve(Constants.Panel.Guidance, pn);
					}
					GUILayout.EndHorizontal();
				}
			}
		}
		EditorGUILayout.EndFoldoutHeaderGroup();
	}

	void InitializeDeserializer()
	{
		var builder = new DeserializerBuilder();
		deserializer = builder.Build();
	}

	void InitializeSerializer()
	{
		var builder = new SerializerBuilder();
		serializer = builder
			.EnsureRoundtrip()
			.Build();
	}

	void AddCurveKey(string panel, GenericMenu menu, string pn)
	{
		var text = Path.GetFileNameWithoutExtension(pn);
		menu.AddItem(new GUIContent(text), text == keyMap[panel], p => OnCurveSelected(panel, p), pn);
	}

	void OnCurveSelected(string panel, object pn)
	{
		try
		{
			keyMap[panel] = Path.GetFileNameWithoutExtension((string)pn);
			using (var r = File.OpenText((string)pn))
			{
				curveMap[panel] = (AnimationCurve)deserializer.Deserialize<AnimationCurveSerialized>(r);
			}
			showResult[panel] = true;
			saveMap[panel] = keyMap[panel];
		}
		catch (Exception ex)
		{
			Debug.LogException(ex);
			showResult[panel] = false;
		}
	}

	void SaveCurve(string panel, string pn)
	{
		Debug.Log($"Saving to {pn}");
		try
		{
			if (File.Exists(pn))
			{
				File.Delete(pn);
			}

			var serialized = (AnimationCurveSerialized)curveMap[panel];
			using (var w = new StreamWriter(File.OpenWrite(pn)))
			{
				serializer.Serialize(w, serialized, typeof(AnimationCurveSerialized));
			}
		}
		catch (Exception ex)
		{
			Debug.LogException(ex);
		}
	}

	void SaveOptimalTable()
	{
		try
		{
			if (File.Exists(pathOptimalRangeTable))
			{
				File.Delete(pathOptimalRangeTable);
			}

			using (var w = new StreamWriter(File.OpenWrite(pathOptimalRangeTable)))
			{
				foreach (var pn in Directory.EnumerateFiles(curveAltMap[Constants.Panel.Damage], "*.yaml"))
				{
					var key = Path.GetFileNameWithoutExtension(pn);
					AnimationCurve animc;
					using (var r = File.OpenText(pn))
					{
						animc = (AnimationCurve)deserializer.Deserialize<AnimationCurveSerialized>(r);
					}

					var low = -1f;
					var high = 1f;
					for (var i = 0; i < 100; i += 1)
					{
						var v = i / 100f;
						var f = animc.Evaluate(v);
						if (f < 0.80f)
						{
							if (high != 1f)
							{
								break;
							}
							continue;
						}

						if (low == -1f)
						{
							low = v;
							continue;
						}

						high = v;
					}

					w.Write(key);
					w.Write("\t");
					w.Write(low);
					w.Write("\t");
					w.WriteLine(high);
				}
			}
		}
		catch (Exception ex)
		{
			Debug.LogException(ex);
		}
	}

	void AddInputTypeKey(GenericMenu menu, string pn)
	{
		var text = Path.GetFileNameWithoutExtension(pn);
		menu.AddItem(new GUIContent(text), text == keyMap[Constants.InputType], OnInputTypeSelected, pn);
	}

	void OnInputTypeSelected(object pn)
	{
		var inputType = Path.GetFileNameWithoutExtension((string)pn);
		if (inputType != keyMap[Constants.InputType])
		{
			keyMap[Constants.InputType] = inputType;
			keyMap[Constants.Panel.Guidance] = "Curves";
			showResult[Constants.Panel.Guidance] = false;
		}
		showResult[Constants.InputType] = true;
	}
}
