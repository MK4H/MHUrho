using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Packaging;
using Urho;
using Urho.Gui;
using Urho.Urho2D;

namespace MHUrho.UserInterface
{
	abstract class LevelPickingItem : UIElement, IDisposable {
		public bool IsSelected => CheckBox.Checked;

		public event Action<LevelPickingItem> ItemSelected;
		public event Action<LevelPickingItem> ItemDeselected;
		public event Action<LevelPickingItem> ItemSelectionChanged;

		protected readonly UIElement CollapsingElement;

		protected readonly UIElement TopElement;
		protected readonly CheckBox CheckBox;


		protected LevelPickingItem(MyGame game, Texture2D thumbnail, string name)
		{
			this.LayoutMode = LayoutMode.Horizontal;

			TopElement = game.UI.LoadLayout(PackageManager.Instance.GetXmlFile("UI/LevelItemLayout.xml"),
											PackageManager.Instance.GetXmlFile("UI/LevelItemStyle.xml"));

			AddChild(TopElement);



			CheckBox = (CheckBox)TopElement.GetChild("CheckBox");
			CheckBox.Toggled += CheckBoxToggled;

			var thumbnailElement = (BorderImage)CheckBox.GetChild("Thumbnail", true);
			thumbnailElement.Texture = thumbnail;
			thumbnailElement.ImageRect = new IntRect(0, 0, thumbnail.Width, thumbnail.Height);

			var nameElement = (Text)CheckBox.GetChild("NameText", true);
			nameElement.Value = name;

			CollapsingElement = CheckBox.GetChild("CollapsingElement", true);
			CollapsingElement.Visible = false;
 
			//Set the size to effective min size, so all unused space with invisible elements gets hidden
			TopElement.Size = new IntVector2(TopElement.Size.X, TopElement.EffectiveMinSize.Y);
			Size = new IntVector2(Size.X, EffectiveMinSize.Y);
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
			CheckBox.Checked = true;
		}

		public virtual void Deselect()
		{
			//If checked changed, automatically calls CheckBoxToggled
			CheckBox.Checked = false;
		}

		protected virtual void OnSelected()
		{
			ItemSelected?.Invoke(this);
		}

		protected virtual void OnDeselected()
		{
			ItemDeselected?.Invoke(this);
		}

		protected virtual void OnSelectionChanged()
		{
			ItemSelectionChanged?.Invoke(this);
		}

		public new virtual void Dispose()
		{
			base.Dispose();
			CheckBox.Toggled -= CheckBoxToggled;

			CollapsingElement.Dispose();
			CheckBox.Dispose();		
		}
	}

	class LevelPickingLevelItem : LevelPickingItem {

		public LevelRep Level { get; private set; }

		public LevelPickingLevelItem(LevelRep level, MyGame game)
			:base(game, level.Thumbnail, level.Name)
		{
			this.Level = level;
			((Text)GetChild("DescriptionText", true)).Value = level.Description;
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
			CollapsingElement.Visible = true;
		}

		void Collapse()
		{
			CollapsingElement.Visible = false;

			TopElement.Size = new IntVector2(TopElement.Size.X, TopElement.EffectiveMinSize.Y);
			Size = new IntVector2(Size.X, EffectiveMinSize.Y);
			Parent.Size = new IntVector2(Parent.Size.X, Parent.EffectiveMinSize.Y);
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
