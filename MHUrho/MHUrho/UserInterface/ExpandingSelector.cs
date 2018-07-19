using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using Urho.Gui;

namespace MHUrho.UserInterface
{
	delegate void ElementSelectedDelegate(UIElement newSelectedElement, UIElement oldSelectedElement);
    class ExpandingSelector
    {
		public bool Expanded { get; private set; }

		public event Action<HoverBeginEventArgs> HoverBegin;
		public event Action<HoverEndEventArgs> HoverEnd;
		public event ElementSelectedDelegate Selected;

		readonly CheckBox checkBox;
		readonly ExpansionWindow expansionWindow;

		readonly List<CheckBox> checkBoxes;

		readonly Texture defaultTexture;
		readonly IntRect defaultImageRect;

		CheckBox currentSelected;

		

		public ExpandingSelector(CheckBox checkBox, ExpansionWindow expansionWindow)
		{
			this.checkBox = checkBox;
			this.defaultTexture = checkBox.Texture;
			this.defaultImageRect = checkBox.ImageRect;

			//TODO: Style
			checkBox.Toggled += MainBoxToggled;

			//TODO: Style
			this.expansionWindow = expansionWindow;
			this.checkBoxes = new List<CheckBox>();
		}

		public void AddCheckBox(CheckBox box)
		{
			expansionWindow.AddChild(box);
			checkBoxes.Add(box);
			box.Toggled += ExpandedBoxToggled;
		}

		public void RemoveCheckBox(CheckBox box)
		{
			if (!checkBoxes.Contains(box)) {
				throw new ArgumentException("Provided box was not registered in the window", nameof(box));
			}

			checkBoxes.Remove(box);
			if (box.Checked) {

				if (checkBoxes.Count != 0) {
					SelectedBox(checkBoxes[0]);
				}
				else {
					SetDefaultTexture();
				}
			}

			box.Toggled -= ExpandedBoxToggled;
			expansionWindow.RmoveChild(box);
		}

		void MainBoxToggled(ToggledEventArgs e)
		{
			//Toggled
			if (e.State) {
				ShowExpansionWindow();
			}
			else {
				HideExpansionWindow();
			}
			
		}

		void ExpandedBoxToggled(ToggledEventArgs e)
		{
			if (e.Element == currentSelected) {
				return;
			}

			if (e.State == false) {
				throw new InvalidOperationException("Checkbox getting unchecked by an event");
			}

			SelectedBox((CheckBox) e.Element);

			checkBox.HoverBegin += OnHoverBegin;
			checkBox.HoverEnd += OnHoverEnd;
		}

		void ShowExpansionWindow()
		{
			expansionWindow.Visible = true;
			expansionWindow.HideAll();

			foreach (var box in checkBoxes) {
				box.Visible = true;
			}

			Expanded = true;
		}

		void HideExpansionWindow()
		{
			expansionWindow.Visible = false;
			foreach (var box in checkBoxes) {
				box.Visible = false;
			}

			Expanded = false;
		}

		void SelectedBox(CheckBox box)
		{
			UIElement oldSelected = currentSelected;

			currentSelected.Checked = false;
			currentSelected = box;

			checkBox.Texture = box.Texture;
			checkBox.ImageRect = box.ImageRect;

			HideExpansionWindow();

			Selected?.Invoke(box, oldSelected);
		}

		void SetDefaultTexture()
		{
			checkBox.Texture = defaultTexture;
			checkBox.ImageRect = defaultImageRect;
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
