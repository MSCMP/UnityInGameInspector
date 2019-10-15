using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HutongGames.PlayMaker;
using UnityEngine;
using Object = UnityEngine.Object;

namespace UnityInGameInspector
{
	/// <summary>
	/// The inspector class.
	/// </summary>
	public class Inspector
	{
		public bool Visible = false;
		List<Transform> rootTransforms = new List<Transform>();
		Dictionary<Transform, bool> hierarchyOpen = new Dictionary<Transform, bool>();
		bool filterItemX;
		bool searchCasesensitive = false;
		Vector2 hierarchyScrollPosition;
		Transform inspect;
		Vector2 inspectScrollPosition;
		string search = "";

		readonly Dictionary<PlayMakerFSM, FsmToggle> fsmToggles = new Dictionary<PlayMakerFSM, FsmToggle>();
		bool bindingFlagPublic;
		bool bindingFlagNonPublic;

		PlayMakerFSMEditor fsmEditor;

		InspectorController controller;

		public Inspector(InspectorController controller) {
			this.controller = controller;

			fsmEditor = new PlayMakerFSMEditor(this);
		}

		public void Log(string message) {
			controller.Log(message);
		}


		class FsmToggle
		{
			public bool showVars;
			public bool showStates;
			public bool showEvents;
			public bool showGlobalTransitions;
		}

		internal void Search(string keyword)
		{
			hierarchyOpen.Clear();
			// get all objs
			var objs = GameObject.FindObjectOfType<GameObject>();
			if (string.IsNullOrEmpty(keyword))
			{
				rootTransforms = Object.FindObjectsOfType<Transform>().Where(x => x.parent == null).ToList();
			}
			else
			{
				var stringComparsion = searchCasesensitive ? StringComparison.CurrentCulture : StringComparison.CurrentCultureIgnoreCase;
				rootTransforms = Object.FindObjectsOfType<Transform>().Where(x => (x.name.IndexOf(keyword, stringComparsion) >= 0)).ToList();
			}
			rootTransforms.Sort(TransformNameAscendingSort);
		}

		int TransformNameAscendingSort(Transform x, Transform y)
		{
			return string.Compare(x.name,y.name);
		}

		internal void OnGUI()
		{
			try
			{
				if (Visible)
				{
					// show hierarchy
					GUILayout.BeginArea(new Rect(0, 0, 600, Screen.height));
					GUILayout.BeginVertical("box");
					hierarchyScrollPosition = GUILayout.BeginScrollView(hierarchyScrollPosition, false, true, GUILayout.Width(600));

					GUILayout.Label("Hierarchy");
					search = GUILayout.TextField(search);
					if (GUILayout.Button("Search"))
						Search(search);

					GUILayout.BeginHorizontal("box");
					filterItemX = GUILayout.Toggle(filterItemX, "Filter out itemx");
					searchCasesensitive = GUILayout.Toggle(searchCasesensitive, "Search case sensitive");
					GUILayout.EndHorizontal();
					foreach (var rootTransform in rootTransforms)
					{
						ShowHierarchy(rootTransform);
					}

					GUILayout.EndScrollView();
					GUILayout.EndVertical();
					GUILayout.EndArea();

					if (inspect != null)
					{
						GUILayout.BeginArea(new Rect(Screen.width - 300, 0, 300, Screen.height));
						inspectScrollPosition = GUILayout.BeginScrollView(inspectScrollPosition, false, true, GUILayout.Width(300));
						GUILayout.Label(inspect.name);
						if (GUILayout.Button("Close"))
							inspect = null;
						ShowInspect(inspect);
						GUILayout.EndScrollView();
						GUILayout.EndArea();
					}
				}

				if ((fsmEditor != null) && (fsmEditor.IsPinned || Visible))
				{
					fsmEditor.OnGUI();
				}
			}
			catch (Exception e)
			{
				Log(e.ToString());
			}
		}

		void ShowInspect(Transform trans)
		{
			if (trans != null)
			{
				if (trans.parent != null && GUILayout.Button("Parent"))
				{
					inspect = trans.parent;
					return;
				}
				trans.gameObject.SetActive(GUILayout.Toggle(trans.gameObject.activeSelf, "Is active"));
				GUILayout.Label("Layer:" + LayerMask.LayerToName(trans.gameObject.layer));
				GUILayout.BeginVertical("box");

				bindingFlagPublic = GUILayout.Toggle(bindingFlagPublic, "Show public");
				bindingFlagNonPublic = GUILayout.Toggle(bindingFlagNonPublic, "Show non-public");

				BindingFlags flags = bindingFlagPublic ? BindingFlags.Public : BindingFlags.Default;
				flags |= bindingFlagNonPublic ? BindingFlags.NonPublic : BindingFlags.Default;

				foreach (var comp in trans.GetComponents<Component>())
				{
					var type = comp.GetType();
					GUILayout.Label(type.ToString());

					if (comp is Transform)
					{
						TransformGUI(comp);
					}
					else if (comp is PlayMakerFSM)
					{
						FSMGUI(comp);
					}
					else if (comp is Light)
					{
						LightGUI(comp as Light);
					}
					else
					{
						GenericsGUI(comp,flags);
					}
				}
				GUILayout.EndVertical();
			}
		}

		void LightGUI(Light light)
		{
			GUILayout.BeginHorizontal();
			GUILayout.Label("Shadow bias:");
			light.shadowBias = (float) Convert.ToDouble(GUILayout.TextField(light.shadowBias.ToString()));
			GUILayout.EndHorizontal();
		}

		void GenericsGUI(Component comp, BindingFlags flags)
		{
			var fields = comp.GetType().GetFields(flags | BindingFlags.Instance);
			GUILayout.BeginHorizontal();
			GUILayout.Space(20);
			GUILayout.BeginVertical("box");
			foreach (var fieldInfo in fields)
			{
				GUILayout.BeginHorizontal();
				try
				{
					var fieldValue = fieldInfo.GetValue(comp);
					var fieldValueStr = fieldValue.ToString();
					if (fieldValue is bool)
					{
						GUILayout.Label(fieldInfo.Name);
						var val = GUILayout.Toggle((bool) fieldValue, fieldInfo.Name);
						fieldInfo.SetValue(comp, val);
					}
					else if (fieldValue is string)
					{
						GUILayout.Label(fieldInfo.Name);
						var val = GUILayout.TextField((string) fieldValue);
						fieldInfo.SetValue(comp, val);
					}
					else if (fieldValue is int)
					{
						GUILayout.Label(fieldInfo.Name);
						var val = Convert.ToInt32(GUILayout.TextField(fieldValue.ToString()));
						fieldInfo.SetValue(comp, val);
					}
					else if (fieldValue is float)
					{
						GUILayout.Label(fieldInfo.Name);
						var val = (float) Convert.ToDouble(GUILayout.TextField(fieldValue.ToString()));
						fieldInfo.SetValue(comp, val);
					}
					else
					{
						GUILayout.Label(fieldInfo.Name + ": " + fieldValueStr);
					}
				}
				catch (Exception)
				{
					GUILayout.Label(fieldInfo.Name);
				}
				//fieldInfo.SetValue(fieldInfo.Name, GUILayout.TextField(fieldInfo.GetValue(fieldInfo.Name).ToString()));
				GUILayout.EndHorizontal();
			}
			GUILayout.EndVertical();
			GUILayout.EndHorizontal();
		}

		void FSMGUI(Component comp)
		{
			var fsm = comp as PlayMakerFSM;

			GUILayout.BeginHorizontal();
			GUILayout.Space(20);
			GUILayout.BeginVertical("box");

			GUILayout.Label("Name: " + fsm.Fsm.Name);

			if (GUILayout.Button("Edit FSM")) {
				fsmEditor.StartEdit(fsm);
			}

			GUILayout.EndVertical();
			GUILayout.EndHorizontal();
		}

		void SetFsmGlobalTransitionFor(PlayMakerFSM fsm, bool p)
		{
			if (!fsmToggles.ContainsKey(fsm))
				fsmToggles.Add(fsm, new FsmToggle());
			fsmToggles[fsm].showGlobalTransitions = p;
		}

		bool ShowFsmGlobalTransitionFor(PlayMakerFSM fsm)
		{
			if (!fsmToggles.ContainsKey(fsm))
				fsmToggles.Add(fsm, new FsmToggle());
			return fsmToggles[fsm].showGlobalTransitions;
		}


		void SetFsmStatesFor(PlayMakerFSM fsm, bool p)
		{
			if (!fsmToggles.ContainsKey(fsm))
				fsmToggles.Add(fsm, new FsmToggle());
			fsmToggles[fsm].showStates = p;
		}

		bool ShowFsmStatesFor(PlayMakerFSM fsm)
		{
			if (!fsmToggles.ContainsKey(fsm))
				fsmToggles.Add(fsm, new FsmToggle());
			return fsmToggles[fsm].showStates;
		}

		void SetFsmEventsFor(PlayMakerFSM fsm, bool p)
		{
			if (!fsmToggles.ContainsKey(fsm))
				fsmToggles.Add(fsm, new FsmToggle());
			fsmToggles[fsm].showEvents = p;
		}

		bool ShowFsmEventsFor(PlayMakerFSM fsm)
		{
			if (!fsmToggles.ContainsKey(fsm))
				fsmToggles.Add(fsm, new FsmToggle());
			return fsmToggles[fsm].showEvents;
		}

		void SetFsmVarsFor(PlayMakerFSM fsm, bool p)
		{
			if (!fsmToggles.ContainsKey(fsm))
				fsmToggles.Add(fsm, new FsmToggle());
			fsmToggles[fsm].showVars = p;
		}

		bool ShowFsmVarsFor(PlayMakerFSM fsm)
		{
			if (!fsmToggles.ContainsKey(fsm))
				fsmToggles.Add(fsm, new FsmToggle());
			return fsmToggles[fsm].showVars;
		}

		void TransformGUI(Component comp)
		{
			GUILayout.Label("Tag:" + comp.gameObject.tag);

			var t = (Transform)comp;
			GUILayout.Label("localPosition:");
			GUILayout.BeginHorizontal();
			var pos = t.localPosition;
			pos.x = (float)Convert.ToDouble(GUILayout.TextField(pos.x.ToString()));
			pos.y = (float)Convert.ToDouble(GUILayout.TextField(pos.y.ToString()));
			pos.z = (float)Convert.ToDouble(GUILayout.TextField(pos.z.ToString()));
			t.localPosition = pos;
			GUILayout.EndHorizontal();

			GUILayout.Label("localRotation:");
			GUILayout.BeginHorizontal();
			pos = t.localRotation.eulerAngles;
			pos.x = (float)Convert.ToDouble(GUILayout.TextField(pos.x.ToString()));
			pos.y = (float)Convert.ToDouble(GUILayout.TextField(pos.y.ToString()));
			pos.z = (float)Convert.ToDouble(GUILayout.TextField(pos.z.ToString()));
			t.localRotation = Quaternion.Euler(pos);
			GUILayout.EndHorizontal();

			GUILayout.Label("localScale:");
			GUILayout.BeginHorizontal();
			pos = t.localScale;
			pos.x = (float)Convert.ToDouble(GUILayout.TextField(pos.x.ToString()));
			pos.y = (float)Convert.ToDouble(GUILayout.TextField(pos.y.ToString()));
			pos.z = (float)Convert.ToDouble(GUILayout.TextField(pos.z.ToString()));
			t.localScale = pos;

			t.gameObject.isStatic = false;
			GUILayout.EndHorizontal();
		}

		void ListFsmVariables(IEnumerable<FsmFloat> variables)
		{
			foreach (var fsmFloat in variables)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label(fsmFloat.Name);
				fsmFloat.Value = (float) Convert.ToDouble(GUILayout.TextField(fsmFloat.Value.ToString()));
				GUILayout.EndHorizontal();
			}
		}

		void ListFsmVariables(IEnumerable<FsmBool> variables)
		{
			foreach (var fsmBool in variables)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label(fsmBool.Name + ": " + fsmBool.Value);
				fsmBool.Value = GUILayout.Toggle(fsmBool.Value, "");
				GUILayout.EndHorizontal();
			}
		}

		void ListFsmVariables(IEnumerable<FsmString> variables)
		{
			foreach (var fsmFloat in variables)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label(fsmFloat.Name);
				fsmFloat.Value = GUILayout.TextField(fsmFloat.Value);
				GUILayout.EndHorizontal();
			}
		}

		void ListFsmVariables(IEnumerable<FsmInt> variables)
		{
			foreach (var fsmFloat in variables)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label(fsmFloat.Name);
				fsmFloat.Value = Convert.ToInt32(GUILayout.TextField(fsmFloat.Value.ToString()));
				GUILayout.EndHorizontal();
			}
		}

		void ListFsmVariables(IEnumerable<FsmColor> variables)
		{
			foreach (var fsmFloat in variables)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label(fsmFloat.Name + ": " + fsmFloat.Value);
				GUILayout.EndHorizontal();
			}
		}

		void ListFsmVariables(IEnumerable<FsmGameObject> variables)
		{
			foreach (var fsmFloat in variables)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label(fsmFloat.Name + ": " + fsmFloat.Value);
				GUILayout.EndHorizontal();
			}
		}

		void ListFsmVariables(IEnumerable<FsmVector2> variables)
		{
			foreach (var fsmFloat in variables)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label(fsmFloat.Name + ": " + fsmFloat.Value);
				GUILayout.EndHorizontal();
			}
		}

		void ListFsmVariables(IEnumerable<FsmVector3> variables)
		{
			foreach (var fsmFloat in variables)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label(fsmFloat.Name + ": " + fsmFloat.Value);
				GUILayout.EndHorizontal();
			}
		}

		void ListFsmVariables(IEnumerable<FsmRect> variables)
		{
			foreach (var fsmFloat in variables)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label(fsmFloat.Name + ": " + fsmFloat.Value);
				GUILayout.EndHorizontal();
			}
		}

		void ListFsmVariables(IEnumerable<FsmQuaternion> variables)
		{
			foreach (var fsmFloat in variables)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label(fsmFloat.Name + ": " + fsmFloat.Value);
				GUILayout.EndHorizontal();
			}
		}

		void ListFsmVariables(IEnumerable<FsmObject> variables)
		{
			foreach (var fsmFloat in variables)
			{
				GUILayout.BeginHorizontal();
				GUILayout.Label(fsmFloat.Name + ": " + fsmFloat.Value);
				GUILayout.EndHorizontal();
			}
		}

		void ShowHierarchy(Transform trans)
		{
			if (filterItemX && trans.name.Contains("itemx"))
				return;

			if (!hierarchyOpen.ContainsKey(trans))
				hierarchyOpen.Add(trans,false);

			GUILayout.BeginHorizontal("box");

			GUILayout.Label(trans.name);
			if (GUILayout.Button("i", GUILayout.Width(20)))
			{
				inspect = trans;
			}
			var btn = GUILayout.Button(hierarchyOpen[trans] ? "<" : ">", GUILayout.Width(20));
			if (hierarchyOpen[trans] && btn)
				hierarchyOpen[trans] = false;
			else if (!hierarchyOpen[trans] && btn)
				hierarchyOpen[trans] = true;

			GUILayout.EndHorizontal();

			if (hierarchyOpen[trans])
			{
				GUILayout.BeginHorizontal("box");
				GUILayout.Space(20);
				GUILayout.BeginVertical();

				foreach (Transform t in trans)
				{
					ShowHierarchy(t);
				}

				GUILayout.EndVertical();
				GUILayout.EndHorizontal();
			}
		}

		internal void Inspect(Transform transform)
		{
			inspect = transform;
			fsmToggles.Clear();
		}
	}
}
