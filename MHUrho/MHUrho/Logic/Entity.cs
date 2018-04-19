using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using MHUrho.WorldMap;

namespace MHUrho.Logic
{
	public abstract class Entity : Component
	{

		/// <summary>
		/// ID of this entity
		/// Hides component member ID, but having two IDs would be more confusing
		/// 
		/// If you need component ID, just cast this to component and access ID
		/// </summary>
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
