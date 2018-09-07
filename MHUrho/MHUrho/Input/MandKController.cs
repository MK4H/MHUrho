using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Packaging;
using Urho;
using Urho.Gui;
using Urho.Resources;

namespace MHUrho.Input
{
	public abstract class MandKController
	{

		public bool Enabled { get; private set; }

		public float MouseSensitivity { get; set; }

		protected MyGame Game => MyGame.Instance;

		protected Urho.Input Input => Game.Input;
		protected UI UI => Game.UI;

		protected MandKController() {
			this.MouseSensitivity = 0.2f;
			this.Enabled = false;

			var style = PackageManager.Instance.GetXmlFile("UI/DefaultStyle.xml");
			UI.Root.SetDefaultStyle(style);

			var cursor = UI.Root.CreateCursor("UICursor");
			UI.Cursor = cursor;
			UI.Cursor.SetStyleAuto(style);
			UI.Cursor.Position = new IntVector2(UI.Root.Width / 2, UI.Root.Height / 2);

			var cursorImage = PackageManager.Instance.GetImage("Textures/xamarin.png");
			UI.Cursor.DefineShape("MyShape",
								  cursorImage,
								  new IntRect(0, 0, cursorImage.Width - 1, cursorImage.Height - 1),
								  new IntVector2(cursorImage.Width / 2, cursorImage.Height / 2));
			//TODO: Shape keeps reseting to NORMAL, even though i set it here
			UI.Cursor.Shape = "MyShape";
			UI.Cursor.UseSystemShapes = false;
			UI.Cursor.Visible = true;

			Input.SetMouseMode(MouseMode.Absolute);
			Input.SetMouseVisible(false);
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
	}
}
