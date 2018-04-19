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

namespace MHUrho.Logic
{
	public class UnitType : IEntityType, IDisposable
	{


		public int ID { get; set; }

		public string Name { get; private set; }

		public GamePack Package { get; private set; }

		public Model Model { get; private set; }

		public MaterialWrapper Material { get; private set; }

		public IReadOnlyDictionary<int, Animation> Animations => animations;

		public Image Icon { get; private set; }

		public object Plugin => unitTypeLogic;

		private UnitTypePluginBase unitTypeLogic;

		private Dictionary<int, Animation> animations;

		//TODO: More loaded properties

		/// <summary>
		/// Data has to be loaded after constructor by <see cref="Load(XElement, int, GamePack)"/>
		/// It is done this way to allow cyclic references during the Load method, so anything 
		/// that references this unitType back can get the reference during the loading of this instance
		/// </summary>
		public UnitType() {
			animations = new Dictionary<int, Animation>();
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

			unitTypeLogic =
				XmlHelpers.LoadTypePlugin<UnitTypePluginBase>(xml,
															 package.XmlDirectoryPath,
															 Name);

			var data = unitTypeLogic.TypeData;

			Model = XmlHelpers.GetModel(xml, package.XmlDirectoryPath);
			Material = XmlHelpers.GetMaterial(xml, package.XmlDirectoryPath);
			Icon = XmlHelpers.GetIcon(xml, package.XmlDirectoryPath);
			
			unitTypeLogic.Initialize(XmlHelpers.GetExtensionElement(xml),
									 package.PackageManager);
		}


		public bool CanSpawnAt(ITile tile) {
			return unitTypeLogic.CanSpawnAt(tile);
		}

		/// <summary>
		/// Creates new instance of this unit type positioned at <paramref name="tile"/>
		/// </summary>
		/// <param name="unitID">identifier unique between units</param>
		/// <param name="unitNode">scene node of the new unit</param>
		/// <param name="level">Level where the unit is being created</param>
		/// <param name="tile">tile where the unit will spawn</param>
		/// <param name="player">owner of the unit</param>
		/// <returns>New unit of this type</returns>
		public Unit CreateNewUnit(int unitID, 
								  Node unitNode, 
								  ILevelManager level, 
								  ITile tile, 
								  IPlayer player) {
			var unit = Unit.CreateNew(unitID, unitNode, this, level, tile, player);
			AddComponents(unitNode);

			return unit;
		}

		/// <summary>
		/// Does first stage of loading a new instance of <see cref="Unit"/> from <paramref name="storedUnit"/> and adds it
		/// to the <paramref name="unitNode"/>
		/// 
		/// Also adds all other components needed by this <see cref="UnitType"/>
		/// Needs to be followed by <see cref="Unit.ConnectReferences"/> and then <see cref="Unit.FinishLoading"/>
		/// </summary>
		/// <param name="level"></param>
		/// <param name="unitNode">scene node representing the new unit</param>
		/// <returns></returns>
		internal void LoadComponentsForUnit(LevelManager level, Node unitNode) {
			AddComponents(unitNode);
		}

		public UnitInstancePluginBase GetNewInstancePlugin(Unit unit, ILevelManager level) {
			return unitTypeLogic.CreateNewInstance(level, unit);
		}

		public UnitInstancePluginBase GetInstancePluginForLoading() {
			return unitTypeLogic.GetInstanceForLoading();
		}

		public void Dispose() {
			//TODO: Release all disposable resources
			Model.Dispose();
			Material.Dispose();
			Icon.Dispose();

			foreach (var animation in animations.Values) {
				animation.Dispose();
			}
		}

		/// <summary>
		/// Adds components according to the XML file
		/// </summary>
		/// <param name="unitNode"></param>
		private void AddComponents(Node unitNode) {
			//TODO: READ FROM XML
			//TODO: Animated model
			var staticModel = unitNode.CreateComponent<AnimatedModel>();
			staticModel.Model = Model;
			Material.ApplyMaterial(staticModel);
			staticModel.CastShadows = true;

			//TODO: Add needed components
		}


	}
}
