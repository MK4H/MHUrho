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
using Urho.Resources;

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
		public LevelPickingScreen LevelPickingScreen { get; private set; }
		public LevelSettingsScreen LevelSettingsScreen { get; private set; }
		public LevelCreationScreen LevelCreationScreen { get; private set; }
		public SaveAsScreen SaveAsScreen { get; private set; }


		public FileSystemBrowsingPopUp FileBrowsingPopUp { get; private set; }
		public ConfirmationPopUp ConfirmationPopUp { get; private set; }
		public ErrorPopUp ErrorPopUp { get; private set; }


		public UIElement MenuRoot { get; private set; }
	

		public MenuScreen CurrentScreen { get; protected set; }

		protected Stack<MenuScreen> PreviousScreens;

		protected MenuUIManager( IMenuController menuController)
		{
			MenuRoot = UI.Root;
			MenuRoot.SetDefaultStyle(PackageManager.Instance.GetXmlFile("UI/MainMenuStyle.xml", true));

			this.MenuController = menuController;

			MainMenu = new MainMenu(this);
			OptionsScreen = new OptionsScreen(this);
			PauseMenu = new PauseMenu(this);
			LoadingScreen = new LoadingScreen(this);
			SaveGameScreen = new SaveGameScreen(this);
			LoadGameScreen = new LoadGameScreen(this);
			PackagePickingScreen = new PackagePickingScreen(this);
			LevelPickingScreen = new LevelPickingScreen(this);
			LevelSettingsScreen = new LevelSettingsScreen(this);
			LevelCreationScreen = new LevelCreationScreen(this);
			SaveAsScreen = new SaveAsScreen(this);
			FileBrowsingPopUp = new FileSystemBrowsingPopUp(this);
			ConfirmationPopUp = new ConfirmationPopUp(this);
			ErrorPopUp = new ErrorPopUp(this);

			PreviousScreens = new Stack<MenuScreen>();
		}

		public MainMenu SwitchToMainMenu()
		{
			SwitchToScreen(MainMenu);
			return MainMenu;
		}

		public PauseMenu SwitchToPauseMenu(ILevelManager level)
		{
			PauseMenu.PausedLevel = level;
			SwitchToScreen(PauseMenu);
			return PauseMenu;
		}

		public OptionsScreen SwitchToOptions()
		{
			SwitchToScreen(OptionsScreen);
			return OptionsScreen;
		}

		public LoadingScreen SwitchToLoadingScreen(Action onLoadingFinished = null)
		{
			LoadingScreen.OnLoadingFinished += onLoadingFinished;
			SwitchToScreen(LoadingScreen);
			return LoadingScreen;
		}

		public SaveGameScreen SwitchToSaveGame()
		{
			SwitchToScreen(SaveGameScreen);
			return SaveGameScreen;
		}

		public LoadGameScreen SwitchToLoadGame()
		{
			SwitchToScreen(LoadGameScreen);
			return LoadGameScreen;
		}

		public PackagePickingScreen SwitchToPackagePickingScreen()
		{
			SwitchToScreen(PackagePickingScreen);
			return PackagePickingScreen;
		}

		public LevelPickingScreen SwitchToLevelPickingScreen(GamePack package)
		{
			LevelPickingScreen.Package = package;
			SwitchToScreen(LevelPickingScreen);
			return LevelPickingScreen;
		}

		public LevelSettingsScreen SwitchToLevelSettingsScreen(LevelRep level)
		{
			LevelSettingsScreen.Level = level;
			SwitchToScreen(LevelSettingsScreen);
			return LevelSettingsScreen;
		}

		public LevelCreationScreen SwitchToLevelCreationScreen(LevelRep level)
		{
			LevelCreationScreen.Level = level;
			SwitchToScreen(LevelCreationScreen);
			return LevelCreationScreen;
		}

		public SaveAsScreen SwitchToSaveAsScreen(ILevelManager level)
		{
			SaveAsScreen.Level = level;
			SwitchToScreen(SaveAsScreen);
			return SaveAsScreen;
		}

		public void SwitchBack()
		{
			CurrentScreen.Hide();

			if (PreviousScreens.Count != 0) {
				CurrentScreen = PreviousScreens.Pop();
				CurrentScreen.Show();
			}
		}

		public void Clear()
		{
			PreviousScreens = new Stack<MenuScreen>();
			CurrentScreen?.Hide();
			CurrentScreen = null;
		}

		void SwitchToScreen(MenuScreen newScreen)
		{
			try {
				if (CurrentScreen != null) {
					CurrentScreen.Hide();

					if (CurrentScreen != LoadingScreen) {
						PreviousScreens.Push(CurrentScreen);
					}
				}

				CurrentScreen = newScreen;
				newScreen.Show();
			}
			catch (Exception e) {
				string message =
					$"There was an error while switching menu screens, probably some missing or corrupted files: {e.Message}";

				Urho.IO.Log.Write(LogLevel.Error, message);
				Game.ErrorExit(message);
			}
		}

	}
}
