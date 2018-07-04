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

		MandKMenuUI UI;

		public MenuMandKController(MyGame game) : base(game)
		{
			UI = new MandKMenuUI(game);
		}

		public IGameController GetGameController(CameraMover cameraMover, ILevelManager levelManager, Octree octree, Player player) {
			return new GameMandKController(Game, levelManager, octree, player, cameraMover);
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
