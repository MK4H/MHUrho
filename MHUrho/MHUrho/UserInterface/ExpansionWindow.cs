using System;
using System.Collections.Generic;
using System.Text;
using Urho.Gui;

namespace MHUrho.UserInterface
{

    class ExpansionWindow : IDisposable
    {
		public bool Visible => window.Visible;

		public event Action<HoverBeginEventArgs> HoverBegin;
		public event Action<HoverEndEventArgs> HoverEnd;

		public event Action<ExpansionWindow> VisibilityChanged;

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
			var newCheckBox = window.CreateCheckBox(name);
			newCheckBox.Visible = false;
			newCheckBox.HoverBegin += OnHoverBegin;
			newCheckBox.HoverEnd += OnHoverEnd;
			return newCheckBox;
		}

		public CheckBox GetChild(string name)
		{
			return (CheckBox) window.GetChild(name);
		}

		public void RemoveCheckBox(CheckBox element)
		{
			element.HoverBegin -= OnHoverBegin;
			element.HoverEnd -= OnHoverEnd;
			window.RemoveChild(element);
		}

		public void Show()
		{
			window.Visible = true;
			VisibilityChanged?.Invoke(this);
		}

		public void Hide()
		{
			window.Visible = false;
			VisibilityChanged?.Invoke(this);
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
