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
	public class BuildingType : IEntityType, IDisposable
	{
		public int ID { get; private set; }

		public string Name { get; private set; }

		public GamePack Package { get; private set; }

		public AssetContainer Assets { get; private set; }

		public IntRect IconRectangle { get; private set; }

		public IntVector2 Size { get; private set; }

		public BuildingTypePlugin Plugin { get; private set; }

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
		/// <param name="id"></param>
		/// <param name="name"></param>
		/// <param name="package"></param>
		/// <param name="assets"></param>
		/// <param name="iconRectangle"></param>
		/// <param name="size"></param>
		/// <param name="plugin"></param>
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
		/// Clears any cache state dependent on the current level
		/// </summary>
		public void ClearCache()
		{
			//If you add any cache dependent on current level, clear it here
		}

		internal IBuilding BuildNewBuilding(int buildingID,
										 ILevelManager level, 
										 IntVector2 topLeft,
										 Quaternion initRotation,
										 IPlayer player) {

			return Building.CreateNew(buildingID, topLeft, initRotation, this, player, level);
		}

		public bool CanBuild(IntVector2 topLeft, IntVector2 bottomRight, IPlayer owner, ILevelManager level) {
			try {
				return Plugin.CanBuild(topLeft, bottomRight, owner, level);
			}
			catch (Exception e) {
				Urho.IO.Log.Write(LogLevel.Error,
								$"Building type plugin call {nameof(Plugin.CanBuild)} failed with Exception: {e.Message}");
				return false;
			}
			
		}

		public bool CanBuild(IntRect buildingTilesRectangle, IPlayer owner, ILevelManager level) {
			return CanBuild(buildingTilesRectangle.TopLeft(), buildingTilesRectangle.BottomRight(), owner, level);
		}
 
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

		public IntRect GetBuildingTilesRectangle(IntVector2 topLeft) {
			return new IntRect(topLeft.X,
							   topLeft.Y,
							   topLeft.X + Size.X - 1,
							   topLeft.Y + Size.Y - 1);
		}


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
