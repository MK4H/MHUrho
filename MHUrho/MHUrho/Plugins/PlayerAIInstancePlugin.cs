using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using MHUrho.Logic;
using MHUrho.Storage;
using MHUrho.WorldMap;

namespace MHUrho.Plugins
{
	/// <summary>
	/// Base class for player AI instance plugins.
	/// </summary>
    public abstract class PlayerAIInstancePlugin : InstancePlugin
    {
		/// <summary>
		/// The player this plugin is controlling.
		/// </summary>
		public IPlayer Player { get; private set; }

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

		/// <summary>
		/// Invoked when building owned by the <see cref="Player"/> is destroyed.
		/// </summary>
		/// <param name="building">The destroyed building.</param>
		public virtual void BuildingDestroyed(IBuilding building)
		{

		}

		/// <summary>
		/// Invoked when unit owned by the <see cref="Player"/> is killed.
		/// </summary>
		/// <param name="unit">The killed unit.</param>
		public virtual void UnitKilled(IUnit unit)
		{

		}

		/// <summary>
		/// Invoked when new unit is added to the ownership of the <see cref="Player"/>.
		/// </summary>
		/// <param name="unit">The newly added unit.</param>
		public virtual void UnitAdded(IUnit unit)
		{

		}

		/// <summary>
		/// Invoked when new building is added to the ownership of the <see cref="Player"/>.
		/// </summary>
		/// <param name="building">The newly added building.</param>
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
