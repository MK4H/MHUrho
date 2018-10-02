using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Packaging;
using MHUrho.StartupManagement;
using Urho;
using Urho.Gui;
using Urho.Urho2D;

namespace MHUrho.UserInterface
{
	class PackagePickingScreen : MenuScreen
	{
		class Screen : ScreenBase {

			readonly PackagePickingScreen proxy;

			Window window;
			ListView listView;
			Button selectButton;

			List<PackageListItem> items;

			public Screen(PackagePickingScreen proxy)
				:base(proxy)
			{
				this.proxy = proxy;
				this.items = new List<PackageListItem>();
				Game.UI.LoadLayoutToElement(MenuUIManager.MenuRoot, Game.ResourceCache, "UI/PackagePickingLayout.xml");

				window = (Window)MenuUIManager.MenuRoot.GetChild("PackagePickingWindow");
				window.Visible = false;

				listView = (ListView)window.GetChild("ListView");

				selectButton = (Button) window.GetChild("SelectButton", true);
				selectButton.Released += SelectButtonReleased;
				selectButton.Enabled = false;


				((Button)window.GetChild("BackButton", true)).Released += BackButtonReleased;

				foreach (var pack in PackageManager.Instance.AvailablePacks) {
					var newItem = new PackageListItem(pack, Game);
					items.Add(newItem);
					newItem.AddTo(listView);
					newItem.Selected += ItemSelected;
					newItem.Deselected += ItemDeselected;
				}

				window.Visible = true;
			}

		
			public override void Dispose()
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
						ItemSelectionConfirmed(item);
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
				listView.Selection = listView.FindItem(args.Element);

				foreach (var item in items) {
					if (args != item) {
						item.Deselect();
					}
				}

				args.Select();
				selectButton.Enabled = true;
			}

			void ItemDeselected(PackageListItem item)
			{
				if (item.Element == listView.SelectedItem) {
					selectButton.Enabled = false;
					listView.ClearSelection();
				}
			}


			void ItemSelectionConfirmed(PackageListItem item)
			{
				//TODO: Maybe switch to loading screen when loading package
				MenuUIManager.SwitchToLevelPickingScreen(PackageManager.Instance.LoadPackage(item.Pack));
			}
#if DEBUG
			public void SimulatePackagePick(string packageName)
			{
				foreach (var item in items) {
					if (item.Pack.Name == packageName) {
						ItemSelectionConfirmed(item);
					}
				}
			}

			public void SimulateBackButtonPress()
			{
				MenuUIManager.SwitchBack();
			}
#endif
		}


		protected override ScreenBase ScreenInstance {
			get => screen;
			set => screen = (Screen)value;
		}

		Screen screen;

		public PackagePickingScreen(MenuUIManager menuUIManager)
			:base(menuUIManager)
		{

		}


		public override void ExecuteAction(MenuScreenAction action)
		{
			if (action is PackagePickScreenAction myAction)
			{
				switch (myAction.Action)
				{
					case PackagePickScreenAction.Actions.Pick:
						screen.SimulatePackagePick(myAction.PackageName);
						break;
					case PackagePickScreenAction.Actions.Back:
						screen.SimulateBackButtonPress();
						break;
					default:
						throw new ArgumentOutOfRangeException();
				}
			}
			else
			{
				throw new ArgumentException("Action does not belong to the current screen", nameof(action));
			}
		}

		public override void Show()
		{
			if (screen != null) {
				return;
			}

			screen = new Screen(this);
#if DEBUG
			//screen.SimulatePackagePick("testRP2");
#endif

		}

	}
}
