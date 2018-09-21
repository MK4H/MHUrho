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
		public MandKFactory()
		{

		}

		public override ICameraController CreateCameraController(IGameController gameController, CameraMover cameraMover)
		{
			var typedController = gameController as GameMandKController;
			if (typedController == null)
			{
				throw new ArgumentException("Wrong type of game controller", nameof(gameController));
			}
			return new CameraControllerMandK(typedController, typedController.UIManager , cameraMover);
		}

		public override IGameController CreateGameController(CameraMover cameraMover, ILevelManager levelManager, Octree octree, IPlayer player)
		{
			return new GameMandKController(levelManager, octree, player, cameraMover);
		}

		public override IMenuController CreateMenuController()
		{
			return new MenuMandKController();
		}
	}
}
