using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using Urho;

namespace MHUrho.Control
{
    public interface IMenuController
    {
        bool Enabled { get; }

        void Enable();

        void Disable();

        IGameController GetGameController(CameraController cameraController, LevelManager levelManager);
    }
}
