using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using Urho.Gui;
using MHUrho.Helpers;

namespace MHUrho.UserInterface
{
    public class SelectionBar : UIElementWrapper {

		public IReadOnlyList<UIElement> Children => buttonHolder.Children;

		protected override UIElement ElementWithChildren => buttonHolder;

		readonly Window wholeWindow;
		readonly UIElement buttonHolder;
		readonly Button leftButton;
		readonly Button rightButton;
		readonly Window centralWindow;

		public SelectionBar(UIElement gameUI)
		{
			wholeWindow = (Window)gameUI.GetChild("SelectionBar");
			wholeWindow.HoverBegin += OnHoverBegin;
			wholeWindow.HoverEnd += OnHoverEnd;

			leftButton = (Button)wholeWindow.GetChild("LeftButton", true);
			leftButton.Pressed += ScrollButtonPressed;
			leftButton.HoverBegin += OnHoverBegin;
			leftButton.HoverEnd += OnHoverEnd;
			rightButton = (Button)wholeWindow.GetChild("RightButton", true);
			rightButton.Pressed += ScrollButtonPressed;
			rightButton.HoverBegin += OnHoverBegin;
			rightButton.HoverEnd += OnHoverEnd;



			buttonHolder = wholeWindow.GetChild("ButtonHolder", true);
			buttonHolder.HoverBegin += OnHoverBegin;
			buttonHolder.HoverEnd += OnHoverEnd;
			centralWindow = (Window)buttonHolder.Parent;
			centralWindow.HoverBegin += OnHoverBegin;
			centralWindow.HoverEnd += OnHoverEnd;
		}

		public void ChangeWidth(int newWidth)
		{
			wholeWindow.MaxWidth = newWidth;
			wholeWindow.MinWidth = newWidth;
			wholeWindow.Width = newWidth;

			int centralWindowWidth = wholeWindow.Width - leftButton.Width - rightButton.Width;

			centralWindow.MaxWidth = centralWindowWidth;
			centralWindow.MinWidth = centralWindowWidth;
			centralWindow.Width = centralWindowWidth;
		}

		void ScrollButtonPressed(PressedEventArgs e)
		{
			if (buttonHolder.Size.X < centralWindow.Size.X) {
				buttonHolder.Position = new IntVector2(0, buttonHolder.Position.Y);
				return;
			}

			int newPos = buttonHolder.Position.X;
			switch (e.Element.Name) {
				case "RightButton":
					newPos -= 100;

					if (newPos + buttonHolder.Size.X < centralWindow.Size.X) {
						newPos = buttonHolder.Parent.Size.X - buttonHolder.Size.X;
					}
					break;
				case "LeftButton":
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

		public override void Dispose()
		{
			wholeWindow.HoverBegin -= OnHoverBegin;
			wholeWindow.HoverEnd -= OnHoverEnd;

			leftButton.Pressed -= ScrollButtonPressed;
			leftButton.HoverBegin -= OnHoverBegin;
			leftButton.HoverEnd -= OnHoverEnd;

			rightButton.Pressed -= ScrollButtonPressed;
			rightButton.HoverBegin -= OnHoverBegin;
			rightButton.HoverEnd -= OnHoverEnd;

			buttonHolder.HoverBegin -= OnHoverBegin;
			buttonHolder.HoverEnd -= OnHoverEnd;

			centralWindow.HoverBegin -= OnHoverBegin;
			centralWindow.HoverEnd -= OnHoverEnd;

			wholeWindow.Dispose();
			buttonHolder.Dispose();
			leftButton.Dispose();
			rightButton.Dispose();
			centralWindow.Dispose();
		}

	}
}
