using System;
using System.Collections.Generic;
using System.Text;
using Urho.Gui;

namespace MHUrho.UserInterface
{
    class ExpansionWindow : IDisposable
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

		public CheckBox CreateCheckBox(string name = "")
		{
			return window.CreateCheckBox(name);
		}

		public CheckBox GetChild(string name)
		{
			return (CheckBox) window.GetChild(name);
		}

		public void RemoveCheckBox(CheckBox element)
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

		public void Dispose()
		{
			window.HoverBegin -= OnHoverBegin;
			window.HoverEnd -= OnHoverEnd;
			window?.Dispose();
		}
	}
}
