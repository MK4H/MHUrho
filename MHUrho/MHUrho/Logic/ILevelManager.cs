using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Packaging;
using MHUrho.UnitComponents;
using MHUrho.WorldMap;
using Urho;

namespace MHUrho.Logic {
    public interface ILevelManager {
        float GameSpeed { get; set; }

        Map Map { get; }

        Scene Scene { get; }

        DefaultComponentFactory DefaultComponentFactory { get; }

        PackageManager PackageManager { get; }

        event OnUpdateDelegate Update;

        IEnumerable<Unit> Units { get; }

        IEnumerable<Player> Players { get; }

        IEnumerable<Building> Buildings { get; }

        Unit SpawnUnit(UnitType unitType, ITile tile, IPlayer player);

        Building BuildBuilding(BuildingType buildingType, IntVector2 topLeft, IPlayer player);

        Unit GetUnit(int ID);

        Building GetBuilding(int ID);

        Player GetPlayer(int ID);
    }
}
