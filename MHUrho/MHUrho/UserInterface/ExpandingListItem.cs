using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Packaging;
using Urho;
using Urho.Gui;
using Urho.Resources;
using Urho.Urho2D;

namespace MHUrho.UserInterface
{
	abstract class ExpandingListItem : UIElement, IDisposable
	{
		public bool IsSelected => CheckBox.Checked;

		public event Action<ExpandingListItem> ItemSelected;
		public event Action<ExpandingListItem> ItemDeselected;
		public event Action<ExpandingListItem> ItemSelectionChanged;



		/// <summary>
		/// Outer element containing the checkbox
		/// </summary>
		protected readonly UIElement OuterElement;
		/// <summary>
		/// Checkbox containing the fixed element and the collapsing element
		/// </summary>
		protected readonly CheckBox CheckBox;

		protected readonly UIElement FixedElement;
		protected readonly UIElement ExpandingElement;

		protected bool ExpandOnSelect;

		protected ExpandingListItem(MyGame game, string stylePath, bool expandOnSelect)
		{
			this.LayoutMode = LayoutMode.Horizontal;
			this.ExpandOnSelect = expandOnSelect;

			OuterElement = game.UI.LoadLayout(PackageManager.Instance.GetXmlFile("UI/ExpandingItemLayout.xml"),
											PackageManager.Instance.GetXmlFile(stylePath));

			AddChild(OuterElement);



			CheckBox = (CheckBox)OuterElement.GetChild("CheckBox");
			CheckBox.Toggled += CheckBoxToggled;

			FixedElement = CheckBox.GetChild("FixedElement");

			ExpandingElement = CheckBox.GetChild("CollapsingElement");
			ExpandingElement.Visible = false;

			//Set the size to effective min size, so all unused space with invisible elements gets hidden
			OuterElement.Size = new IntVector2(OuterElement.Size.X, OuterElement.EffectiveMinSize.Y);
			Size = new IntVector2(Size.X, EffectiveMinSize.Y);
		}

		void CheckBoxToggled(ToggledEventArgs args)
		{
			if (args.State)
			{
				OnSelected();
			}
			else
			{
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
			if (ExpandOnSelect)
			{
				Expand();
			}

			ItemSelected?.Invoke(this);
		}

		protected virtual void OnDeselected()
		{

			if (ExpandOnSelect)
			{
				Collapse();
			}

			ItemDeselected?.Invoke(this);
		}

		protected virtual void OnSelectionChanged()
		{
			ItemSelectionChanged?.Invoke(this);
		}

		protected virtual void Expand()
		{
			ExpandingElement.Visible = true;
		}

		protected virtual void Collapse()
		{
			ExpandingElement.Visible = false;

			OuterElement.Size = new IntVector2(OuterElement.Size.X, OuterElement.EffectiveMinSize.Y);
			Size = new IntVector2(Size.X, EffectiveMinSize.Y);
			Parent.Size = new IntVector2(Parent.Size.X, Parent.EffectiveMinSize.Y);
		}

		public new virtual void Dispose()
		{
			base.Dispose();
			CheckBox.Toggled -= CheckBoxToggled;

		ItemSelected = null;
		ItemDeselected = null;
		ItemSelectionChanged = null;

		ExpandingElement.Dispose();
			CheckBox.Dispose();
		}
	}
}
