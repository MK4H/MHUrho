using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.CameraMovement;
using MHUrho.EditorTools;
using MHUrho.Logic;
using Urho;

namespace MHUrho.Input.MouseKeyboard
{
	/// <summary>
	/// Implementation of the Abstract factory design pattern for abstraction of different input schemas.
	/// </summary>
    public class MouseKeyboardFactory : IControllerFactory
    {
		public MouseKeyboardFactory()
		{

		}

		/// <summary>
		/// Creates a controller for camera control using the mouse and keyboard.
		/// </summary>
		/// <param name="gameController">The platform input subsystem.</param>
		/// <param name="cameraMover">The camera movement directing component.</param>
		/// <returns>Controller that translates user input to camera movement.</returns>
		public ICameraController CreateCameraController(IGameController gameController, CameraMover cameraMover)
		{
			if (!(gameController is GameController typedController))
			{
				throw new ArgumentException("Wrong type of game controller", nameof(gameController));
			}
			return new CameraController(typedController, typedController.UIManager , cameraMover);
		}

		/// <summary>
		/// Creates controller for a game level.
		/// </summary>
		/// <param name="cameraMover">The component used for directing camera movement.</param>
		/// <param name="levelManager">The manager of the controlled level.</param>
		/// <param name="octree">The engine component used for raycasting.</param>
		/// <param name="player">The player the user will be controlling in the begining.</param>
		/// <returns>A controller for the given level.</returns>
		public IGameController CreateGameController(CameraMover cameraMover, ILevelManager levelManager, Octree octree, IPlayer player)
		{
			return new GameController(levelManager, octree, player, cameraMover);
		}

		/// <summary>
		/// Creates a controller for menu.
		/// </summary>
		/// <param name="app">The application instance.</param>
		/// <returns>A controller for menu.</returns>
		public IMenuController CreateMenuController(MHUrhoApp app)
		{
			return new MenuController(app);
		}
	}
}
