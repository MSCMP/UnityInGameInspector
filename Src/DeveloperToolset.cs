using System;
using UnityEngine;

namespace UnityInGameInspector {
	/// <summary>
	/// The main class that you should integrate in your project.
	/// </summary>
	public class InspectorController {
		Inspector inspector = null;

		public delegate void LogDelegate(string message);
		LogDelegate logDelegate;

		public bool Visible {
			get {
				return inspector.Visible;
			}
			set {
				inspector.Visible = value;
			}
		}

		public InspectorController(LogDelegate logDelegate) {
			this.logDelegate = logDelegate;

			inspector = new Inspector(this);
		}


		public void Log(string message) {
			if (logDelegate != null) {
				logDelegate(message);
			}
		}

		public void Inspect(Transform transform) {
			inspector.Inspect(transform);
		}

		public void OnGUI() {
			inspector.OnGUI();
		}
	}
}
