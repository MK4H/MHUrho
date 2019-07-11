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
using MHUrho.UserInterface.MandK;
using Urho;
using Urho.Gui;
using Urho.IO;

namespace MHUrho.Input.MandK
{
	class MenuController : Controller, IMenuController {

		public InputType InputType => InputType.MouseAndKeyboard;

		public event OnScreenChangeDelegate ScreenChanged {
			add {
				UIController.ScreenChanged += value;
			}
			remove {
				UIController.ScreenChanged -= value;
			}
		}

		readonly MenuUI UIController;
		readonly MHUrhoApp app;

		IGameController pausedLevelController;

		public MenuController(MHUrhoApp app)
		{
			this.app = app;
			UIController = new MenuUI(this);
		}

		public IGameController GetGameController(CameraMover cameraMover, ILevelManager levelManager, Octree octree, Player player) {
			return new GameController(levelManager, octree, player, cameraMover);
		}

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

		public void SwitchToPauseMenu(IGameController gameController)
		{
			pausedLevelController = gameController;
			UIController.SwitchToPauseMenu(pausedLevelController.Level);
		}

		public void SwitchToEndScreen(bool victory)
		{
			UIController.SwitchToEndScreen(victory);
		}

		public ILevelLoader GetLevelLoaderForEditing(LevelRep level, IProgressEventWatcher parentProgress = null, double subsectionSize = 100)
		{
			if (pausedLevelController != null) {
				EndPausedLevel();
			}

			return level.GetLoaderForEditing(parentProgress, subsectionSize);
		}

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

		public void ExecuteActionOnCurrentScreen(MenuScreenAction action)
		{
			UIController.CurrentScreen.ExecuteAction(action);
		}

		public void ResumePausedLevel()
		{
			pausedLevelController.UnPause();
			pausedLevelController = null;
		}

		public void EndPausedLevel()
		{
			pausedLevelController.EndLevel();
			pausedLevelController = null;
		}

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

		protected override void KeyUp(KeyUpEventArgs e) {

		}

		protected override void KeyDown(KeyDownEventArgs e) {

		}

		protected override void MouseButtonDown(Urho.MouseButtonDownEventArgs e) {

		}

		protected override void MouseButtonUp(Urho.MouseButtonUpEventArgs e) { 

		}

		protected override void MouseMoved(Urho.MouseMovedEventArgs e) {

		}

		protected override void MouseWheel(Urho.MouseWheelEventArgs e) {

		}

		void ClearUI()
		{
			UIController.Clear();
		}

	}
}
