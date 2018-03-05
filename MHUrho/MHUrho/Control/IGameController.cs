using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;

namespace MHUrho.Control
{
    public interface IGameController : IDisposable
    {
        //TODO: MOVE THIS ELSEWHERE
        TileType SelectedTileType { get; }

        bool Enabled { get; }

        bool DoOnlySingleRaycasts { get; set; }

        void Enable();

        void Disable();

    }
}
