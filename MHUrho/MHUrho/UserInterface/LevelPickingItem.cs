using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Packaging;
using Urho.Gui;
using Urho.Urho2D;

namespace MHUrho.UserInterface
{
	abstract class LevelPickingItem {
		public UIElement Element { get; private set; }

		public bool IsSelected => checkBox.Checked;

		public event Action<LevelPickingItem> Selected;
		public event Action<LevelPickingItem> Deselected;
		public event Action<LevelPickingItem> SelectionChanged;

		CheckBox checkBox;
		UIElement descriptionElement;

		protected LevelPickingItem(MyGame game, Texture2D thumbnail, string name)
		{
			Element = game.UI.LoadLayout(PackageManager.Instance.GetXmlFile("UI/LevelItemLayout.xml"),
										PackageManager.Instance.GetXmlFile("UI/LevelItemStyle.xml"));

			checkBox = (CheckBox)Element.GetChild("CheckBox");
			checkBox.Toggled += CheckBoxToggled;

			var thumbnailElement = (BorderImage)checkBox.GetChild("Thumbnail", true);
			thumbnailElement.Texture = thumbnail;

			var nameElement = (Text)checkBox.GetChild("NameText", true);
			nameElement.Value = name;



		}

		void CheckBoxToggled(ToggledEventArgs args)
		{
			if (args.State) {
				OnSelected();
			}
			else {
				OnDeselected();
			}

			OnSelectionChanged();
		}

		protected virtual void OnSelected()
		{
			Selected?.Invoke(this);
		}

		protected virtual void OnDeselected()
		{
			Deselected?.Invoke(this);
		}

		protected virtual void OnSelectionChanged()
		{
			SelectionChanged?.Invoke(this);
		}
	}

	class LevelPickingLevelItem : LevelPickingItem
    {

		public LevelRep Level { get; private set; }

		UIElement descriptionElement;

		public LevelPickingLevelItem(LevelRep level, MyGame game)
			:base(game, level.Thumbnail, level.Name)
		{
			descriptionElement = Element.GetChild("DescriptionElement", true);

			((Text)Element.GetChild("DescriptionText", true)).Value = level.Description;
		}


		protected override void OnSelected()
		{
			Expand();
			base.OnSelected();
		}

		protected override void OnDeselected()
		{
			Collapse();
			base.OnDeselected();
		}

		void Expand()
		{
			descriptionElement.Visible = true;
		}

		void Collapse()
		{
			descriptionElement.Visible = false;
		}
	}

	class LevelPickingNewLevelItem : LevelPickingItem
	{
		public LevelPickingNewLevelItem(MyGame game, Texture2D thumbnail, string message)
			:base(game, thumbnail, message)
		{

		}
	}
}
