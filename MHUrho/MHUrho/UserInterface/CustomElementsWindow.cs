using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using Urho.Gui;

namespace MHUrho.UserInterface
{
    public class CustomElementsWindow : UIElementWrapper {
		public int Width => window.Width;
		public int Height => window.Height;

		public IntVector2 Size => window.Size;

		protected override UIElement ElementWithChildren => window;

		readonly Window window;

		public CustomElementsWindow(Window window)
		{
			this.window = window;
			window.HoverBegin += OnHoverBegin;
			window.HoverEnd += OnHoverEnd;
		}

		

		public override void Dispose()
		{
			window.HoverBegin -= OnHoverBegin;
			window.HoverEnd -= OnHoverEnd;

			window.Dispose();
		}
	}
}
