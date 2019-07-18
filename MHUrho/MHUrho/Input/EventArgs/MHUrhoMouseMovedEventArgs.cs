using System;
using System.Collections.Generic;
using System.Text;
using Urho;

namespace MHUrho.Input
{
	/// <summary>
	/// Custom class containing the data of the mouse moved event.
	/// Mostly to enable mocking of mouse movement.
	/// </summary>
	public struct MHUrhoMouseMovedEventArgs {

		/// <summary>
		/// X coordinate of the cursor after the movement.
		/// </summary>
		public int X => CursorPosition.X;

		/// <summary>
		/// Y coordinate of the cursor after the movement.
		/// </summary>
		public int Y => CursorPosition.Y;

		/// <summary>
		/// The position of the cursor after the movement.
		/// </summary>
		public IntVector2 CursorPosition { get; private set; }

		/// <summary>
		/// Change of the X coordinate.
		/// </summary>
		public int DeltaX => CursorDelta.X;

		/// <summary>
		/// Change of the Y coordinate.
		/// </summary>
		public int DeltaY => CursorDelta.Y;

		/// <summary>
		/// Change of the cursor position.
		/// </summary>
		public IntVector2 CursorDelta { get; private set; }

		/// <summary>
		/// Pressed buttons during the movement.
		/// </summary>
		public MouseButton Buttons { get; private set; }

		/// <summary>
		/// Pressed qualifier keys during the movement.
		/// </summary>
		public int Qualifiers { get; private set; }


		/// <summary>
		/// Creates representation of mouse movement event.
		/// </summary>
		/// <param name="cursorPosition">The position of the cursor after the movement.</param>
		/// <param name="cursorDelta">The change of the position of the cursor this event represents.</param>
		/// <param name="buttons">Pressed mouse buttons during the event.</param>
		/// <param name="qualifiers">Pressed qualifier keys during the event.</param>
		public MHUrhoMouseMovedEventArgs(IntVector2 cursorPosition, IntVector2 cursorDelta, MouseButton buttons, int qualifiers)
		{
			this.CursorPosition = cursorPosition;
			this.CursorDelta = cursorDelta;
			this.Buttons = buttons;
			this.Qualifiers = qualifiers;
		}

		/// <summary>
		/// Creates representation of mouse movement event.
		/// </summary>
		/// <param name="e">Engine representation of the event.</param>
		public MHUrhoMouseMovedEventArgs(Urho.MouseMovedEventArgs e)
			: this(new IntVector2(e.X, e.Y), new IntVector2(e.DX, e.DY), (MouseButton)e.Buttons, e.Qualifiers)
		{

		}

		/// <summary>
		/// Creates representation of mouse movement event.
		/// </summary>
		/// <param name="x">The X coordinate of the cursor after the movement.</param>
		/// <param name="y">The Y coordinate of the cursor after the movement.</param>
		/// <param name="dx">Change in the X coordinate this event represents.</param>
		/// <param name="dy">Change in the Y coordinate this event represents.</param>
		/// <param name="buttons">Pressed mouse buttons during the event.</param>
		/// <param name="qualifiers">Pressed qualifier keys during the event.</param>
		public MHUrhoMouseMovedEventArgs(int x, int y, int dx, int dy, MouseButton buttons, int qualifiers)
			: this(new IntVector2(x, y), new IntVector2(dx, dy), buttons, qualifiers)
		{

		}

		/// <summary>
		/// Creates representation of mouse movement event.
		/// </summary>
		/// <param name="cursorPosition">The position of the cursor after the movement.</param>
		/// <param name="dx">Change in the X coordinate this event represents.</param>
		/// <param name="dy">Change in the Y coordinate this event represents.</param>
		/// <param name="buttons">Pressed mouse buttons during the event.</param>
		/// <param name="qualifiers">Pressed qualifier keys during the event.</param>
		public MHUrhoMouseMovedEventArgs(IntVector2 cursorPosition, int dx, int dy, MouseButton buttons, int qualifiers)
			: this(cursorPosition, new IntVector2(dx, dy), buttons, qualifiers)
		{

		}


		/// <summary>
		/// Creates representation of mouse movement event.
		/// </summary>
		/// <param name="x">The X coordinate of the cursor after the movement.</param>
		/// <param name="y">The Y coordinate of the cursor after the movement.</param>
		/// <param name="cursorDelta">The change of the position of the cursor this event represents.</param>
		/// <param name="buttons">Pressed mouse buttons during the event.</param>
		/// <param name="qualifiers">Pressed qualifier keys during the event.</param>
		public MHUrhoMouseMovedEventArgs(int x, int y, IntVector2 cursorDelta, MouseButton buttons, int qualifiers)
			: this(new IntVector2(x, y), cursorDelta, buttons, qualifiers)
		{

		}

	}
}
