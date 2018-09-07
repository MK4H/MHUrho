using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.EditorTools;
using MHUrho.Logic;
using Urho;

namespace MHUrho.Input
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
			return new GameTouchController(levelManager, octree, player, cameraMover);
		}

		public override IMenuController CreateMenuController()
		{
			return new MenuTouchController();
		}

		public override ToolManager CreateToolManager(IGameController gameController, CameraMover cameraMover)
		{
			throw new NotImplementedException();
		}
	}
}
