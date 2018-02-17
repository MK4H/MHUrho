using System;
using System.Collections.Generic;
using System.Text;
using Urho;

namespace MHUrho.Logic
{
    public interface IUnit : ISelectable {
        int ID { get; }

        UnitType Type { get; }

        void Update(TimeSpan gameTime);

        Vector2 Position { get; }

        ITile Tile { get; }

        LogicManager Level { get; }

        IPlayer Player { get; }

        bool CanPass(ITile tile);

        float MovementSpeed(ITile tile);
    }
}
