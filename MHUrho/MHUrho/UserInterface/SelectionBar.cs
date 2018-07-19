using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using Urho.Gui;
using MHUrho.Helpers;

namespace MHUrho.UserInterface
{
    class SelectionBar {
		readonly Window wholeWindow;
		readonly UIElement buttonHolder;
		readonly Button LeftButton;
		readonly Button RightButton;
		readonly Window centralWindow;

		public event Action<HoverBeginEventArgs> HoverBegin;
		public event Action<HoverEndEventArgs> HoverEnd;

		public SelectionBar(UIElement gameUI)
		{
			wholeWindow = (Window)gameUI.GetChild("ToolButtons");
			wholeWindow.HoverBegin += OnHoverBegin;
			wholeWindow.HoverEnd += OnHoverEnd;

			LeftButton = (Button)wholeWindow.GetChild("LeftButton", true);
			LeftButton.Pressed += ScrollButtonPressed;
			RightButton = (Button)wholeWindow.GetChild("RightButton", true);
			RightButton.Pressed += ScrollButtonPressed;

			

			buttonHolder = wholeWindow.GetChild("ButtonHolder", true);
			centralWindow = (Window)buttonHolder.Parent;
		}

		public void ChangeWidth(int newWidth)
		{
			wholeWindow.Width = newWidth;
			centralWindow.Width = wholeWindow.Width - LeftButton.Width - RightButton.Width;
		}


		public void AddElement(UIElement element)
		{
			buttonHolder.AddChild(element);
		}

		public void RemoveElement(UIElement element)
		{
			if (!element.IsChildOf(buttonHolder)) {
				throw new ArgumentException("Button was not in the selectionBar", nameof(element));
			}

			buttonHolder.RemoveChild(element);
		}
		void ScrollButtonPressed(PressedEventArgs e)
		{
			if (buttonHolder.Size.X < centralWindow.Size.X) {
				buttonHolder.Position = new IntVector2(0, buttonHolder.Position.Y);
				return;
			}

			int newPos = buttonHolder.Position.X;
			switch (e.Element.Name) {
				case "LeftButton":
					newPos -= 100;

					if (newPos + buttonHolder.Size.X < centralWindow.Size.X) {
						newPos = buttonHolder.Parent.Size.X - buttonHolder.Size.X;
					}
					break;
				case "RightButton":
					newPos += 100;

					if (newPos > 0) {
						newPos = 0;
					}
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}

			//TODO: disable buttons
			buttonHolder.Position = new IntVector2(newPos, buttonHolder.Position.Y);
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
