using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Packaging;
using Urho;
using Urho.Gui;
using Urho.Resources;

namespace MHUrho.Input.MouseKeyboard
{
	/// <summary>
	/// Base class for user input providers.
	/// </summary>
	public abstract class Controller
	{
		/// <summary>
		/// If the input is being captured by this class.
		/// </summary>
		public bool Enabled { get; private set; }

		/// <summary>
		/// Scaling of the mouse movement.
		/// </summary>
		public float MouseSensitivity { get; set; }

		/// <summary>
		/// Instance of the current application.
		/// </summary>
		protected MHUrhoApp Game => MHUrhoApp.Instance;

		/// <summary>
		/// Game engine input subsystem.
		/// </summary>
		protected Urho.Input Input => Game.Input;

		/// <summary>
		/// Game engine ui subsystem.
		/// </summary>
		protected UI UI => Game.UI;

		/// <summary>
		/// Creates an instance to capture and provide the user input.
		/// </summary>
		protected Controller() {
			this.MouseSensitivity = 0.2f;
			this.Enabled = false;

			if (UI.Cursor == null) {
				CreateCursor();
			}
		}

		/// <summary>
		/// Captures the user input.
		/// </summary>
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

		/// <summary>
		/// Releases the user input.
		/// </summary>
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

		/// <summary>
		/// Invoked when a key is released.
		/// </summary>
		/// <param name="e">Additional data of the key release event.</param>
		protected abstract void KeyUp(KeyUpEventArgs e);

		/// <summary>
		/// Invoked when a key is pressed down.
		/// </summary>
		/// <param name="e">Additional data of the key press event.</param>
		protected abstract void KeyDown(KeyDownEventArgs e);

		/// <summary>
		/// Invoked when a mouse button is pressed down.
		/// </summary>
		/// <param name="e">Additional data of the button press event.</param>
		protected abstract void MouseButtonDown(MouseButtonDownEventArgs e);

		/// <summary>
		/// Invoked when a mouse button is released.
		/// </summary>
		/// <param name="e">Additional data of the button release event.</param>
		protected abstract void MouseButtonUp(MouseButtonUpEventArgs e);

		/// <summary>
		/// Invoked when mouse is moved.
		/// </summary>
		/// <param name="e">Additional data of the mouse move event.</param>
		protected abstract void MouseMoved(MouseMovedEventArgs e);

		/// <summary>
		/// Invoked when the mouse wheel is moved.
		/// </summary>
		/// <param name="e">Additional data of the mouse wheel move event.</param>
		protected abstract void MouseWheel(MouseWheelEventArgs e);

		/// <summary>
		/// Creates a graphical representation of the cursor.
		/// </summary>
		void CreateCursor()
		{
			XmlFile style = Game.PackageManager.GetXmlFile("UI/DefaultCursorStyle.xml", true);

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
