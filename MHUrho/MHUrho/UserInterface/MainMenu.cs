using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using Urho;
using Urho.Gui;

namespace MHUrho.UserInterface
{
    class MainMenu : MenuScreen
    {
		public override bool Visible {
			get => window.Visible;
			set => window.Visible = value;
		}

		readonly Window window;


		public MainMenu(MyGame game, MenuUIManager menuUIManager)
			: base(game, menuUIManager)
		{
			UI.LoadLayoutToElement(UI.Root, game.ResourceCache, "UI/FirstTryMenu.xml");
			window = (Window)UI.Root.GetChild("MainMenu");

			((Button)window.GetChild("StartButton", true)).Released += ButtonPressed;
			((Button) window.GetChild("LoadButton", true)).Released += ButtonPressed;
			((Button)window.GetChild("OptionsButton", true)).Released += ButtonPressed;
			((Button)window.GetChild("AboutButton", true)).Released += ButtonPressed;
			((Button)window.GetChild("ExitButton", true)).Released += ButtonPressed;
		}

		void ButtonPressed(ReleasedEventArgs obj)
		{
			//Log.Write(LogLevel.Debug, "Button pressed");

			switch (obj.Element.Name) {
				case "StartButton":
					window.Visible = false;
					//DO NOT WAIT, let the ui thread respond to user
					LevelManager.LoadDefaultLevel(Game, new IntVector2(100, 100), "testRP2", MenuUIManager.LoadingScreen.GetLoadingWatcher());
					MenuUIManager.SwitchToLoadingScreen();
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
					throw new ArgumentOutOfRangeException(nameof(obj.Element), "Unknown button on the MainMenu screen");
			}
		}

		

		public override void Show()
		{
			Visible = true;
		}

		public override void Hide()
		{
			Visible = false;
		}
	}
}
