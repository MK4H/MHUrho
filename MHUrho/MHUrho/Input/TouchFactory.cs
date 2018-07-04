using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using Urho;

namespace MHUrho.Input
{
    public class TouchFactory : ControllerFactory
    {
		readonly MyGame game;

		public TouchFactory(MyGame game)
		{
			this.game = game;
		}

		public override ICameraController CreateCameraController(IGameController gameController, CameraMover cameraMover)
		{
			throw new NotImplementedException();
		}

		public override IGameController CreateGameController(CameraMover cameraMover, ILevelManager levelManager, Octree octree, Player player)
		{
			return new GameTouchController(game, levelManager, octree, player, cameraMover);
		}

		public override IMenuController CreateMenuController()
		{
			return new MenuTouchController(game);
		}
	}
}
