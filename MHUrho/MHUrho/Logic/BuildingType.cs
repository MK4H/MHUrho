using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using MHUrho.Control;
using MHUrho.Packaging;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.Helpers;
using MHUrho.Helpers.Extensions;
using Urho;
using Urho.Resources;
using Urho.Urho2D;

namespace MHUrho.Logic
{
	/// <summary>
	/// Represents a type of buildings defined in a package.
	/// </summary>
	public class BuildingType : IEntityType, IDisposable
	{
		///<inheritdoc/>
		public int ID { get; private set; }
		///<inheritdoc/>
		public string Name { get; private set; }

		///<inheritdoc/>
		public GamePack Package { get; private set; }

		/// <summary>
		/// The assets of this building type that will be added to
		/// every instance of this type.
		/// </summary>
		public AssetContainer Assets { get; private set; }

		/// <summary>
		/// Part of the <see cref="GamePack.BuildingIconTexture"/> representing this building type.
		/// </summary>
		public IntRect IconRectangle { get; private set; }

		/// <summary>
		/// Size of buildings of this type, in number of tiles.
		/// </summary>
		public IntVector2 Size { get; private set; }

		/// <summary>
		/// The type plugin of this building type.
		/// </summary>
		public BuildingTypePlugin Plugin { get; private set; }

		///<inheritdoc/>
		TypePlugin IEntityType.Plugin => Plugin;

		/// <summary>
		/// Data has to be loaded after constructor by <see cref="Load(XElement, int, GamePack)"/>
		/// It is done this way to allow cyclic references during the Load method, so anything 
		/// that references this buildingType back can get the reference during the loading of this instance
		/// </summary>
		public BuildingType() {

		}

		/// <summary>
		/// This constructor enables creation of mock instances, that are not loaded from package and have other uses.
		/// </summary>
		/// <param name="id">Identifier of the building type.</param>
		/// <param name="name">Name of the building type.</param>
		/// <param name="package">The package the building type is loaded from.</param>
		/// <param name="assets">The assets added to every instance of building of this type.</param>
		/// <param name="iconRectangle">Part of the <see cref="GamePack.BuildingIconTexture"/> representing this type.</param>
		/// <param name="size">Size of the buildings of this type in number of tiles.</param>
		/// <param name="plugin">Type plugin of this building type.</param>
		protected BuildingType(int id, 
							string name,
							GamePack package,
							AssetContainer assets, 
							IntRect iconRectangle, 
							IntVector2 size, 
							BuildingTypePlugin plugin)
		{
			this.ID = id;
			this.Name = name;
			this.Package = package;
			this.Assets = assets;
			this.IconRectangle = iconRectangle;
			this.Size = size;
			this.Plugin = plugin;
		}

		/// <summary>
		/// Loads building type from the given <paramref name="xml"/> element.
		/// Expects that the <paramref name="xml"/> is validated against the <see cref="PackageManager.schemas"/>.
		/// </summary>
		/// <param name="xml">The XML element to load the building from.</param>
		/// <param name="package">The package this Xml element is from.</param>
		public void Load(XElement xml, GamePack package) {

			Package = package;

			XElement extensionElem = null;
			string assemblyPath = null;
			try {
				ID = XmlHelpers.GetID(xml);
				Name = XmlHelpers.GetName(xml);
				IconRectangle = XmlHelpers.GetIconRectangle(xml);
				Size = XmlHelpers.GetIntVector2(xml.Element(BuildingTypeXml.Inst.Size));
				assemblyPath = XmlHelpers.GetPath(xml.Element(BuildingTypeXml.Inst.AssemblyPath));
				extensionElem = XmlHelpers.GetExtensionElement(xml);
			}
			catch (Exception e) {
				LoadError($"Building type loading failed: Invalid XML of the package {package.Name}", e);
			}

			try {
				Assets = AssetContainer.FromXml(xml.Element(BuildingTypeXml.Inst.Assets), package);
			}
			catch (Exception e) {
				LoadError($"Building type \"{Name}\"[{ID}] loading failed: Asset instantiation failed with exception: {e.Message}", e);
			}

			try {
				Plugin = TypePlugin.LoadTypePlugin<BuildingTypePlugin>(assemblyPath, package, Name, ID, extensionElem);
			}
			catch (Exception e) {
				LoadError($"Building type \"{Name}\"[{ID}] loading failed: Plugin loading failed with exception: {e.Message}", e);
			}
			
		}

		/// <summary>
		/// Compares building types for equality.
		/// </summary>
		/// <param name="obj">Other object.</param>
		/// <returns>True if the <paramref name="obj"/> is the same building type, false otherwise.</returns>
		public override bool Equals(object obj)
		{
			return object.ReferenceEquals(this, obj);
		}

		/// <summary>
		/// Returns hashcode of this building type.
		/// </summary>
		/// <returns>Returns hashcode of this building type.</returns>
		public override int GetHashCode()
		{
			return ID;
		}

		/// <summary>
		/// Clears any cache state dependent on the current level.
		/// </summary>
		public void ClearCache()
		{
			//If you add any cache dependent on current level, clear it here
		}

		/// <summary>
		/// Creates new building of this type.
		/// </summary>
		/// <param name="buildingID">The ID of the new building.</param>
		/// <param name="level">The level the building is created in.</param>
		/// <param name="topLeft">Position of the top left corner of the building.</param>
		/// <param name="initRotation">Initial rotation of the building.</param>
		/// <param name="player">Owner of the building.</param>
		/// <returns>Newly created building, or null if building cannot be created for some reason.</returns>
		/// <exception cref="CreationException">Thrown when there was an unexpected exception during the creation of the building.</exception>
		internal IBuilding BuildNewBuilding(int buildingID,
										 ILevelManager level, 
										 IntVector2 topLeft,
										 Quaternion initRotation,
										 IPlayer player) {

			return Building.CreateNew(buildingID, topLeft, initRotation, this, player, level);
		}

		/// <summary>
		/// Returns if this type of buildings can be built at <paramref name="topLeft"/> by the <paramref name="owner"/> in the
		/// <paramref name="level"/>.
		/// </summary>
		/// <param name="topLeft">The position of the building.</param>
		/// <param name="owner">Owner of the building.</param>
		/// <param name="level">Level to build the building in.</param>
		/// <returns>True if it can be built, false if it cannot.</returns>
		public bool CanBuild(IntVector2 topLeft, IPlayer owner, ILevelManager level) {
			try {
				return Plugin.CanBuild(topLeft, owner, level);
			}
			catch (Exception e) {
				Urho.IO.Log.Write(LogLevel.Error,
								$"Building type plugin call {nameof(Plugin.CanBuild)} failed with Exception: {e.Message}");
				return false;
			}
			
		}
 
		/// <summary>
		/// Creates an instance plugin for a new building <paramref name="building"/>.
		/// </summary>
		/// <param name="building">The new building to get the instance plugin for.</param>
		/// <param name="level">The level the building is in.</param>
		/// <returns>New instance of instance plugin to be used to control the <paramref name="building"/>.</returns>
		internal BuildingInstancePlugin GetNewInstancePlugin(IBuilding building, ILevelManager level) {
			try {
				return Plugin.CreateNewInstance(level, building);
			}
			catch (Exception e) {
				Urho.IO.Log.Write(LogLevel.Error,
								$"Building type plugin call {nameof(Plugin.CreateNewInstance)} failed with Exception: {e.Message}");
				throw;
			}
			
		}

		/// <summary>
		/// Creates an instance plugin for a loading building <paramref name="building"/>.
		/// </summary>
		/// <param name="building">The loading building to get the instance plugin for.</param>
		/// <param name="level">The level the building is loading into.</param>
		/// <returns>New instance of instance plugin to be used to control the <paramref name="building"/>.</returns>
		internal BuildingInstancePlugin GetInstancePluginForLoading(IBuilding building, ILevelManager level) {
			try {
				return Plugin.GetInstanceForLoading(level, building);
			}
			catch (Exception e)
			{
				Urho.IO.Log.Write(LogLevel.Error,
								$"Building type plugin call {nameof(Plugin.GetInstanceForLoading)} failed with Exception: {e.Message}");
				throw;
			}
			
		}

		/// <summary>
		/// Gets the rectangle taken up by the building if placed with top left corner at <paramref name="topLeft"/>.
		/// </summary>
		/// <param name="topLeft">The position of the top left corner of the building.</param>
		/// <returns>Rectangle taken up by the building placed with it's top left corner at <paramref name="topLeft"/>. </returns>
		public IntRect GetBuildingTilesRectangle(IntVector2 topLeft)
		{
			IntVector2 bottomRight = GetBottomRightTileIndex(topLeft);
			return new IntRect(topLeft.X,
							   topLeft.Y,
							   bottomRight.X,
							   bottomRight.Y);
		}

		/// <summary>
		/// Gets the position of the bottom right corner if placed with top left corner at <paramref name="topLeft"/>.
		/// </summary>
		/// <param name="topLeft">The position of the top left corner of the building.</param>
		/// <returns>The position of the bottom right corner of a building of this type placed with it's top left corner at <paramref name="topLeft"/>. </returns>
		public IntVector2 GetBottomRightTileIndex(IntVector2 topLeft)
		{
			return topLeft + Size - new IntVector2(1, 1);
		}

		/// <summary>
		/// Releases all resources.
		/// </summary>
		public void Dispose() {
			Assets.Dispose();
		}

		/// <summary>
		/// Logs message and throws a <see cref="PackageLoadingException"/>
		/// </summary>
		/// <param name="message">Message to log and propagate via exception</param>
		/// <exception cref="PackageLoadingException">Always throws this exception</exception>
		void LoadError(string message, Exception e)
		{
			Urho.IO.Log.Write(LogLevel.Error, message);
			throw new PackageLoadingException(message, e);
		}
	}
}
