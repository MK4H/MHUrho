using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MHUrho.Control;
using MHUrho.Logic;
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

		public void LoadLevel(string fromPath)
		{
			if (pausedLevelController != null) {
				EndPausedLevel();
			}

			using (Stream stream = MyGame.Files.OpenDynamicFile(fromPath, System.IO.FileMode.Open, FileAccess.Read)) {
				//This is correct, dont await, leave UI responsive
				LevelManager.Load(Game, StLevel.Parser.ParseFrom(stream), UIController.LoadingScreen.GetLoadingWatcher());
			}
			
			UIController.SwitchToLoadingScreen();
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
			using (var output = new Google.Protobuf.CodedOutputStream(MyGame.Files.OpenDynamicFile(toPath, System.IO.FileMode.Create, FileAccess.Write))) {
				pausedLevelController.Level.Save().WriteTo(output);
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
