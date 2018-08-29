using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MHUrho.Control;
using MHUrho.Logic;
using MHUrho.Packaging;
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

		public MenuMandKController(MyGame game) : base(game)
		{
			UIController = new MandKMenuUI(game, this);
		}

		public IGameController GetGameController(CameraMover cameraMover, ILevelManager levelManager, Octree octree, Player player) {
			return new GameMandKController(Game, levelManager, octree, player, cameraMover);
		}

		public void SwitchToPauseMenu(IGameController gameController)
		{
			pausedLevelController = gameController;
			UIController.SwitchToPauseMenu();
		}

		public void StartLoadingLevel(LevelRep level, bool editorMode)
		{
			if (pausedLevelController != null) {
				EndPausedLevel();
			}

			//This is correct, dont await, leave UI responsive
			ILevelLoader loader = level.StartLoading(editorMode);
			
			UIController.SwitchToLoadingScreen(loader.LoadingWatcher);
		}

		public void StartLoadingLevel(string savePath, bool editorMode)
		{
			if (pausedLevelController != null) {
				EndPausedLevel();
			}

			//This is correct, dont await, leave UI responsive
			ILevelLoader loader = LevelManager.GetLoader(Game);
			
			//TODO: Try catch if file failed to open
			loader.LoadFrom(MyGame.Files.OpenDynamicFile(savePath, System.IO.FileMode.Open, FileAccess.Read), editorMode);
						
			UIController.SwitchToLoadingScreen(loader.LoadingWatcher);
		}

		public void StartLoadingDefaultLevel(IntVector2 mapSize)
		{
			if (pausedLevelController != null) {
				EndPausedLevel();
			}

			var loader = LevelManager.GetLoader(Game);

			loader.LoadDefaultLevel(mapSize);
			UIController.SwitchToLoadingScreen(loader.LoadingWatcher);
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

		public void SavePausedLevel(string toPath)
		{
			pausedLevelController.Level.SaveTo(MyGame.Files.OpenDynamicFile(toPath, System.IO.FileMode.Create, FileAccess.Write));
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
