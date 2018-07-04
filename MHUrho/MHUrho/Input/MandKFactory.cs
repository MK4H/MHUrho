using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using Urho;

namespace MHUrho.Input
{
    public class MandKFactory : ControllerFactory
    {
		readonly MyGame game;

		public MandKFactory(MyGame game)
		{
			this.game = game;
		}

		public override ICameraController CreateCameraController(IGameController gameController, CameraMover cameraMover)
		{
			return new CameraControllerMandK((GameMandKController) gameController, cameraMover);
		}

		public override IGameController CreateGameController(CameraMover cameraMover, ILevelManager levelManager, Octree octree, Player player)
		{
			return new GameMandKController(game, levelManager, octree, player, cameraMover);
		}

		public override IMenuController CreateMenuController()
		{
			return new MenuMandKController(game);
		}
	}
}
