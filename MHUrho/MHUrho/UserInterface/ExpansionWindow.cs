using System;
using System.Collections.Generic;
using System.Text;
using Urho.Gui;

namespace MHUrho.UserInterface
{
    class ExpansionWindow
    {
		public bool Visible {
			get => window.Visible;
			set => window.Visible = value;
		}

		public event Action<HoverBeginEventArgs> HoverBegin;
		public event Action<HoverEndEventArgs> HoverEnd;

		readonly Window window;

		public ExpansionWindow(Window window)
		{
			this.window = window;

			window.HoverBegin += OnHoverBegin;
			window.HoverEnd += OnHoverEnd;
		}

		public void HideAll()
		{
			foreach (var child in window.Children) {
				child.Visible = false;	
			}
		}

		public void AddChild(UIElement element)
		{
			window.AddChild(element);
		}

		public void RmoveChild(UIElement element)
		{
			window.RemoveChild(element);
		}

		void OnHoverBegin(HoverBeginEventArgs e)
		{
			HoverBegin?.Invoke(e);
		}

		void OnHoverEnd(HoverEndEventArgs e)
		{
			HoverEnd?.Invoke(e);
		}
    }
}
