using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MHUrho.Input;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.Storage;
using Urho;
using Urho.Gui;

namespace MHUrho.UserInterface
{
    abstract class MenuUIManager : UIManager
    {
		public IMenuController MenuController { get; private set; }

		public MainMenu MainMenu { get; private set; }
		public OptionsScreen OptionsScreen { get; private set; }
		public PauseMenu PauseMenu { get; private set; }
		public LoadingScreen LoadingScreen { get; private set; }
		public SaveGameScreen SaveGameScreen { get; private set; }
		public LoadGameScreen LoadGameScreen { get; private set; }
		public PackagePickingScreen PackagePickingScreen { get; private set; }

		public PopUpConfirmation PopUpConfirmation { get; private set; }

		protected MenuScreen currentScreen;

		protected Stack<MenuScreen> previousScreens;

		protected MenuUIManager(MyGame game, IMenuController menuController)
			: base(game)
		{
			UI.Root.SetDefaultStyle(PackageManager.Instance.GetXmlFile("UI/MainMenuStyle.xml"));

			this.MenuController = menuController;

			MainMenu = new MainMenu(game, this);
			OptionsScreen = new OptionsScreen(game, this);
			PauseMenu = new PauseMenu(game, this);
			LoadingScreen = new LoadingScreen(game, this);
			SaveGameScreen = new SaveGameScreen(game, this);
			LoadGameScreen = new LoadGameScreen(game, this);
			PackagePickingScreen = new PackagePickingScreen(game, this);
			PopUpConfirmation = new PopUpConfirmation(game, this);

			previousScreens = new Stack<MenuScreen>();

			currentScreen = MainMenu;

			MainMenu.Show();
			OptionsScreen.Hide();
			PauseMenu.Hide();
			LoadingScreen.Hide();
			SaveGameScreen.Hide();
			LoadGameScreen.Hide();
		}

		public void SwitchToMainMenu()
		{
			SwitchToScreen(MainMenu);
		}

		public void SwitchToPauseMenu()
		{
			SwitchToScreen(PauseMenu);
		}

		public void SwitchToOptions()
		{
			SwitchToScreen(OptionsScreen);
		}

		public void SwitchToLoadingScreen()
		{
			SwitchToScreen(LoadingScreen);
		}

		public void SwitchToSaveGame()
		{
			SwitchToScreen(SaveGameScreen);
		}

		public void SwitchToLoadGame()
		{
			SwitchToScreen(LoadGameScreen);
		}

		public void SwitchToPackagePickingScreen()
		{
			SwitchToScreen(PackagePickingScreen);
		}

		public void SwitchBack()
		{
			currentScreen.Hide();

			if (previousScreens.Count != 0) {
				currentScreen = previousScreens.Pop();
				currentScreen.Show();
			}
		}

		public void Clear()
		{
			previousScreens = new Stack<MenuScreen>();
			currentScreen.Hide();
			currentScreen = null;
		}


		void SwitchToScreen(MenuScreen newScreen)
		{
			if (currentScreen != null) {
				currentScreen.Hide();
				previousScreens.Push(currentScreen);
			}

			currentScreen = newScreen;
			newScreen.Show();
		}
	}
}
