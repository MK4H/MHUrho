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
		class Screen : IDisposable {

			readonly PackagePickingScreen proxy;
			MyGame Game => proxy.Game;
			MenuUIManager MenuUIManager => proxy.menuUIManager;


			Window window;
			ListView listView;

			List<PackageListItem> items;

			public Screen(PackagePickingScreen proxy)
			{
				this.proxy = proxy;
				this.items = new List<PackageListItem>();
				Game.UI.LoadLayoutToElement(Game.UI.Root, Game.ResourceCache, "UI/PackagePickingLayout.xml");

				window = (Window)Game.UI.Root.GetChild("PackagePickingWindow");
				window.Visible = false;

				listView = (ListView)window.GetChild("ListView");

				((Button)window.GetChild("SelectButton", true)).Released += SelectButtonReleased;
				((Button)window.GetChild("BackButton", true)).Released += BackButtonReleased;

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

			public void Dispose()
			{

				listView.RemoveAllItems();

				foreach (var item in items) {
					item.Dispose();
				}

				window.RemoveAllChildren();
				window.Remove();
				window.Dispose();
				listView.Dispose();
			}

			void SelectButtonReleased(ReleasedEventArgs args)
			{
				foreach (var item in items) {
					if (item.Element == listView.SelectedItem) {
						//TODO: Maybe switch to loading screen
						MenuUIManager.SwitchToLevelPickingScreen(PackageManager.Instance.LoadPackage(item.Pack));
						return;
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

				args.Select();
				listView.Selection = listView.FindItem(args.Element);
			}
		}


		public override bool Visible {
			get => screen != null;
			set {
				if (value) {
					Show();
				}
				else {
					Hide();
				}
			}
		}



		MyGame Game => MyGame.Instance;
		readonly MenuUIManager menuUIManager;

		Screen screen;

		public PackagePickingScreen(MenuUIManager menuUIManager)
		{
			this.menuUIManager = menuUIManager;
		}

		

		public override void Show()
		{
			if (screen != null) {
				return;
			}

			screen = new Screen(this);
		}

		

		public override void Hide()
		{
			if (screen == null) {
				return;
			}

			screen.Dispose();
			screen = null;
		}

	}
}
