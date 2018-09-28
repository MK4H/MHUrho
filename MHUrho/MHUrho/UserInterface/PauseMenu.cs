using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.StartupManagement;
using Urho.Gui;

namespace MHUrho.UserInterface
{
	class PauseMenu : MenuScreen {
		abstract class Screen : ScreenBase {

			protected readonly PauseMenu Proxy;

			protected ILevelManager Level => Proxy.PausedLevel;


			public Screen(PauseMenu proxy)
				:base(proxy)
			{
				this.Proxy = proxy;
			}



			protected void Resume()
			{
				Proxy.PausedLevel = null;
				MenuUIManager.Clear();
				MenuUIManager.MenuController.ResumePausedLevel();
			}

			protected void GoToOptions()
			{
				MenuUIManager.SwitchToOptions();
			}

			protected void Exit()
			{
				MenuUIManager.MenuController.EndPausedLevel();
				Proxy.PausedLevel = null;
				MenuUIManager.Clear();
				MenuUIManager.SwitchToMainMenu();
			}

		}

		class EditorScreen : Screen
		{
			const string WindowName = "EditorPauseMenu";
			const string ResumeButton = "Resume";
			const string SaveButton = "Save";
			const string SaveAsButton = "SaveAs";
			const string OptionsButton = "Options";
			const string ExitButton = "Exit";


			readonly Window window;

			public EditorScreen(PauseMenu proxy)
				:base(proxy)
			{
				Game.UI.LoadLayoutToElement(MenuUIManager.MenuRoot, Game.ResourceCache, "UI/EditorPauseMenuLayout.xml");

				window = (Window)MenuUIManager.MenuRoot.GetChild(WindowName);

				((Button)window.GetChild(ResumeButton)).Released += ButtonReleased;

				((Button)window.GetChild(SaveButton)).Released += ButtonReleased;

				((Button)window.GetChild(SaveAsButton)).Released += ButtonReleased;

				((Button)window.GetChild(OptionsButton)).Released += ButtonReleased;

				((Button)window.GetChild(ExitButton)).Released += ButtonReleased;
			}

			void ButtonReleased(ReleasedEventArgs args)
			{
				switch (args.Element.Name)
				{
					case ResumeButton:
						Resume();
						break;
					case SaveButton:
						SaveLevelPrototype();
						break;
					case SaveAsButton:
						SaveLevelPrototypeAs();
						break;
					case OptionsButton:
						GoToOptions();
						break;
					case ExitButton:
						Exit();
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(args.Element.Name), args.Element.Name, "Unknown button released");
				}
			}

			void SaveLevelPrototype()
			{
				Level.LevelRep.SaveToGamePack();
			}

			void SaveLevelPrototypeAs()
			{
				MenuUIManager.SwitchToSaveAsScreen(Level);
			}

			public override void Dispose()
			{
				((Button)window.GetChild(ResumeButton)).Released -= ButtonReleased;

				((Button)window.GetChild(SaveButton)).Released -= ButtonReleased;

				((Button)window.GetChild(SaveAsButton)).Released -= ButtonReleased;

				((Button)window.GetChild(OptionsButton)).Released -= ButtonReleased;

				((Button)window.GetChild(ExitButton)).Released -= ButtonReleased;

				window.RemoveAllChildren();
				window.Remove();
				window.Dispose();
			}
		}

		class PlayScreen : Screen {
			const string WindowName = "PauseMenu";
			const string ResumeButton = "Resume";
			const string SaveButton = "Save";
			const string LoadButton = "Load";
			const string OptionsButton = "Options";
			const string ExitButton = "Exit";


			readonly Window window;

			public PlayScreen(PauseMenu proxy)
				: base(proxy)
			{
				Game.UI.LoadLayoutToElement(MenuUIManager.MenuRoot, Game.ResourceCache, "UI/PauseMenuLayout.xml");

				window = (Window)MenuUIManager.MenuRoot.GetChild(WindowName);

				((Button)window.GetChild(ResumeButton)).Released += ButtonReleased;

				((Button)window.GetChild(SaveButton)).Released += ButtonReleased;

				((Button)window.GetChild(LoadButton)).Released += ButtonReleased;

				((Button)window.GetChild(OptionsButton)).Released += ButtonReleased;

				((Button)window.GetChild(ExitButton)).Released += ButtonReleased;
			}

			void ButtonReleased(ReleasedEventArgs args)
			{
				switch (args.Element.Name)
				{
					case ResumeButton:
						Resume();
						break;
					case SaveButton:
						SaveGame();
						break;
					case LoadButton:
						LoadGame();
						break;
					case OptionsButton:
						GoToOptions();
						break;
					case ExitButton:
						Exit();
						break;
					default:
						throw new ArgumentOutOfRangeException(nameof(args.Element.Name), args.Element.Name, "Unknown button released");
				}
			}

			void SaveGame()
			{
				MenuUIManager.SwitchToSaveGame();
			}

			void LoadGame()
			{
				MenuUIManager.SwitchToLoadGame();
			}

			public override void Dispose()
			{
				((Button)window.GetChild(ResumeButton)).Released -= ButtonReleased;

				((Button)window.GetChild(SaveButton)).Released -= ButtonReleased;

				((Button)window.GetChild(LoadButton)).Released -= ButtonReleased;

				((Button)window.GetChild(OptionsButton)).Released -= ButtonReleased;

				((Button)window.GetChild(ExitButton)).Released -= ButtonReleased;

				window.RemoveAllChildren();
				window.Remove();
				window.Dispose();
			}
		}

		public ILevelManager PausedLevel { get; set; }

		protected override ScreenBase ScreenInstance {
			get => screen;
			set => screen = (Screen)value;
		}

		Screen screen;


		public PauseMenu(MenuUIManager menuUIManager)
			:base(menuUIManager)
		{
		}

		public override void ExecuteAction(MenuScreenAction action)
		{
			throw new NotImplementedException();
		}

		public override void Show()
		{
			if (PausedLevel == null) {
				throw new InvalidOperationException("Cannot show pause menu with PausedLevel null, you have to set PauseLevel before showing Pause Menu");
			}

			if (screen != null) {
				return;
			}

			if (PausedLevel.EditorMode) {
				screen = new EditorScreen(this);
			}
			else {
				screen = new PlayScreen(this);
			}
		}
	}
}
