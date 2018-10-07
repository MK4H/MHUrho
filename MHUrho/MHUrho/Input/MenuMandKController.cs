﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MHUrho.Control;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.Plugins;
using MHUrho.StartupManagement;
using MHUrho.Storage;
using MHUrho.UserInterface;
using Urho;
using Urho.Gui;
using Urho.IO;

namespace MHUrho.Input
{
	class MenuMandKController : MandKController, IMenuController {

		public InputType InputType => InputType.MouseAndKeyboard;
		
		MandKMenuUI UIController;

		IGameController pausedLevelController;

		public MenuMandKController()
		{
			UIController = new MandKMenuUI(this);
		}

		public IGameController GetGameController(CameraMover cameraMover, ILevelManager levelManager, Octree octree, Player player) {
			return new GameMandKController(levelManager, octree, player, cameraMover);
		}

		public void SwitchToPauseMenu(IGameController gameController)
		{
			pausedLevelController = gameController;
			UIController.SwitchToPauseMenu(pausedLevelController.Level);
		}

		public void StartLoadingLevelForEditing(LevelRep level)
		{
			if (pausedLevelController != null) {
				EndPausedLevel();
			}

			//This is correct, dont await, leave UI responsive
			ILevelLoader loader = level.LoadForEditing();
			
			UIController.SwitchToLoadingScreen(loader.LoadingWatcher);
		}

		public void StartLoadingLevelForPlaying(LevelRep level, PlayerSpecification players, LevelLogicCustomSettings customSettings)
		{
			if (pausedLevelController != null)
			{
				EndPausedLevel();
			}

			ILevelLoader loader = level.LoadForPlaying(players, customSettings);

			UIController.SwitchToLoadingScreen(loader.LoadingWatcher);
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
			//TODO: More checks for the fileName
			if (string.IsNullOrEmpty(fileName) || Path.GetFileName(fileName) != fileName) {
				throw new ArgumentException("Invalid fileName for the save file", nameof(fileName));
			}

			string dynamicPath = Path.Combine(MyGame.Files.SaveGameDirPath, fileName);
			try {
				Stream file = MyGame.Files.OpenDynamicFile(dynamicPath, System.IO.FileMode.Create, FileAccess.Write);
				pausedLevelController.Level.SaveTo(file);
			}
			catch (IOException e) {
				Urho.IO.Log.Write(LogLevel.Error, $"Saving a level failed with exception: {e}");
				//TODO: Inform user
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



	}
}
