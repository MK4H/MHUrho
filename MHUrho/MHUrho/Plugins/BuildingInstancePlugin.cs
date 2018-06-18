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
	public abstract class BuildingInstancePlugin : InstancePlugin {
		public IBuilding Building { get; protected set; }

		protected BuildingInstancePlugin(ILevelManager level, IBuilding building) 
			:base (level) {
			this.Building = building;
		}

		protected BuildingInstancePlugin() {

		}

		/// <summary>
		/// Loads instance into the state saved in <paramref name="pluginData"/>
		/// </summary>
		/// <param name="level"></param>
		/// <param name="building"></param>
		/// <param name="pluginData">stored state of the building plugin</param>
		/// <returns>Instance loaded into saved state</returns>
		public abstract void LoadState(ILevelManager level, IBuilding building, PluginDataWrapper pluginData);

		public virtual float? GetHeightAt(float x, float y)
		{
			return null;
		}

		public virtual IFormationController GetFormationController(Vector3 centerPosition)
		{
			return null;
		}

		public abstract void OnProjectileHit(IProjectile projectile);

		public abstract void OnMeeleHit(IEntity byEntity);
	}
}
