using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Sirenix.OdinInspector;
using Sirenix.OdinInspector.Editor;
using Sirenix.Utilities.Editor;
using UnityEditor;
using UnityEngine;

namespace DevTools.SaveEditor
{
	[System.Serializable]
	public class SaveEditorPanel : IDevToolPanel
	{
		public string Title => "Save Editor";
		public int Order => 30;
		public SdfIconType Icon => SdfIconType.HddFill;

		const float LabelWidth = 120f;
		const float LeftWidth = 540f;
		const float RightWidth = 340f;

		class Entry
		{
			public string Key;
			public string File;
			public object State;
			public PropertyTree Tree;
			public bool Expanded;
		}

		List<Entry> _entries;
		FieldInfo[] _profileFields;

		[OnInspectorGUI]
		void Draw()
		{
			DrawToolbar();

			if (!Application.isPlaying)
			{
				SirenixEditorGUI.InfoMessageBox("Vào Play mode để xem và chỉnh data. Data nằm trong RAM nên chỉ có khi game đang chạy.");
				Dispose();
				_entries = null;
				return;
			}

			if (_entries == null)
			{
				Rebuild();
			}

			GUILayout.BeginHorizontal();

			// ---- LEFT: save data ----
			GUILayout.BeginVertical(GUILayout.Width(LeftWidth));
			DrawSaveables();
			GUILayout.EndVertical();

			GUILayout.Space(10);

			// ---- RIGHT: UseProfile ----
			GUILayout.BeginVertical(GUILayout.Width(RightWidth));
			DrawUseProfile();
			GUILayout.EndVertical();

			GUILayout.EndHorizontal();
		}

		void DrawSaveables()
		{
			if (_entries.Count == 0)
			{
				SirenixEditorGUI.WarningMessageBox("Chưa có saveable nào được đăng ký. Đợi SaveBootstrap init xong rồi bấm Refresh.");
				return;
			}

			foreach (var e in _entries)
			{
				SirenixEditorGUI.BeginBox();

				SirenixEditorGUI.BeginBoxHeader();
				GUILayout.BeginHorizontal();
				e.Expanded = SirenixEditorGUI.Foldout(e.Expanded, $"{e.Key}    ({e.File})");
				GUILayout.FlexibleSpace();
				if (GUILayout.Button("Apply", EditorStyles.miniButton, GUILayout.Width(60)))
				{
					Apply(e);
				}
				GUILayout.EndHorizontal();
				SirenixEditorGUI.EndBoxHeader();

				if (SirenixEditorGUI.BeginFadeGroup(e, e.Expanded))
				{
					if (e.State == null)
					{
						EditorGUILayout.LabelField("(null)");
					}
					else
					{
						float prevLabel = EditorGUIUtility.labelWidth;
						EditorGUIUtility.labelWidth = LabelWidth;
						e.Tree.Draw(false);
						EditorGUIUtility.labelWidth = prevLabel;
					}
				}
				SirenixEditorGUI.EndFadeGroup();

				SirenixEditorGUI.EndBox();
			}
		}

		void DrawUseProfile()
		{
			SirenixEditorGUI.BeginBox();
			SirenixEditorGUI.BeginBoxHeader();
			GUILayout.BeginHorizontal();
			GUILayout.Label("UseProfile  (game_data)", EditorStyles.boldLabel);
			GUILayout.FlexibleSpace();
			if (GUILayout.Button("Apply", EditorStyles.miniButton, GUILayout.Width(60)))
			{
				SaveBootstrap.SaveNow();
				Debug.Log("<color=cyan>[SaveEditor]</color> Applied UseProfile.");
			}
			GUILayout.EndHorizontal();
			SirenixEditorGUI.EndBoxHeader();

			_profileFields ??= typeof(UseProfile)
				.GetFields(BindingFlags.Public | BindingFlags.Static)
				.Where(f => f.FieldType.IsGenericType && f.FieldType.GetGenericTypeDefinition() == typeof(PrefVar<>))
				.ToArray();

			float prev = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = 150;

			foreach (var f in _profileFields)
			{
				var pref = f.GetValue(null);
				var valueProp = f.FieldType.GetProperty("Value");
				var valueType = f.FieldType.GetGenericArguments()[0];
				object cur = valueProp.GetValue(pref);

				EditorGUI.BeginChangeCheck();
				object next = DrawProfileField(f.Name, valueType, cur);
				if (EditorGUI.EndChangeCheck())
				{
					valueProp.SetValue(pref, next);
				}
			}

			EditorGUIUtility.labelWidth = prev;
			SirenixEditorGUI.EndBox();
		}

		object DrawProfileField(string label, Type type, object cur)
		{
			if (type == typeof(int))
			{
				return EditorGUILayout.IntField(label, cur is int i ? i : 0);
			}
			if (type == typeof(bool))
			{
				return EditorGUILayout.Toggle(label, cur is bool b && b);
			}
			if (type == typeof(float))
			{
				return EditorGUILayout.FloatField(label, cur is float fl ? fl : 0f);
			}
			if (type == typeof(string))
			{
				return EditorGUILayout.TextField(label, cur as string ?? "");
			}

			EditorGUILayout.LabelField(label, cur?.ToString() ?? "null");
			return cur;
		}

		void DrawToolbar()
		{
			SirenixEditorGUI.BeginHorizontalToolbar();

			if (SirenixEditorGUI.ToolbarButton("Refresh"))
			{
				Rebuild();
			}

			if (Application.isPlaying && SirenixEditorGUI.ToolbarButton("Apply All"))
			{
				ApplyAll();
			}

			if (SirenixEditorGUI.ToolbarButton("Open Folder"))
			{
				EditorUtility.RevealInFinder(Application.persistentDataPath);
			}

			GUILayout.FlexibleSpace();

			if (SirenixEditorGUI.ToolbarButton("Delete All"))
			{
				DeleteAll();
			}

			SirenixEditorGUI.EndHorizontalToolbar();
		}

		void Rebuild()
		{
			Dispose();
			_entries = new List<Entry>();

			if (!Application.isPlaying)
			{
				return;
			}

			foreach (var s in SaveManager.EditorGetAll())
			{
				// game_prefs đã hiển thị dạng UseProfile ở pane phải nên bỏ khỏi pane trái
				if (s.Key == "game_prefs")
				{
					continue;
				}

				bool hasContent = s.State is not ICollection c || c.Count > 0;

				_entries.Add(new Entry
				{
					Key = s.Key,
					File = s.Filename,
					State = s.State,
					Tree = s.State != null ? PropertyTree.Create(s.State) : null,
					Expanded = hasContent
				});
			}
		}

		void Apply(Entry e)
		{
			SaveManager.EditorApply(e.Key, e.State);
			SaveBootstrap.SaveNow();
			Debug.Log($"<color=cyan>[SaveEditor]</color> Applied '{e.Key}'.");
		}

		void ApplyAll()
		{
			foreach (var e in _entries)
			{
				SaveManager.EditorApply(e.Key, e.State);
			}

			SaveBootstrap.SaveNow();
			Debug.Log($"<color=cyan>[SaveEditor]</color> Applied all ({_entries.Count}).");
		}

		void DeleteAll()
		{
			if (!EditorUtility.DisplayDialog("Xóa toàn bộ save?",
				"Xóa hết file .sav và data trong RAM. Không hoàn tác!", "Xóa", "Hủy"))
			{
				return;
			}

			if (Application.isPlaying)
			{
				SaveBootstrap.DeleteAllData();
			}
			else
			{
				foreach (var f in Directory.GetFiles(Application.persistentDataPath))
				{
					if (f.Contains(".sav"))
					{
						File.Delete(f);
					}
				}
			}

			Rebuild();
			Debug.Log("<color=red>[SaveEditor]</color> Deleted all save data.");
		}

		void Dispose()
		{
			if (_entries == null)
			{
				return;
			}

			foreach (var e in _entries)
			{
				e.Tree?.Dispose();
			}
		}
	}
}
