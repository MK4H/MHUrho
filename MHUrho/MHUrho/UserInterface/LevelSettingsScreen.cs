using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Packaging;
using MHUrho.StartupManagement;
using Urho.Gui;

namespace MHUrho.UserInterface
{
	class LevelSettingsScreen : MenuScreen
	{
		class Screen :  ScreenBase {

			readonly LevelSettingsScreen proxy;

			LevelRep Level => proxy.Level;

			readonly Window window;
			readonly Window customSettingsWindow;
			readonly ScrollView descriptionView;
			readonly BorderImage mapImage;
			readonly ListView playerList;



			public Screen(LevelSettingsScreen proxy)
				:base(proxy)
			{
				this.proxy = proxy;
				Game.UI.LoadLayoutToElement(MenuUIManager.MenuRoot, Game.ResourceCache, "UI/LevelSettingsLayout.xml");

				window = (Window)MenuUIManager.MenuRoot.GetChild("LevelSettingsWindow");

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

			public override void Dispose()
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

		//TODO: Ensure that Show cannot be called with Level null, that level is not changed after show etc.
		public LevelRep Level { get; set; }

		protected override ScreenBase ScreenInstance {
			get => screen;
			set => screen = (Screen)value;
		}

		Screen screen;

		public LevelSettingsScreen(MenuUIManager menuUIManager)
			:base(menuUIManager)
		{

		}

		public override void ExecuteAction(MenuScreenAction action)
		{
			throw new NotImplementedException();
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

			Level = null;
			base.Hide();
		}

		
	}
}
