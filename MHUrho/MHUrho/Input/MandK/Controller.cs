using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Packaging;
using Urho;
using Urho.Gui;
using Urho.Resources;

namespace MHUrho.Input.MandK
{
	public abstract class Controller
	{

		public bool Enabled { get; private set; }

		public float MouseSensitivity { get; set; }

		protected MHUrhoApp Game => MHUrhoApp.Instance;

		protected Urho.Input Input => Game.Input;
		protected UI UI => Game.UI;

		protected Controller() {
			this.MouseSensitivity = 0.2f;
			this.Enabled = false;

			if (UI.Cursor == null) {
				CreateCursor();
			}
		}

		public void Enable() {
			if (Enabled) {
				return;
			}
			Enabled = true;

			Input.KeyUp += KeyUp;
			Input.KeyDown += KeyDown;
			Input.MouseButtonDown += MouseButtonDown;
			Input.MouseButtonUp += MouseButtonUp;
			Input.MouseMoved += MouseMoved;
			Input.MouseWheel += MouseWheel;
		}

		public void Disable() {
			if (!Enabled) {
				return;
			}
			Enabled = false;

			Input.KeyUp -= KeyUp;
			Input.KeyDown -= KeyDown;
			Input.MouseButtonDown -= MouseButtonDown;
			Input.MouseButtonUp -= MouseButtonUp;
			Input.MouseMoved -= MouseMoved;
			Input.MouseWheel -= MouseWheel;
		}

		protected abstract void KeyUp(KeyUpEventArgs e);

		protected abstract void KeyDown(KeyDownEventArgs e);

		protected abstract void MouseButtonDown(MouseButtonDownEventArgs e);

		protected abstract void MouseButtonUp(MouseButtonUpEventArgs e);

		protected abstract void MouseMoved(MouseMovedEventArgs e);

		protected abstract void MouseWheel(MouseWheelEventArgs e);

		void CreateCursor()
		{
			XmlFile style = PackageManager.Instance.GetXmlFile("UI/DefaultCursorStyle.xml", true);

			Cursor cursor = UI.Root.CreateCursor("UICursor");
			UI.Cursor = cursor;
			UI.Cursor.SetStyle("Cursor", style);
			UI.Cursor.Position = new IntVector2(UI.Root.Width / 2, UI.Root.Height / 2);

			UI.Cursor.UseSystemShapes = false;
			UI.Cursor.Visible = true;

			Input.SetMouseMode(MouseMode.Absolute);
			Input.SetMouseVisible(false);
		}
	}
}
