using System;
using System.Collections.Generic;
using System.Text;

namespace MHUrho.Control
{
    public interface IGameController
    {
        bool Enabled { get; }

        void Enable();

        void Disable();
    }
}
