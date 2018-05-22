﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.Storage;
using Urho;

namespace MHUrho.Plugins
{
	public abstract class UnitTypePlugin : TypePlugin
	{

		/// <summary>
		/// Data overriding or superseding the XML data describing the type
		/// </summary>
		public virtual UnitTypeInitializationData TypeData { get; } = null;

		/// <summary>
		/// Create new instance of the unit in with default contents
		/// 
		/// Add components from <see cref="MHUrho.UnitComponents"/> to <see name="unit.Node"/> and/or
		/// create your own Plugin in <see cref="UnitInstancePlugin.OnUpdate(float)"/>
		/// </summary>
		/// <param name="level"></param>
		/// <param name="unit"></param>
		/// <returns></returns>
		public abstract UnitInstancePlugin CreateNewInstance(ILevelManager level, IUnit unit);


		/// <summary>
		/// Creates instance of <see cref="UnitInstancePlugin"/> that will be loaded by <see cref="UnitInstancePlugin.LoadState(LevelManager, PluginDataWrapper)"/>
		/// </summary>
		/// <returns></returns>
		public abstract UnitInstancePlugin GetInstanceForLoading();


		/// <summary>
		/// Checks if the UnitType can be spawned at <paramref name="centerTile"/>
		/// </summary>
		/// <param name="centerTile">Tile to spawn the unit at, the center of the unit will be at the center of the tile</param>
		/// <returns>true if can, false if cannot</returns>
		public abstract bool CanSpawnAt(ITile centerTile);

	}
}