using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Packaging;
using Urho;
using Urho.Gui;
using Urho.Urho2D;

namespace MHUrho.UserInterface
{
	abstract class LevelPickingItem : IDisposable {
		public UIElement Element { get; private set; }

		public bool IsSelected => checkBox.Checked;

		public event Action<LevelPickingItem> Selected;
		public event Action<LevelPickingItem> Deselected;
		public event Action<LevelPickingItem> SelectionChanged;

		readonly CheckBox checkBox;

		protected LevelPickingItem(MyGame game, Texture2D thumbnail, string name)
		{
			Element = game.UI.LoadLayout(PackageManager.Instance.GetXmlFile("UI/LevelItemLayout.xml"),
										PackageManager.Instance.GetXmlFile("UI/LevelItemStyle.xml"));

			checkBox = (CheckBox)Element.GetChild("CheckBox");
			checkBox.Toggled += CheckBoxToggled;

			var thumbnailElement = (BorderImage)checkBox.GetChild("Thumbnail", true);
			thumbnailElement.Texture = thumbnail;
			thumbnailElement.ImageRect = new IntRect(0, 0, thumbnail.Width, thumbnail.Height);

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

		public virtual void Select()
		{
			//If checked changed, automatically calls CheckBoxToggled
			checkBox.Selected = true;
		}

		public virtual void Deselect()
		{
			//If checked changed, automatically calls CheckBoxToggled
			checkBox.Selected = false;
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

		public virtual void Dispose()
		{
			checkBox.Toggled -= CheckBoxToggled;

			Element.RemoveAllChildren();
			Element.Remove();

			checkBox.Dispose();
			Element.Dispose();
			
		}
	}

	class LevelPickingLevelItem : LevelPickingItem
    {

		public LevelRep Level { get; private set; }

		readonly UIElement descriptionElement;

		public LevelPickingLevelItem(LevelRep level, MyGame game)
			:base(game, level.Thumbnail, level.Name)
		{
			descriptionElement = Element.GetChild("DescriptionElement", true);

			((Text)Element.GetChild("DescriptionText", true)).Value = level.Description;
		}

		public override void Dispose()
		{
			descriptionElement.Dispose();
			base.Dispose();
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
