using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using MHUrho.Logic;
using MHUrho.Storage;
using MHUrho.WorldMap;

namespace MHUrho.Plugins
{
    public abstract class PlayerAIInstancePlugin : InstancePlugin
    {

		protected IPlayer Player;

		protected PlayerAIInstancePlugin(ILevelManager level, IPlayer player)
			:base(level)
		{

			this.Player = player;
		}

		/// <summary>
		/// Initializes the player AI plugin on it's first loading into the level.
		/// During this call, all units, buildings and projectiles of this player should already be loaded and connected to this player.
		/// State of other players cannot is undefined.
		///
		/// If there is a saved state of the player with the same plugin type, <see cref="InstancePlugin.LoadState(PluginDataWrapper)"/>
		/// will be called instead.
		/// </summary>
		/// <param name="level">Current level into which this player is being loaded.</param>
		public abstract void Init(ILevelManager level);


		public virtual void OnBuildingDestroyed(IBuilding building)
		{

		}

		public virtual void OnUnitKilled(IUnit unit)
		{

		}

		public virtual void UnitAdded(IUnit unit)
		{

		}

		public virtual void BuildingAdded(IBuilding building)
		{

		}

		/// <summary>
		/// Message sent when an amount of resource that player owns changes. This method should check if this
		/// change is possible and return the wanted new value.
		/// </summary>
		/// <param name="resourceType">Type of the resource the amount changed for.</param>
		/// <param name="currentAmount">Current amount of the resource.</param>
		/// <param name="requestedNewAmount">The requested new amount of the resource.</param>
		/// <returns>New amount of the resource.</returns>
		public virtual double ResourceAmountChanged(ResourceType resourceType, double currentAmount, double requestedNewAmount)
		{
			return requestedNewAmount;
		}
	}
}
