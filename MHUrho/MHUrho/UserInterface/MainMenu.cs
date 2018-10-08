using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.StartupManagement;
using Urho;
using Urho.Gui;

namespace MHUrho.UserInterface
{
	class MainMenu : MenuScreen {
		class Screen : ScreenBase {

			public const string StartButtonName = "StartButton";
			public const string LoadButtonName = "LoadButton";
			public const string OptionsButtonName = "OptionsButton";
			public const string AboutButtonName = "AboutButton";
			public const string ExitButtonName = "ExitButton";

			readonly MainMenu proxy;

			readonly Window window;

			public Screen(MainMenu proxy)
				:base(proxy)
			{
				this.proxy = proxy;

				Game.UI.LoadLayoutToElement(MenuUIManager.MenuRoot, Game.ResourceCache, "UI/MainMenuLayout.xml");
				window = (Window)MenuUIManager.MenuRoot.GetChild("MainMenu");

				((Button)window.GetChild(StartButtonName, true)).Released += ButtonPressed;
				((Button)window.GetChild(LoadButtonName, true)).Released += ButtonPressed;
				((Button)window.GetChild(OptionsButtonName, true)).Released += ButtonPressed;
				((Button)window.GetChild(AboutButtonName, true)).Released += ButtonPressed;
				((Button)window.GetChild(ExitButtonName, true)).Released += ButtonPressed;
			}

			public override void Dispose()
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
					case StartButtonName:

						//DO NOT WAIT, let the ui thread respond to user
						//LevelManager.LoadDefaultLevel(Game, new IntVector2(100, 100), "testRP2", MenuUIManager.LoadingScreen.GetLoadingWatcher());
						//MenuUIManager.SwitchToLoadingScreen();
						MenuUIManager.SwitchToPackagePickingScreen();
						break;
					case LoadButtonName:
						MenuUIManager.SwitchToLoadGame();
						break;
					case OptionsButtonName:
						MenuUIManager.SwitchToOptions();
						break;
					case AboutButtonName:

						break;
					case ExitButtonName:
						//DO NOT WAIT, THIS IS CORRECT
						Game.Exit();
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(buttonName), "Unknown button on the MainMenu screen");
				}
			}


			public void SimulateButtonPress(string buttonName)
			{
				ExecuteButtonPressAction(buttonName);
			}

		}

		protected override ScreenBase ScreenInstance {
			get => screen;
			set => screen = (Screen)value;
		}


		Screen screen;

		public MainMenu(MenuUIManager menuUIManager)
			:base(menuUIManager)
		{}

		public override void ExecuteAction(MenuScreenAction action)
		{
			if (action is MainMenuAction myAction)
			{
				switch (myAction.Action)
				{
					case MainMenuAction.Actions.Start:
						screen.SimulateButtonPress(Screen.StartButtonName);
						break;
					case MainMenuAction.Actions.Load:
						screen.SimulateButtonPress(Screen.LoadButtonName);
						break;
					case MainMenuAction.Actions.Options:
						screen.SimulateButtonPress(Screen.OptionsButtonName);
						break;
					case MainMenuAction.Actions.About:
						screen.SimulateButtonPress(Screen.AboutButtonName);
						break;
					case MainMenuAction.Actions.Exit:
						screen.SimulateButtonPress(Screen.ExitButtonName);
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
