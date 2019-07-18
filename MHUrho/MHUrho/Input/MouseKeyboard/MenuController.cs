using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MHUrho.CameraMovement;
using MHUrho.Control;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.Plugins;
using MHUrho.StartupManagement;
using MHUrho.Storage;
using MHUrho.UserInterface;
using MHUrho.UserInterface.MouseKeyboard;
using Urho;
using Urho.Gui;
using Urho.IO;

namespace MHUrho.Input.MouseKeyboard
{
	/// <summary>
	/// Controls the input inside the menu screens.
	/// Provides a fasade around our UI subsystem.
	/// </summary>
	class MenuController : Controller, IMenuController {

		/// <inheritdoc />
		public InputType InputType => InputType.MouseAndKeyboard;

		/// <summary>
		/// Invoked when we switch to different menu screen.
		/// </summary>
		public event OnScreenChangeDelegate ScreenChanged {
			add {
				UIController.ScreenChanged += value;
			}
			remove {
				UIController.ScreenChanged -= value;
			}
		}

		/// <summary>
		/// The UI controller.
		/// </summary>
		readonly MenuUI UIController;

		/// <summary>
		/// The instance representing the current App.
		/// </summary>
		readonly MHUrhoApp app;

		/// <summary>
		/// Controller controlling the currently paused level.
		/// </summary>
		IGameController pausedLevelController;

		/// <summary>
		/// Provides facade over the UI control
		/// </summary>
		/// <param name="app"></param>
		public MenuController(MHUrhoApp app)
		{
			this.app = app;
			UIController = new MenuUI(this);
		}

		/// <summary>
		/// Displays the initial main menu screen with possible warnings about the packages we were unable to load.
		/// </summary>
		/// <param name="loadingErrorTitle">The title of the error display.</param>
		/// <param name="loadingErrorDescription">The description of the loading error.</param>
		public void InitialSwitchToMainMenu(string loadingErrorTitle = null, string loadingErrorDescription = null)
		{
			UIController.Clear();
			UIController.SwitchToMainMenu();
			if (loadingErrorTitle != null && loadingErrorDescription != null) {
				UIController.ErrorPopUp
							.DisplayError(loadingErrorTitle,
										loadingErrorDescription,
										UIController.MainMenu);
			}	
		}

		/// <summary>
		/// Dsiplays the pause menu for the level controlled by <paramref name="gameController"/>
		/// </summary>
		/// <param name="gameController">Game controller controlling the paused level.</param>
		public void SwitchToPauseMenu(IGameController gameController)
		{
			pausedLevelController = gameController;
			UIController.SwitchToPauseMenu(pausedLevelController.Level);
		}

		/// <summary>
		/// Displays the end screen with the victory or defeat based on <paramref name="victory"/>.
		/// </summary>
		/// <param name="victory">If user was victorious or defeated.</param>
		public void SwitchToEndScreen(bool victory)
		{
			UIController.SwitchToEndScreen(victory);
		}

		/// <summary>
		/// If there is a paused running level, ends it and then creates a loader for loading <paramref name="level"/>
		/// for editing.
		/// Can send loading updates if given <paramref name="parentProgress"/>.
		/// Scales the percentage updates by <paramref name="subsectionSize"/>, to enable us and the parent to go from 0 to 100.
		/// </summary>
		/// <param name="level">The level to create the loader for.</param>
		/// <param name="parentProgress">The progress watcher for the parent.</param>
		/// <param name="subsectionSize">The precentage size of the level loading in the whole loading process.</param>
		/// <returns>A loader for the <paramref name="level"/> that can be used to load the <paramref name="level"/> for editing.</returns>
		public ILevelLoader GetLevelLoaderForEditing(LevelRep level, IProgressEventWatcher parentProgress = null, double subsectionSize = 100)
		{
			if (pausedLevelController != null) {
				EndPausedLevel();
			}

			return level.GetLoaderForEditing(parentProgress, subsectionSize);
		}


		/// <summary>
		/// If there is a paused running level, ends it and then creates a loader for loading <paramref name="level"/>
		/// for playing.
		/// Initializes players based on <paramref name="players"/>.
		/// Initializes level logic based on <paramref name="customSettings"/>.
		/// Can send loading updates if given <paramref name="parentProgress"/>.
		/// Scales the percentage updates by <paramref name="subsectionSize"/>, to enable us and the parent to go from 0 to 100.
		/// </summary>
		/// <param name="level">The level to create the loader for.</param>
		/// <param name="players">Data to use for player initialization.</param>
		/// <param name="customSettings">Settings to use for level logic plugin initialization.</param>
		/// <param name="parentProgress">The progress watcher for the parent.</param>
		/// <param name="subsectionSize">The precentage size of the level loading in the whole loading process.</param>
		/// <returns>A loader for the <paramref name="level"/> that can be used to load the <paramref name="level"/> for editing.</returns>
		public ILevelLoader GetLevelLoaderForPlaying(LevelRep level, 
														PlayerSpecification players, 
														LevelLogicCustomSettings customSettings, 
														IProgressEventWatcher parentProgress = null, 
														double subsectionSize = 100)
		{
			if (pausedLevelController != null)
			{
				EndPausedLevel();
			}

			return level.GetLoaderForPlaying(players, customSettings, parentProgress, subsectionSize);
		}

		/// <summary>
		/// Executes the given <paramref name="action"/> on the current screen.
		/// </summary>
		/// <param name="action">The action to execute.</param>
		public void ExecuteActionOnCurrentScreen(MenuScreenAction action)
		{
			UIController.CurrentScreen.ExecuteAction(action);
		}

		/// <summary>
		/// Resumes the currently paused level.
		/// </summary>
		public void ResumePausedLevel()
		{
			pausedLevelController.UnPause();
			pausedLevelController = null;
		}

		/// <summary>
		/// Ends the currently paused level.
		/// </summary>
		public void EndPausedLevel()
		{
			pausedLevelController.EndLevel();
			pausedLevelController = null;
		}

		/// <summary>
		/// Saves the currently paused level to file with the name <paramref name="fileName"/>.
		/// </summary>
		/// <param name="fileName">The name of the save file.</param>
		public void SavePausedLevel(string fileName)
		{
			//NOTE:Maybe add more checks for the fileName
			if (string.IsNullOrEmpty(fileName) || Path.GetFileName(fileName) != fileName) {
				throw new ArgumentException("Invalid fileName for the save file", nameof(fileName));
			}

			string dynamicPath = Path.Combine(app.Files.SaveGameDirPath, fileName);
			try {
				Stream file = app.Files.OpenDynamicFile(dynamicPath, System.IO.FileMode.Create, FileAccess.Write);
				pausedLevelController.Level.SaveTo(file);
			}
			catch (IOException e) {
				Urho.IO.Log.Write(LogLevel.Error, $"Saving a level failed with exception: {e}");
				throw;
			}
		}

		///<inheritdoc />
		protected override void KeyUp(KeyUpEventArgs e) {

		}
		///<inheritdoc />
		protected override void KeyDown(KeyDownEventArgs e) {

		}
		///<inheritdoc />
		protected override void MouseButtonDown(Urho.MouseButtonDownEventArgs e) {

		}
		///<inheritdoc />
		protected override void MouseButtonUp(Urho.MouseButtonUpEventArgs e) { 

		}
		///<inheritdoc />
		protected override void MouseMoved(Urho.MouseMovedEventArgs e) {

		}
		///<inheritdoc />
		protected override void MouseWheel(Urho.MouseWheelEventArgs e) {

		}

		/// <summary>
		/// Clears the UI, hides all menu screens.
		/// </summary>
		void ClearUI()
		{
			UIController.Clear();
		}

	}
}
