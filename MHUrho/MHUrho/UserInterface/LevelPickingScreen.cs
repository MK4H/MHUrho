using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.Packaging;
using Urho.Gui;

namespace MHUrho.UserInterface
{
    class LevelPickingScreen : MenuScreen
    {

		class Screen : IDisposable {

			LevelPickingScreen proxy;

			MyGame Game => proxy.game;
			MenuUIManager MenuUIManager => proxy.menuUIManager;

			Window window;
			ListView listView;
			Button editButton;
			Button playButton;

			List<LevelPickingItem> items;

			public Screen(LevelPickingScreen proxy)
			{
				this.proxy = proxy;

				GetLevels();

				Game.UI.LoadLayoutToElement(Game.UI.Root, Game.ResourceCache, "UI/LevelPickingLayout.xml");

				window = (Window)Game.UI.Root.GetChild("LevelPickingWindow");
				window.Visible = false;

				listView = (ListView)window.GetChild("ListView");

				editButton = (Button)window.GetChild("EditButton", true);
				playButton = (Button)window.GetChild("PlayButton", true);

				editButton.Released += EditButtonReleased;
				playButton.Released += PlayButtonReleased;
				((Button)window.GetChild("BackButton", true)).Released += BackButtonReleased;
			}

			public void Dispose()
			{
				editButton.Released -= EditButtonReleased;
				playButton.Released -= PlayButtonReleased;
				((Button)window.GetChild("BackButton", true)).Released -= BackButtonReleased;

				window.RemoveAllChildren();
				window.Remove();

				window.Dispose();
				listView.Dispose();
				editButton.Dispose();
				playButton.Dispose();
			}

			void GetLevels()
			{
				foreach (var level in proxy.Package.Levels) {
					items.Add(new LevelPickingLevelItem(level, Game));
				}
			}

			void EditButtonReleased(ReleasedEventArgs obj)
			{
				foreach (var item in items) {
					if (item.IsSelected) {
						if (item is LevelPickingLevelItem levelItem) {
							MenuUIManager.SwitchToLevelCreationScreen(levelItem.Level);
							return;
						}
						else if (item is LevelPickingNewLevelItem newItem) {
							MenuUIManager.SwitchToLevelCreationScreen(null);
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
							MenuUIManager.SwitchToLevelSettingsScreen(levelItem.Level);
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

		readonly MyGame game;
		readonly MenuUIManager menuUIManager;

		Screen screen;

		public LevelPickingScreen(MyGame game, MenuUIManager menuUIManager)
		{
			this.game = game;
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
