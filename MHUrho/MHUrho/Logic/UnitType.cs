﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using MHUrho.Control;
using MHUrho.Helpers;
using MHUrho.Packaging;
using MHUrho.Storage;
using MHUrho.Plugins;
using Urho;
using Urho.Resources;
using Urho.Urho2D;

namespace MHUrho.Logic
{
	public class UnitType : ILoadableType, IDisposable
	{

		public int ID { get; private set; }

		public string Name { get; private set; }

		public GamePack Package { get; private set; }

		public AssetContainer Assets { get; private set; }

		public IntRect IconRectangle { get; private set; }

		public UnitTypePlugin Plugin { get; private set; }

		public bool IsManuallySpawnable { get; private set; }

		//TODO: More loaded properties

		/// <summary>
		/// Data has to be loaded after constructor by <see cref="Load(XElement, int, GamePack)"/>
		/// It is done this way to allow cyclic references during the Load method, so anything 
		/// that references this unitType back can get the reference during the loading of this instance
		/// </summary>
		public UnitType() {

		}

		/// <summary>
		/// Loads the standard data of the unitType from the xml
		/// 
		/// THE STANDARD DATA cannot reference any other types, it would cause infinite cycles
		/// 
		/// After this loading, you should register this type so it can be referenced, and then call
		/// <see cref="UnitType.ParseExtensionData(XElement, GamePack)"/>
		/// </summary>
		/// <param name="xml">xml element describing the type, according to <see cref="PackageManager.XMLNamespace"/> schema</param>
		/// <param name="newID">ID of this type in the current game</param>
		/// <param name="package">Package this unitType belongs to</param>
		/// <returns>UnitType with filled standard members</returns>
		public void Load(XElement xml, GamePack package) {
			//TODO: Check for errors
			ID = XmlHelpers.GetID(xml);
			Name = XmlHelpers.GetName(xml);
			Package = package;

			XElement pathElement = xml.Element(UnitTypeXml.Inst.AssemblyPath);

			Plugin = TypePlugin.LoadTypePlugin<UnitTypePlugin>(XmlHelpers.GetPath(pathElement), package, Name);

			//var data = Plugin.TypeData;

			Assets = AssetContainer.FromXml(xml.Element(UnitTypeXml.Inst.Assets));
			IconRectangle = XmlHelpers.GetIconRectangle(xml);
			IsManuallySpawnable = XmlHelpers.GetManuallySpawnable(xml);

			Plugin.Initialize(XmlHelpers.GetExtensionElement(xml),
									 package);
		}

		/// <summary>
		/// Clears any cache state dependent on the current level
		/// </summary>
		public void ClearCache()
		{
			//If you add any cache dependent on current level, clear it here
		}


		public bool CanSpawnAt(ITile tile) {
			return Plugin.CanSpawnAt(tile);
		}

		/// <summary>
		/// Creates new instance of this unit type positioned at <paramref name="tile"/>.
		/// </summary>
		/// <param name="unitID">identifier unique between units.</param>
		/// <param name="level">Level where the unit is being created.</param>
		/// <param name="tile">Tile where the unit will spawn.</param>
		/// <param name="initRotation">Initial rotation of the unit after it is created.</param>
		/// <param name="player">Owner of the unit.</param>
		/// <returns>New unit of this type.</returns>
		internal IUnit CreateNewUnit(int unitID,
								ILevelManager level,
								ITile tile,
								Quaternion initRotation,
								IPlayer player) {


			return Unit.CreateNew(unitID, this, level, tile, initRotation, player);
		}



		internal UnitInstancePlugin GetNewInstancePlugin(IUnit unit, ILevelManager level) {
			return Plugin.CreateNewInstance(level, unit);
		}

		internal UnitInstancePlugin GetInstancePluginForLoading(IUnit unit, ILevelManager level) {
			return Plugin.GetInstanceForLoading(level, unit);
		}

		public void Dispose() {
			//TODO: Release all disposable resources
			Assets.Dispose();
		}




	}
}
