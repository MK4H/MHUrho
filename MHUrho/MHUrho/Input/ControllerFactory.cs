using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.EditorTools;
using MHUrho.Logic;
using Urho;
using Urho.Resources;

namespace MHUrho.Input
{
    public abstract class ControllerFactory {
		public abstract ICameraController CreateCameraController(IGameController gameController, CameraMover cameraMover);

		public abstract IGameController CreateGameController(CameraMover cameraMover, ILevelManager levelManager, Octree octree, IPlayer player);

		public abstract IMenuController CreateMenuController();

		public abstract ToolManager CreateToolManager(IGameController gameController, CameraMover cameraMover);
	}
}
