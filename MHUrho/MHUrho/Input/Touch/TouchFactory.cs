using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.CameraMovement;
using MHUrho.EditorTools;
using MHUrho.Logic;
using Urho;

namespace MHUrho.Input.Touch
{
    public class TouchFactory : IControllerFactory
    {
		public TouchFactory()
		{

		}

		public ICameraController CreateCameraController(IGameController gameController, CameraMover cameraMover)
		{
			throw new NotImplementedException();
		}

		public IGameController CreateGameController(CameraMover cameraMover, ILevelManager levelManager, Octree octree, IPlayer player)
		{
			return new GameController(levelManager, octree, player, cameraMover);
		}

		public IMenuController CreateMenuController(MHUrhoApp app)
		{
			return new MenuController();
		}
	}
}
