using System;
using System.Collections.Generic;
using System.Text;
using Urho.Gui;

namespace MHUrho.UserInterface
{
    public abstract class UIElementWrapper : IDisposable
    {
		public event Action<HoverBeginEventArgs> HoverBegin;
		public event Action<HoverEndEventArgs> HoverEnd;

		protected abstract UIElement ElementWithChildren { get; }

		public BorderImage CreateBorderImage(string name = "", uint index = 4294967295)
		{
			BorderImage newBorderImage = ElementWithChildren.CreateBorderImage(name, index);
			InitElement(newBorderImage);
			return newBorderImage;
		}

		public Button CreateButton(string name = "", uint index = 4294967295)
		{
			Button newButton = ElementWithChildren.CreateButton(name, index);
			InitElement(newButton);
			return newButton;
		}

		public CheckBox CreateCheckBox(string name = "", uint index = 4294967295)
		{
			CheckBox newCheckBox = ElementWithChildren.CreateCheckBox(name, index);
			InitElement(newCheckBox);
			return newCheckBox;
		}

		public DropDownList CreateDropDownList(string name = "", uint index = 4294967295)
		{
			DropDownList newDropDownList = ElementWithChildren.CreateDropDownList(name, index);
			InitElement(newDropDownList);
			return newDropDownList;
		}

		public LineEdit CreateLineEdit(string name = "", uint index = 4294967295)
		{
			LineEdit newLineEdit = ElementWithChildren.CreateLineEdit(name, index);
			InitElement(newLineEdit);
			return newLineEdit;
		}

		public ListView CreateListView(string name = "", uint index = 4294967295)
		{
			ListView newListView = ElementWithChildren.CreateListView(name, index);
			InitElement(newListView);
			return newListView;
		}

		public Menu CreateMenu(string name = "", uint index = 4294967295)
		{
			Menu newMenu = ElementWithChildren.CreateMenu(name, index);
			InitElement(newMenu);
			return newMenu;
		}

		public ScrollBar CreateScrollBar(string name = "", uint index = 4294967295)
		{
			ScrollBar newScrollBar = ElementWithChildren.CreateScrollBar(name, index);
			InitElement(newScrollBar);
			return newScrollBar;
		}

		public ScrollView CreateScrollView(string name = "", uint index = 4294967295)
		{
			ScrollView newScrollView = ElementWithChildren.CreateScrollView(name, index);
			InitElement(newScrollView);
			return newScrollView;
		}

		public Slider CreateSlider(string name = "", uint index = 4294967295)
		{
			Slider newSlider = ElementWithChildren.CreateSlider(name, index);
			InitElement(newSlider);
			return newSlider;
		}

		public Sprite CreateSprite(string name = "", uint index = 4294967295)
		{
			Sprite newSprite = ElementWithChildren.CreateSprite(name, index);
			InitElement(newSprite);
			return newSprite;
		}

		public Text CreateText(string name = "", uint index = 4294967295)
		{
			Text newText = ElementWithChildren.CreateText(name, index);
			InitElement(newText);
			return newText;
		}

		public ToolTip CreateToolTip(string name = "", uint index = 4294967295)
		{
			ToolTip newToolTip = ElementWithChildren.CreateToolTip(name, index);
			InitElement(newToolTip);
			return newToolTip;
		}

		public Window CreateWindow(string name = "", uint index = 4294967295)
		{
			Window newWindow = ElementWithChildren.CreateWindow(name, index);
			InitElement(newWindow);
			return newWindow;
		}

		public void RemoveChild(UIElement childElement)
		{
			if (!childElement.IsChildOf(ElementWithChildren)) {
				throw new ArgumentException("Button was not a child of the ui element", nameof(childElement));
			}
			childElement.HoverBegin -= OnHoverBegin;
			childElement.HoverEnd -= OnHoverEnd;
			ElementWithChildren.RemoveChild(childElement);
		}

		public UIElement GetChild(uint index)
		{
			return ElementWithChildren.GetChild(index);
		}

		public UIElement GetChild(string name, bool recursive = false)
		{
			return ElementWithChildren.GetChild(name, recursive);
		}

		protected virtual void OnHoverBegin(HoverBeginEventArgs e)
		{
			HoverBegin?.Invoke(e);
		}
		protected virtual void OnHoverEnd(HoverEndEventArgs e)
		{
			HoverEnd?.Invoke(e);
		}

		void InitElement(UIElement element)
		{
			element.Visible = false;
			element.HoverBegin += OnHoverBegin;
			element.HoverEnd += OnHoverEnd;
		}

		public abstract void Dispose();
	}
}
