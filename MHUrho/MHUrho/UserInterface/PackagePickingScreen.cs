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

			readonly Window window;
			readonly ListView listView;
			readonly Button selectButton;


			public Screen(PackagePickingScreen proxy)
				:base(proxy)
			{
				this.proxy = proxy;

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
					newItem.ItemSelected += ItemSelected;
					newItem.ItemDeselected += ItemDeselected;
					listView.AddItem(newItem);
				}

				window.Visible = true;
			}

		
			public override void Dispose()
			{
				selectButton.Released -= SelectButtonReleased;


				foreach (var item in GetItems()) {
					item.Dispose();
				}

				listView.RemoveAllItems();
				listView.Dispose();
				selectButton.Dispose();

				window.RemoveAllChildren();
				window.Remove();
				window.Dispose();
			}

			void SelectButtonReleased(ReleasedEventArgs args)
			{
				foreach (var item in GetItems()) {
					if (item == listView.SelectedItem) {
						ItemSelectionConfirmed(item);
						return;
					}
				}
			}

			void BackButtonReleased(ReleasedEventArgs args)
			{
				MenuUIManager.SwitchBack();
			}

			void ItemSelected(ExpandingListItem args)
			{
				listView.Selection = listView.FindItem(args);

				foreach (var item in GetItems()) {
					if (args != item) {
						item.Deselect();
					}
				}

				args.Select();
				selectButton.Enabled = true;
			}

			void ItemDeselected(ExpandingListItem item)
			{
				if (item == listView.SelectedItem) {
					selectButton.Enabled = false;
					listView.ClearSelection();
				}
			}


			void ItemSelectionConfirmed(PackageListItem item)
			{
				//TODO: Maybe switch to loading screen when loading package
				MenuUIManager.SwitchToLevelPickingScreen(PackageManager.Instance.LoadPackage(item.Pack));
			}

			IEnumerable<PackageListItem> GetItems()
			{
				for (uint i = 0; i < listView.NumItems; i++) {
					yield return (PackageListItem)listView.GetItem(i);
				}
			}

			PackageListItem GetSelectedItem()
			{
				return (PackageListItem) listView.SelectedItem;
			}
#if DEBUG
			public void SimulatePackagePick(string packageName)
			{
				foreach (var item in GetItems()) {
					if (item.Pack.Name == packageName) {
						ItemSelectionConfirmed(item);
						return;
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
