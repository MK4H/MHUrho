using System;
using System.Collections.Generic;
using System.Text;
using Urho;

namespace MHUrho.Control
{
    public abstract class Selected {
        public enum SelectedType {
            MapVerticies,
            TileRectangle,
            NewBuilding,
            NewUnit,
            WorldUnits,
            WorldBuildings

        }

        public SelectedType Type { get; protected set; }
    }

    public class SelectedMapVerticies : Selected {

        public IEnumerable<IntVector2> Verticies;

        private List<IntVector2> verticies;

        /// <summary>
        /// Adds vertex to selected if it is not already selected
        /// </summary>
        /// <param name="vertex"></param>
        public void AddVertex(IntVector2 vertex) {
            if (verticies.Contains(vertex)) return;

            verticies.Add(vertex);
        }

        public void RemoveVertex(IntVector2 vertex) {
            verticies.Remove(vertex);
        }

        public SelectedMapVerticies() {
            this.verticies = new List<IntVector2>();
            this.Type = SelectedType.MapVerticies;
        }
    }

    public class SelectedNewBuilding {

    }

    public class SelectedNewUnit {

    }


    public class SelectedTileRectangle : Selected {
        public IntRect Rectangle { get; set; }

        public SelectedTileRectangle(IntRect rectangle) {
            this.Rectangle = rectangle;
            this.Type = SelectedType.TileRectangle;
        }
    }

    public class SelectedWorldUnits : Selected {
        //TODO: THIS
    }

    public class SelectedWorldBuildings : Selected {
        //TODO: THIS
    }
}
