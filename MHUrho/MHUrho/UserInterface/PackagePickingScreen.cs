using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Packaging;
using Urho;
using Urho.Gui;
using Urho.Urho2D;

namespace MHUrho.UserInterface
{
    class PackagePickingScreen : MenuScreen
    {
		public override bool Visible {
			get => throw new NotImplementedException();
			set => throw new NotImplementedException();
		}

		Window window;
		ListView listView;

		List<PackageListItem> items;

		public PackagePickingScreen(MyGame game, MenuUIManager menuUIManager)
			: base(game, menuUIManager)
		{
			this.items = new List<PackageListItem>();

			UI.LoadLayoutToElement(UI.Root, game.ResourceCache, "UI/PackagePickingLayout.xml");

			window = (Window)UI.Root.GetChild("PackagePickingWindow");
			window.Visible = false;

			listView = (ListView) window.GetChild("PackageListView");

			((Button)window.GetChild("PickButton", true)).Released += PickButtonReleased;
			((Button)window.GetChild("ExitButton", true)).Released += BackButtonReleased;
		}

		

		public override void Show()
		{
			foreach (var pack in PackageManager.Instance.AvailablePacks) {
				//TODO: Just for testing
				for (int i = 0; i < 10; i++) {
					var newItem = new PackageListItem(pack, Game);
					items.Add(newItem);
					newItem.AddTo(listView);
					newItem.Selected += ItemSelected;
				}
			}

			window.Visible = true;
		}

		

		public override void Hide()
		{
			window.Visible = false;
			listView.RemoveAllItems();
			foreach (var item in items) {
				item.Dispose();
			}

			items.Clear();
		}

		void PickButtonReleased(ReleasedEventArgs args)
		{
			foreach (var item in items) {
				if (item.Element == listView.SelectedItem) {

				}
			}
		}

		void BackButtonReleased(ReleasedEventArgs args)
		{
			MenuUIManager.SwitchBack();
		}

		void ItemSelected(PackageListItem args)
		{
			foreach (var item in items) {
				if (args != item) {
					item.Deselect();
				}
			}
		}

	}
}
