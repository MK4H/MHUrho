using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.Control
{
    public interface IMenuController
    {
        bool Enabled { get; }

        void Enable();

        void Disable();

        IGameController GetGameController(CameraController cameraController);
    }
}
