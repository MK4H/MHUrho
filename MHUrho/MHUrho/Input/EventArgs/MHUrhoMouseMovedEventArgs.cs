using System;
using System.Collections.Generic;
using System.Text;
using Urho;

namespace MHUrho.Input
{
	public struct MHUrhoMouseMovedEventArgs {
		public int X => CursorPosition.X;
		public int Y => CursorPosition.Y;

		public IntVector2 CursorPosition { get; private set; }

		public int DX => CursorDelta.X;

		public int DY => CursorDelta.Y;

		public IntVector2 CursorDelta { get; private set; }

		public MouseButton Buttons { get; private set; }

		public int Qualifiers { get; private set; }



		public MHUrhoMouseMovedEventArgs(IntVector2 cursorPosition, IntVector2 cursorDelta, MouseButton buttons, int qualifiers)
		{
			this.CursorPosition = cursorPosition;
			this.CursorDelta = cursorDelta;
			this.Buttons = buttons;
			this.Qualifiers = qualifiers;
		}

		public MHUrhoMouseMovedEventArgs(Urho.MouseMovedEventArgs e)
			: this(new IntVector2(e.X, e.Y), new IntVector2(e.DX, e.DY), (MouseButton)e.Buttons, e.Qualifiers)
		{

		}

		public MHUrhoMouseMovedEventArgs(int x, int y, int dx, int dy, MouseButton buttons, int qualifiers)
			: this(new IntVector2(x, y), new IntVector2(dx, dy), buttons, qualifiers)
		{

		}

		public MHUrhoMouseMovedEventArgs(IntVector2 cursorPosition, int dx, int dy, MouseButton buttons, int qualifiers)
			: this(cursorPosition, new IntVector2(dx, dy), buttons, qualifiers)
		{

		}

		public MHUrhoMouseMovedEventArgs(int x, int y, IntVector2 cursorDelta, MouseButton buttons, int qualifiers)
			: this(new IntVector2(x, y), cursorDelta, buttons, qualifiers)
		{

		}

	}
}
