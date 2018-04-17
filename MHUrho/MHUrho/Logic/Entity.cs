using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using MHUrho.WorldMap;

namespace MHUrho.Logic
{
    public abstract class Entity : Component
    {
        public new int ID { get; protected set; }

        public IPlayer Player { get; protected set; }

        public ILevelManager Level { get; protected set; }

        public Map Map => Level.Map;

        public Vector3 Position => Node.Position;

        protected Entity(int ID, ILevelManager level) {
            this.ID = ID;
            this.Level = level;
        }
    }
}
