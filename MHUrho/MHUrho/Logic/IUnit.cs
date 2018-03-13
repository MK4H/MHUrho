using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Control;
using Urho;
using MHUrho.Storage;

namespace MHUrho.Logic
{
    public interface IUnit : ISelectable {

        int ID { get; }

        UnitType Type { get; }

        Vector2 Position { get; }

        ITile Tile { get; }

        IPlayer Player { get; }

        bool CanPass(ITile tile);

        float MovementSpeed(ITile tile);

        StUnit Save();
    }
}
