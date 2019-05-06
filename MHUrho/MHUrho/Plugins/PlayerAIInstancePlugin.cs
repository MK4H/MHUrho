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
