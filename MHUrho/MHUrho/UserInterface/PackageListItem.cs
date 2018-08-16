using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Helpers;
using MHUrho.Packaging;
using Urho;
using Urho.Gui;

namespace MHUrho.UserInterface
{
    class PackageListItem : IDisposable
    {
		public IAvailablePack Pack { get; private set; }

		public UIElement Element { get; private set; }

		public bool IsSelected => checkBox.Checked;

		public event Action<PackageListItem> Selected;
		public event Action<PackageListItem> Deselected;
		public event Action<PackageListItem> SelectionChanged;

		CheckBox checkBox;
		Text descriptionText;
		UIElement descriptionElement;
		UIElement bottomElement;

		public PackageListItem(IAvailablePack pack, MyGame game)
		{
			this.Pack = pack;
			Element = game.UI.LoadLayout(PackageManager.Instance.GetXmlFile("UI/AvailablePackItemLayout.xml"),
										PackageManager.Instance.GetXmlFile("UI/AvailablePackItemStyle.xml"));

			checkBox = (CheckBox)Element.GetChild("CheckBox");
			checkBox.Toggled += CheckBoxToggled;

			var thumbnail = (BorderImage)checkBox.GetChild("Thumbnail", true);
			thumbnail.Texture = pack.Thumbnail;

			var name = (Text)checkBox.GetChild("NameText", true);
			name.Value = pack.Name;

			descriptionElement = checkBox.GetChild("DescriptionElement", true);
			bottomElement = checkBox.GetChild("BottomElement", true);

			descriptionText = (Text)checkBox.GetChild("DescriptionText", true);
			descriptionText.Value = pack.Description;
		}

		public void AddTo(ListView listView)
		{
			listView.AddItem(Element);
		}

		public void Dispose()
		{
			checkBox.Toggled -= CheckBoxToggled;
			checkBox.Dispose();
			descriptionText.Dispose();
			descriptionElement.Dispose();
			bottomElement.Dispose();
		}

		public void Select()
		{
			checkBox.Checked = true;
		}


		public void Deselect()
		{
			checkBox.Checked = false;
		}
		void CheckBoxToggled(ToggledEventArgs args)
		{
			if (args.State) {
				//If the description text is clipped, move it to bottomElement and expand the checkBox
				if (descriptionText.Size.Y > descriptionElement.Size.Y) {
					descriptionText.SetFixedWidth(checkBox.Size.X - 10);

					bottomElement.LayoutBorder = new IntRect(0, 5, 0, 5);
					bottomElement.AddChild(descriptionText);

					descriptionText.UpdateLayout();
					bottomElement.UpdateLayout();
					checkBox.UpdateLayout();
					Element.UpdateLayout();
					Element.Parent.UpdateLayout();
				}
				

				Selected?.Invoke(this);
			}
			else {
				if (descriptionText.Parent == bottomElement) {
					descriptionText.SetFixedWidth(descriptionElement.Size.X - (descriptionElement.LayoutBorder.Left + descriptionElement.LayoutBorder.Right));
					descriptionElement.AddChild(descriptionText);
					bottomElement.LayoutBorder = new IntRect(0, 0, 0, 0);

					descriptionText.UpdateLayout();
					bottomElement.UpdateLayout();
					checkBox.UpdateLayout();
					Element.UpdateLayout();
					Element.Parent.UpdateLayout();
				}
			
				Deselected?.Invoke(this);
			}

			SelectionChanged?.Invoke(this);
		}
    }
}
