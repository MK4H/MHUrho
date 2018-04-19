using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Control;
using MHUrho.Logic;
using Urho;

namespace MHUrho.Input
{
	public interface IMenuController
	{
		bool Enabled { get; }

		void Enable();

		void Disable();

		IGameController GetGameController(CameraController cameraController, ILevelManager levelManager, Player player);
	}
}
