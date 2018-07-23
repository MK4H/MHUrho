using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using Urho.Gui;
using MHUrho.Helpers;

namespace MHUrho.UserInterface
{
    public class SelectionBar : IDisposable {

		public IReadOnlyList<UIElement> Children => buttonHolder.Children;

		readonly Window wholeWindow;
		readonly UIElement buttonHolder;
		readonly Button leftButton;
		readonly Button rightButton;
		readonly Window centralWindow;

		public event Action<HoverBeginEventArgs> HoverBegin;
		public event Action<HoverEndEventArgs> HoverEnd;

		public SelectionBar(UIElement gameUI)
		{
			wholeWindow = (Window)gameUI.GetChild("SelectionBar");
			wholeWindow.HoverBegin += OnHoverBegin;
			wholeWindow.HoverEnd += OnHoverEnd;

			leftButton = (Button)wholeWindow.GetChild("LeftButton", true);
			leftButton.Pressed += ScrollButtonPressed;
			rightButton = (Button)wholeWindow.GetChild("RightButton", true);
			rightButton.Pressed += ScrollButtonPressed;

			

			buttonHolder = wholeWindow.GetChild("ButtonHolder", true);
			centralWindow = (Window)buttonHolder.Parent;
		}

		public void ChangeWidth(int newWidth)
		{
			wholeWindow.Width = newWidth;
			centralWindow.Width = wholeWindow.Width - leftButton.Width - rightButton.Width;
		}

		public BorderImage CreateBorderImage(string name = "", uint index = 4294967295)
		{
			return buttonHolder.CreateBorderImage(name, index);
		}

		public Button CreateButton(string name = "", uint index = 4294967295)
		{
			return buttonHolder.CreateButton(name, index);
		}

		public CheckBox CreateCheckBox(string name = "", uint index = 4294967295)
		{
			return buttonHolder.CreateCheckBox(name, index);
		}

		public DropDownList CreateDropDownList(string name = "", uint index = 4294967295)
		{
			return buttonHolder.CreateDropDownList(name, index);
		}

		public LineEdit CreateLineEdit(string name = "", uint index = 4294967295)
		{
			return buttonHolder.CreateLineEdit(name, index);
		}

		public ListView CreateListView(string name = "", uint index = 4294967295)
		{
			return buttonHolder.CreateListView(name, index);
		}

		public Menu CreateMenu(string name = "", uint index = 4294967295)
		{
			return buttonHolder.CreateMenu(name, index);
		}

		public ScrollBar CreateScrollBar(string name = "", uint index = 4294967295)
		{
			return buttonHolder.CreateScrollBar(name, index);
		}

		public ScrollView CreateScrollView(string name = "", uint index = 4294967295)
		{
			return buttonHolder.CreateScrollView(name, index);
		}

		public Slider CreateSlider(string name = "", uint index = 4294967295)
		{
			return buttonHolder.CreateSlider(name, index);
		}

		public Sprite CreateSprite(string name = "", uint index = 4294967295)
		{
			return buttonHolder.CreateSprite(name, index);
		}

		public Text CreateText(string name = "", uint index = 4294967295)
		{
			return buttonHolder.CreateText(name, index);
		}

		public ToolTip CreateToolTip(string name = "", uint index = 4294967295)
		{
			return buttonHolder.CreateToolTip(name, index);
		}

		public Window CreateWindow(string name = "", uint index = 4294967295)
		{
			return buttonHolder.CreateWindow(name, index);
		}

		public void RemoveChild(UIElement element)
		{
			if (!element.IsChildOf(buttonHolder)) {
				throw new ArgumentException("Button was not a child of the SelectionBar", nameof(element));
			}

			buttonHolder.RemoveChild(element);
		}

		public UIElement GetChild(uint index)
		{
			return buttonHolder.GetChild(index);
		}

		public UIElement GetChild(string name, bool recursive = false)
		{
			return buttonHolder.GetChild(name, recursive);
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

		public void Dispose()
		{
			wholeWindow.HoverBegin -= OnHoverBegin;
			wholeWindow.HoverEnd -= OnHoverEnd;

			leftButton.Pressed -= ScrollButtonPressed;
			rightButton.Pressed -= ScrollButtonPressed;

			wholeWindow.Dispose();
			buttonHolder.Dispose();
			leftButton.Dispose();
			rightButton.Dispose();
			centralWindow.Dispose();
		}
	}
}
