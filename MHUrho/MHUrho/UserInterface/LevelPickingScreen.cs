using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.Packaging;
using Urho.Gui;
using Urho.Urho2D;

namespace MHUrho.UserInterface
{
	class LevelPickingScreen : MenuScreen
	{

		class Screen : IDisposable {

			LevelPickingScreen proxy;

			MyGame Game => proxy.Game;
			MenuUIManager MenuUIManager => proxy.menuUIManager;

			Window window;
			ListView listView;
			Button editButton;
			Button playButton;

			const string newLevelItemTexturePath = "Textures/NewLevelItem.png";
			readonly Texture2D newLevelItemTexture;

			readonly List<LevelPickingItem> items;

			public Screen(LevelPickingScreen proxy)
			{
				this.proxy = proxy;

				items = new List<LevelPickingItem>();

				newLevelItemTexture = PackageManager.Instance.GetTexture2D(newLevelItemTexturePath);

				Game.UI.LoadLayoutToElement(MenuUIManager.MenuRoot, Game.ResourceCache, "UI/LevelPickingLayout.xml");

				window = (Window)MenuUIManager.MenuRoot.GetChild("LevelPickingWindow");

				listView = (ListView)window.GetChild("ListView");

				editButton = (Button)window.GetChild("EditButton", true);
				playButton = (Button)window.GetChild("PlayButton", true);

				editButton.Released += EditButtonReleased;
				playButton.Released += PlayButtonReleased;
				((Button)window.GetChild("BackButton", true)).Released += BackButtonReleased;

				GetLevels(listView);
			}

			public void Dispose()
			{
				editButton.Released -= EditButtonReleased;
				playButton.Released -= PlayButtonReleased;
				((Button)window.GetChild("BackButton", true)).Released -= BackButtonReleased;

				foreach (var item in items) {
					item.Dispose();
				}

				window.RemoveAllChildren();
				window.Remove();

				window.Dispose();
				listView.Dispose();
				editButton.Dispose();
				playButton.Dispose();
			}

			void GetLevels(ListView listView)
			{
				AddItem(new LevelPickingNewLevelItem(Game, newLevelItemTexture, "Create New Level"), listView);
				
				foreach (var level in proxy.Package.Levels) {
					AddItem(new LevelPickingLevelItem(level, Game), listView);
				}
			}

			void EditButtonReleased(ReleasedEventArgs obj)
			{
				foreach (var item in items) {
					if (item.IsSelected) {
						if (item is LevelPickingLevelItem levelItem) {
							SwitchToEditingExistingLevel(levelItem.Level);
							return;
						}
						else if (item is LevelPickingNewLevelItem newItem) {
							SwitchToEditingNewLevel();
						}
						else {
							throw new InvalidOperationException("Edit button was pressed when it should not have been possible to press");
						}
					}
				}
			}

			void PlayButtonReleased(ReleasedEventArgs args)
			{
				foreach (var item in items) {
					if (item.IsSelected) {
						if (item is LevelPickingLevelItem levelItem) {
							SwitchToPlayingLevel(levelItem.Level);
							return;
						}
						else {
							throw new InvalidOperationException("Play button was pressed when it should not have been possible to press");
						}
					}
				}
				
			}

			void BackButtonReleased(ReleasedEventArgs args)
			{
				MenuUIManager.SwitchBack();
			}

			void ItemSelected(LevelPickingItem selectedItem)
			{
				foreach (var item in items) {
					if (selectedItem != item) {
						item.Deselect();
					}
				}

				listView.Selection = listView.FindItem(selectedItem.Element);

				if (selectedItem is LevelPickingNewLevelItem) {
					playButton.SetStyle("DisabledButton");
					playButton.Enabled = false;
				}
				else {
					playButton.SetStyleAuto();
					playButton.Enabled = true;
				}
			}

			void AddItem(LevelPickingItem newItem, ListView listView)
			{
				items.Add(newItem);
				newItem.Selected += ItemSelected;
				listView.AddItem(newItem.Element);
			}

			void SwitchToEditingExistingLevel(LevelRep level)
			{
				MenuUIManager.SwitchToLevelCreationScreen(level);
			}

			void SwitchToEditingNewLevel()
			{
				MenuUIManager.SwitchToLevelCreationScreen(null);
			}

			void SwitchToPlayingLevel(LevelRep level)
			{
				MenuUIManager.SwitchToLevelSettingsScreen(level);
			}
#if DEBUG
			public void SimulateEditPickingLevel(string levelName)
			{
				foreach (var item in items) {
					if (item is LevelPickingLevelItem levelItem && levelItem.Level.Name == levelName)
					{
						SwitchToEditingExistingLevel(levelItem.Level);
						return;
					}
				}

				throw new ArgumentOutOfRangeException(nameof(levelName),
													  levelName,
													  "Level with this name does not exist");
			}

			public void SimulateEditPickingNewLevel()
			{
				SwitchToEditingNewLevel();
			}

			public void SimulatePlayPickingLevel(string levelName)
			{
				foreach (var item in items)
				{
					if (item is LevelPickingLevelItem levelItem && levelItem.Level.Name == levelName)
					{
						SwitchToPlayingLevel(levelItem.Level);
						return;
					}
				}

				throw new ArgumentOutOfRangeException(nameof(levelName),
													  levelName,
													  "Level with this name does not exist");
			}
#endif
		}

		//TODO: Check this if it is null, prohibit changing when the screen is visible etc.
		public GamePack Package { get; set; }

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

		public LevelPickingScreen(MenuUIManager menuUIManager)
		{
			this.menuUIManager = menuUIManager;

		}

		public override void Show()
		{
			if (screen != null) {
				return;
			}

			screen = new Screen(this);

#if DEBUG
			screen.SimulateEditPickingNewLevel();
#endif
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
