using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.CameraMovement;
using MHUrho.EditorTools;
using MHUrho.Logic;
using Urho;

namespace MHUrho.Input.Touch
{
    public class TouchFactory : ControllerFactory
    {
		public TouchFactory()
		{

		}

		public override ICameraController CreateCameraController(IGameController gameController, CameraMover cameraMover)
		{
			throw new NotImplementedException();
		}

		public override IGameController CreateGameController(CameraMover cameraMover, ILevelManager levelManager, Octree octree, IPlayer player)
		{
			return new GameController(levelManager, octree, player, cameraMover);
		}

		public override IMenuController CreateMenuController(MHUrhoApp app)
		{
			return new MenuController();
		}
	}
}
