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

		public BorderImage CreateBorderImage(string name = "", uint index = 4294967295)
		{
			BorderImage newBorderImage = buttonHolder.CreateBorderImage(name, index);
			InitElement(newBorderImage);
			return newBorderImage;
		}

		public Button CreateButton(string name = "", uint index = 4294967295)
		{
			Button newButton = buttonHolder.CreateButton(name, index);
			InitElement(newButton);
			return newButton;
		}

		public CheckBox CreateCheckBox(string name = "", uint index = 4294967295)
		{
			CheckBox newCheckBox = buttonHolder.CreateCheckBox(name, index);
			InitElement(newCheckBox);
			return newCheckBox;
		}

		public DropDownList CreateDropDownList(string name = "", uint index = 4294967295)
		{
			DropDownList newDropDownList = buttonHolder.CreateDropDownList(name, index);
			InitElement(newDropDownList);
			return newDropDownList;
		}

		public LineEdit CreateLineEdit(string name = "", uint index = 4294967295)
		{
			LineEdit newLineEdit = buttonHolder.CreateLineEdit(name, index);
			InitElement(newLineEdit);
			return newLineEdit;
		}

		public ListView CreateListView(string name = "", uint index = 4294967295)
		{
			ListView newListView = buttonHolder.CreateListView(name, index);
			InitElement(newListView);
			return newListView;
		}

		public Menu CreateMenu(string name = "", uint index = 4294967295)
		{
			Menu newMenu = buttonHolder.CreateMenu(name, index);
			InitElement(newMenu);
			return newMenu;
		}

		public ScrollBar CreateScrollBar(string name = "", uint index = 4294967295)
		{
			ScrollBar newScrollBar = buttonHolder.CreateScrollBar(name, index);
			InitElement(newScrollBar);
			return newScrollBar;
		}

		public ScrollView CreateScrollView(string name = "", uint index = 4294967295)
		{
			ScrollView newScrollView = buttonHolder.CreateScrollView(name, index);
			InitElement(newScrollView);
			return newScrollView;
		}

		public Slider CreateSlider(string name = "", uint index = 4294967295)
		{
			Slider newSlider = buttonHolder.CreateSlider(name, index);
			InitElement(newSlider);
			return newSlider;
		}

		public Sprite CreateSprite(string name = "", uint index = 4294967295)
		{
			Sprite newSprite = buttonHolder.CreateSprite(name, index);
			InitElement(newSprite);
			return newSprite;
		}

		public Text CreateText(string name = "", uint index = 4294967295)
		{
			Text newText = buttonHolder.CreateText(name, index);
			InitElement(newText);
			return newText;
		}

		public ToolTip CreateToolTip(string name = "", uint index = 4294967295)
		{
			ToolTip newToolTip = buttonHolder.CreateToolTip(name, index);
			InitElement(newToolTip);
			return newToolTip;
		}

		public Window CreateWindow(string name = "", uint index = 4294967295)
		{
			Window newWindow = buttonHolder.CreateWindow(name, index);
			InitElement(newWindow);
			return newWindow;
		}

		public void RemoveChild(UIElement element)
		{
			if (!element.IsChildOf(buttonHolder)) {
				throw new ArgumentException("Button was not a child of the SelectionBar", nameof(element));
			}
			element.HoverBegin -= OnHoverBegin;
			element.HoverEnd -= OnHoverEnd;
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

		void InitElement(UIElement element)
		{
			element.Visible = false;
			element.HoverBegin += OnHoverBegin;
			element.HoverEnd += OnHoverEnd;
		}
	}
}
