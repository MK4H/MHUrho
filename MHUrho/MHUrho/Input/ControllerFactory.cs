using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using Urho;
using Urho.Resources;

namespace MHUrho.Input
{
    public abstract class ControllerFactory {
		public abstract ICameraController CreateCameraController(IGameController gameController, CameraMover cameraMover);

		public abstract IGameController CreateGameController(CameraMover cameraMover, ILevelManager levelManager, Octree octree, Player player);

		public abstract IMenuController CreateMenuController();
	}
}
