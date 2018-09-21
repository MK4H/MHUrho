using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using Urho;
using Urho.Gui;

namespace MHUrho.UserInterface
{
	class MainMenu : MenuScreen {
		class Screen : IDisposable {

			readonly MainMenu proxy;

			MenuUIManager MenuUIManager => proxy.menuUIManager;
			MyGame Game => proxy.Game;

			readonly Window window;

			public Screen(MainMenu proxy)
			{
				this.proxy = proxy;

				Game.UI.LoadLayoutToElement(MenuUIManager.MenuRoot, Game.ResourceCache, "UI/MainMenuLayout.xml");
				window = (Window)MenuUIManager.MenuRoot.GetChild("MainMenu");

				((Button)window.GetChild("StartButton", true)).Released += ButtonPressed;
				((Button)window.GetChild("LoadButton", true)).Released += ButtonPressed;
				((Button)window.GetChild("OptionsButton", true)).Released += ButtonPressed;
				((Button)window.GetChild("AboutButton", true)).Released += ButtonPressed;
				((Button)window.GetChild("ExitButton", true)).Released += ButtonPressed;
			}

			public void Dispose()
			{
				((Button)window.GetChild("StartButton", true)).Released -= ButtonPressed;
				((Button)window.GetChild("LoadButton", true)).Released -= ButtonPressed;
				((Button)window.GetChild("OptionsButton", true)).Released -= ButtonPressed;
				((Button)window.GetChild("AboutButton", true)).Released -= ButtonPressed;
				((Button)window.GetChild("ExitButton", true)).Released -= ButtonPressed;
				window.RemoveAllChildren();
				window.Remove();
				window.Dispose();
			}

			void ButtonPressed(ReleasedEventArgs obj)
			{
				//Log.Write(LogLevel.Debug, "Button pressed");
				ExecuteButtonPressAction(obj.Element.Name);
			}

			void ExecuteButtonPressAction(string buttonName)
			{
				switch (buttonName)
				{
					case "StartButton":

						//DO NOT WAIT, let the ui thread respond to user
						//LevelManager.LoadDefaultLevel(Game, new IntVector2(100, 100), "testRP2", MenuUIManager.LoadingScreen.GetLoadingWatcher());
						//MenuUIManager.SwitchToLoadingScreen();
						MenuUIManager.SwitchToPackagePickingScreen();
						break;
					case "LoadButton":
						MenuUIManager.SwitchToLoadGame();
						break;
					case "OptionsButton":
						MenuUIManager.SwitchToOptions();
						break;
					case "AboutButton":

						break;
					case "ExitButton":
						//DO NOT WAIT, THIS IS CORRECT
						Game.Exit();
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(buttonName), "Unknown button on the MainMenu screen");
				}
			}

#if DEBUG
			public void SimulateButtonPress(string buttonName)
			{
				ExecuteButtonPressAction(buttonName);
			}
#endif

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

		public MainMenu(MenuUIManager menuUIManager)
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
			screen.SimulateButtonPress("StartButton");
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
