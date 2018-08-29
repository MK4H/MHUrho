using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Packaging;
using Urho.Gui;

namespace MHUrho.UserInterface
{
    class LevelSettingsScreen : MenuScreen
    {
		class Screen : IDisposable {

			LevelSettingsScreen proxy;

			MyGame Game => proxy.game;
			MenuUIManager MenuUIManager => proxy.menuUIManager;

			LevelRep Level => proxy.Level;

			Window window;
			Window customSettingsWindow;
			ScrollView descriptionView;
			BorderImage mapImage;
			ListView playerList;



			public Screen(LevelSettingsScreen proxy)
			{

				Game.UI.LoadLayoutToElement(Game.UI.Root, Game.ResourceCache, "UI/LevelSettingsLayout.xml");

				window = (Window)Game.UI.Root.GetChild("LevelSettingsWindow");

				customSettingsWindow = (Window)window.GetChild("CustomSettings");

				descriptionView = (ScrollView)window.GetChild("DescriptionScrollView");

				mapImage = (BorderImage)window.GetChild("MapImage");

				playerList = (ListView)window.GetChild("PlayerListView");

				((Button)window.GetChild("PlayButton", true)).Released += PlayButtonReleased;
				((Button)window.GetChild("BackButton", true)).Released += BackButtonReleased;
			}

			void PlayButtonReleased(ReleasedEventArgs args)
			{
				
			}

			void BackButtonReleased(ReleasedEventArgs args)
			{

			}

			public void Dispose()
			{
				((Button)window.GetChild("PlayButton", true)).Released -= PlayButtonReleased;
				((Button)window.GetChild("BackButton", true)).Released -= BackButtonReleased;

				window.RemoveAllChildren();
				window.Remove();

				window.Dispose();
				customSettingsWindow.Dispose();
				descriptionView.Dispose();
				mapImage.Dispose();
				playerList.Dispose();				
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

		//TODO: Ensure that Show cannot be called with Level null, that level is not changed after show etc.
		public LevelRep Level { get; set; }

		readonly MyGame game;
		readonly MenuUIManager menuUIManager;

		Screen screen;

		public LevelSettingsScreen(MyGame game, MenuUIManager menuUIManager)
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
