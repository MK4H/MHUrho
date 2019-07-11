using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.CameraMovement;
using MHUrho.EditorTools;
using MHUrho.Logic;
using Urho;

namespace MHUrho.Input.MandK
{
    public class MandKFactory : IControllerFactory
    {
		public MandKFactory()
		{

		}

		public ICameraController CreateCameraController(IGameController gameController, CameraMover cameraMover)
		{
			if (!(gameController is GameController typedController))
			{
				throw new ArgumentException("Wrong type of game controller", nameof(gameController));
			}
			return new CameraController(typedController, typedController.UIManager , cameraMover);
		}

		public IGameController CreateGameController(CameraMover cameraMover, ILevelManager levelManager, Octree octree, IPlayer player)
		{
			return new GameController(levelManager, octree, player, cameraMover);
		}

		public IMenuController CreateMenuController(MHUrhoApp app)
		{
			return new MenuController(app);
		}
	}
}
