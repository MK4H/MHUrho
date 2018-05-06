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
		MandKMenuUI UI;

		public MenuMandKController(MyGame game) : base(game)
		{
			UI = new MandKMenuUI(game);
		}

		public IGameController GetGameController(CameraController cameraController, ILevelManager levelManager, Player player) {
			return new GameMandKController(Game, levelManager, player, cameraController);
		}

		protected override void KeyUp(KeyUpEventArgs e) {

		}

		protected override void KeyDown(KeyDownEventArgs e) {

		}

		protected override void MouseButtonDown(MouseButtonDownEventArgs e) {

		}

		protected override void MouseButtonUp(MouseButtonUpEventArgs e) { 

		}

		protected override void MouseMoved(MouseMovedEventArgs e) {

		}

		protected override void MouseWheel(MouseWheelEventArgs e) {

		}



	}
}
