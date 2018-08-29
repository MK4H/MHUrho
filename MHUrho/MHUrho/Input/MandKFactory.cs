using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.EditorTools;
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
			var typedController = (GameMandKController) gameController;
			return new CameraControllerMandK(typedController, typedController.UIManager , cameraMover);
		}

		public override IGameController CreateGameController(CameraMover cameraMover, ILevelManager levelManager, Octree octree, IPlayer player)
		{
			return new GameMandKController(game, levelManager, octree, player, cameraMover);
		}

		public override IMenuController CreateMenuController()
		{
			return new MenuMandKController(game);
		}

		public override ToolManager CreateToolManager(IGameController gameController, CameraMover cameraMover)
		{
			var typedController = gameController as GameMandKController;
			if (typedController == null) {
				throw new ArgumentException("Wrong type of game controller", nameof(gameController));
			}

			return new ToolManagerMandK(typedController, typedController.UIManager, cameraMover);
		}
	}
}
