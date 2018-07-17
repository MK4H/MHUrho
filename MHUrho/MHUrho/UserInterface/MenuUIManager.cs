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

		protected readonly MainMenu mainMenu;
		protected readonly Options options;
		protected readonly PauseMenu pauseMenu;

		protected MenuScreen currentScreen;

		protected Stack<MenuScreen> previousScreens;

		protected MenuUIManager(MyGame game, IMenuController menuController)
			: base(game)
		{
			UI.Root.SetDefaultStyle(PackageManager.Instance.GetXmlFile("UI/MHUrhoStyle.xml"));

			this.MenuController = menuController;

			mainMenu = new MainMenu(game, this);
			options = new Options(game, this);
			pauseMenu = new PauseMenu(game, this);

			previousScreens = new Stack<MenuScreen>();

			currentScreen = mainMenu;

			mainMenu.Show();
			options.Hide();
			pauseMenu.Hide();
		}

		public void SwitchToMainMenu()
		{
			SwitchToScreen(mainMenu);
		}

		public void SwitchToPauseMenu()
		{
			SwitchToScreen(pauseMenu);
		}

		public void SwitchToOptions()
		{
			SwitchToScreen(options);
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
