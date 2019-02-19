using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;
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
			readonly Button addButton;
			readonly Button removeButton;
			readonly Button selectButton;
			readonly Button backButton;

			public Screen(PackagePickingScreen proxy)
				:base(proxy)
			{
				this.proxy = proxy;

				Game.UI.LoadLayoutToElement(MenuUIManager.MenuRoot, Game.ResourceCache, "UI/PackagePickingLayout.xml");

				window = (Window)MenuUIManager.MenuRoot.GetChild("PackagePickingWindow");
				window.Visible = false;

				listView = (ListView)window.GetChild("ListView");

				addButton = (Button) window.GetChild("AddButton", true);
				addButton.Released += AddButtonReleased;

				removeButton = (Button) window.GetChild("RemoveButton", true);
				removeButton.Released += RemoveButtonReleased;
				removeButton.Enabled = false;

				selectButton = (Button) window.GetChild("SelectButton", true);
				selectButton.Released += SelectButtonReleased;
				selectButton.Enabled = false;


				backButton = (Button)window.GetChild("BackButton", true);
				backButton.Released += BackButtonReleased;

				foreach (var pack in PackageManager.Instance.AvailablePacks) {
					AddItem(pack);
				}

				window.Visible = true;
			}

			public override void EnableInput()
			{
				window.SetDeepEnabled(true);
			}

			public override void DisableInput()
			{
				window.SetDeepEnabled(false);
			}

			public override void ResetInput()
			{
				window.ResetDeepEnabled();
			}

			public override void Dispose()
			{
				addButton.Released -= AddButtonReleased;
				removeButton.Released -= RemoveButtonReleased;
				selectButton.Released -= SelectButtonReleased;
				backButton.Released -= BackButtonReleased;


				foreach (var item in GetItems()) {
					item.Dispose();
				}

			
				listView.RemoveAllItems();
				listView.Dispose();

				addButton.Dispose();
				removeButton.Dispose();
				selectButton.Dispose();
				backButton.Dispose();

				window.RemoveAllChildren();
				window.Remove();
				window.Dispose();
			}

			void RemoveButtonReleased(ReleasedEventArgs obj)
			{
				PackageListItem selectedItem = GetSelectedItem();

				try {
					PackageManager.Instance.RemoveGamePack(selectedItem.Pack);
					RemoveItem(selectedItem);
				}
				catch (FatalPackagingException e) {
					Game.ErrorExit(e.Message);
				}
				catch (ArgumentException e) {
					MenuUIManager.ErrorPopUp.DisplayError("Error", e.Message, proxy);
				}
				catch (PackageLoadingException e) {
					MenuUIManager.ErrorPopUp.DisplayError("Error", e.Message, proxy);
				}
			}

			async void AddButtonReleased(ReleasedEventArgs obj)
			{
				IPathResult result = await MenuUIManager
											.FileBrowsingPopUp
											.Request(MyGame.Files.PackageDirectoryAbsolutePath,
													SelectOption.File);
				if (result == null) {
					return;
				}

				try {
					var newPack = PackageManager.Instance.AddGamePack(result.RelativePath);
					AddItem(newPack);
				}
				catch (FatalPackagingException e) {
					Game.ErrorExit(e.Message);
				}
				catch (ArgumentException e) {
					await MenuUIManager.ErrorPopUp.DisplayError("Error", e.Message, proxy);
				}
				catch (PackageLoadingException e) {
					await MenuUIManager.ErrorPopUp.DisplayError("Error", e.Message, proxy);
				}
			
			}

			void SelectButtonReleased(ReleasedEventArgs args)
			{
				ItemSelectionConfirmed(GetSelectedItem());
			}

			void BackButtonReleased(ReleasedEventArgs args)
			{
				MenuUIManager.SwitchBack();
			}

			void AddItem(GamePackRep gamePack)
			{
				var newItem = new PackageListItem(gamePack, Game);
				newItem.ItemSelected += ItemSelected;
				newItem.ItemDeselected += ItemDeselected;
				listView.AddItem(newItem);
			}

			void RemoveItem(PackageListItem item)
			{
				listView.RemoveItem(item);
				item.Dispose();
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
				removeButton.Enabled = true;
			}

			void ItemDeselected(ExpandingListItem item)
			{
				if (item == listView.SelectedItem) {
					selectButton.Enabled = false;
					removeButton.Enabled = false;
					listView.ClearSelection();
				}
			}


			async void ItemSelectionConfirmed(PackageListItem item)
			{
				LoadingScreen screen = MenuUIManager.SwitchToLoadingScreen();
				try {
					GamePack package = await PackageManager.Instance.LoadPackage(item.Pack, screen.LoadingWatcher);
					MenuUIManager.SwitchToLevelPickingScreen(package);
				}
				catch (PackageLoadingException e) {
					//Switch back from loading screen
					MenuUIManager.SwitchBack();
					MenuUIManager.ErrorPopUp.DisplayError("Error", e.Message, proxy);
				}
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

		}

	}
}
