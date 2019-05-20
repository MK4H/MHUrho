using System;
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
	public class UnitType : IEntityType, IDisposable
	{

		public int ID { get; private set; }

		public string Name { get; private set; }

		public GamePack Package { get; private set; }

		public AssetContainer Assets { get; private set; }

		public IntRect IconRectangle { get; private set; }

		public UnitTypePlugin Plugin { get; private set; }

		TypePlugin IEntityType.Plugin => Plugin;

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
		/// <param name="package">Package this unitType belongs to</param>
		/// <returns>UnitType with filled standard members</returns>
		public void Load(XElement xml, GamePack package) {
			
			Package = package;

			//The XML should be validated, there should be no errors
			string assemblyPath = null;
			XElement assetsElement = null;
			XElement extensionElem = null;
			try {
				ID = XmlHelpers.GetID(xml);
				Name = XmlHelpers.GetName(xml);
				IconRectangle = XmlHelpers.GetIconRectangle(xml);
				assemblyPath = XmlHelpers.GetPath(xml.Element(UnitTypeXml.Inst.AssemblyPath));
				assetsElement = xml.Element(UnitTypeXml.Inst.Assets);
				extensionElem = XmlHelpers.GetExtensionElement(xml);
			}
			catch (Exception e) {
				LoadError($"Unit type loading failed: Invalid XML of the package {package.Name}", e);
			}

			try
			{
				Assets = AssetContainer.FromXml(assetsElement);
			}
			catch (Exception e)
			{
				LoadError($"Unit type \"{Name}\"[{ID}] loading failed: Asset instantiation failed with exception: {e.Message}", e);
			}

			try {
				Plugin = TypePlugin.LoadTypePlugin<UnitTypePlugin>(assemblyPath, package, Name, ID, extensionElem);

			}
			catch (Exception e) {
				LoadError($"Unit type \"{Name}\"[{ID}] loading failed: Plugin loading failed with exception: {e.Message}", e);
			}
		}

		/// <summary>
		/// Clears any cache state dependent on the current level
		/// </summary>
		public void ClearCache()
		{
			//If you add any cache dependent on current level, clear it here
		}


		public bool CanSpawnAt(ITile tile) {
			try {
				return Plugin.CanSpawnAt(tile);
			}
			catch (Exception e) {
				Urho.IO.Log.Write(LogLevel.Error,
								$"Unit type plugin call {nameof(Plugin.CanSpawnAt)} failed with Exception: {e.Message}");
				return false;
			}
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
			try {
				return Plugin.CreateNewInstance(level, unit);
			}
			catch (Exception e) {
				Urho.IO.Log.Write(LogLevel.Error,
								$"Unit type plugin call {nameof(Plugin.CreateNewInstance)} failed with Exception: {e.Message}");
				throw;
			}
			
		}

		internal UnitInstancePlugin GetInstancePluginForLoading(IUnit unit, ILevelManager level) {
			try {
				return Plugin.GetInstanceForLoading(level, unit);
			}
			catch (Exception e)
			{
				Urho.IO.Log.Write(LogLevel.Error,
								$"Unit type plugin call {nameof(Plugin.GetInstanceForLoading)} failed with Exception: {e.Message}");
				throw;
			}
		}

		public void Dispose() {
			//NOTE: Release all disposable resources
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
