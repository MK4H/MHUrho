﻿using System;
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

			public override void EnableInput()
			{
				window.SetDeepEnabled(true);
			}

			public override void DisableInput()
			{
				window.SetDeepEnabled(false);
			}

			public override void ResetInput()
			{
				window.ResetDeepEnabled();
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

			void ButtonReleased(ReleasedEventArgs args)
			{
				switch (args.Element.Name)
				{
					case ResumeButton:
						Resume();
						break;
					case SaveButton:
						SaveLevelPrototype(true);
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

			void SaveLevelPrototype(bool overrideLevel)
			{
				try {
					Level.LevelRep.SaveToGamePack(overrideLevel);
				}
				catch (Exception e) {
					Urho.IO.Log.Write(Urho.LogLevel.Error, $"Saving level failed with exception: {e.Message}");
					MenuUIManager.ErrorPopUp.DisplayError("Saving failed",
														"Saving the current level failed with an error, see Log for details.",
														Proxy);
				}
			}

			void SaveLevelPrototypeAs()
			{
				MenuUIManager.SwitchToSaveAsScreen(Level);
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

			public override void EnableInput()
			{
				window.SetDeepEnabled(true);
			}

			public override void DisableInput()
			{
				window.SetDeepEnabled(false);
			}

			public override void ResetInput()
			{
				window.ResetDeepEnabled();
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
