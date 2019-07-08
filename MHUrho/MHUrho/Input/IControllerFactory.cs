using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.CameraMovement;
using MHUrho.EditorTools;
using MHUrho.Logic;
using Urho;
using Urho.Resources;

namespace MHUrho.Input
{
    public interface IControllerFactory {
		ICameraController CreateCameraController(IGameController gameController, CameraMover cameraMover);

		IGameController CreateGameController(CameraMover cameraMover, ILevelManager levelManager, Octree octree, IPlayer player);

		IMenuController CreateMenuController(MHUrhoApp app);
	}
}
