using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using Urho;
using Urho.Gui;
using Urho.Resources;

namespace MHUrho.UserInterface
{
    public class CustomElementsWindow : UIElementWrapper {
		public int Width => window.Width;
		public int Height => window.Height;

		public IntVector2 Size => window.Size;

		protected override UIElement ElementWithChildren => window;

		readonly Window window;
		readonly UI ui;
		readonly ResourceCache cache;

		public CustomElementsWindow(Window window, UI ui, ResourceCache cache)
		{
			this.window = window;
			this.ui = ui;
			this.cache = cache;
			window.HoverBegin += OnHoverBegin;
			window.HoverEnd += OnHoverEnd;
		}

		public override void Dispose()
		{
			window.HoverBegin -= OnHoverBegin;
			window.HoverEnd -= OnHoverEnd;

			window.Dispose();
		}

		public void LoadLayout(string path)
		{
			ui.LoadLayoutToElement(window, cache, path);
		}
	}
}
