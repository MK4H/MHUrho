using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Control;
using MHUrho.Logic;
using MHUrho.Storage;
using MHUrho.UserInterface;
using Urho;

namespace MHUrho.Plugins
{
	public abstract class BuildingInstancePlugin : EntityInstancePlugin {
		public IBuilding Building { get; protected set; }

		protected BuildingInstancePlugin(ILevelManager level, IBuilding building) 
			:base (level, building) {
			this.Building = building;
		}

		public abstract bool CanChangeTileHeight(int x, int y);

		public virtual void TileHeightChanged(ITile tile)
		{

		}

		public virtual float? GetHeightAt(float x, float y)
		{
			return null;
		}

		public virtual IFormationController GetFormationController(Vector3 centerPosition)
		{
			return null;
		}

		public virtual void OnHit(IEntity byEntity, object userData)
		{

		}
	}
}
