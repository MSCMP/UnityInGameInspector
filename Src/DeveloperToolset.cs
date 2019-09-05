using System;
using UnityEngine;

namespace UnityInGameInspector {
	/// <summary>
	/// The main class that you should integrate in your project.
	/// </summary>
	public class InspectorController {
		Inspector inspector = null;

		delegate void LogDelegate(string message);
		LogDelegate logDelegate;

		InspectorController(LogDelegate logDelegate) {
			this.logDelegate = logDelegate;

			inspector = new Inspector(this);
		}



		public void Show(bool show) {
			inspector.Show(show);
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
