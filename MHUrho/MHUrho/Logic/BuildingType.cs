using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using MHUrho.Control;
using MHUrho.Packaging;
using MHUrho.Plugins;
using MHUrho.Storage;
using MHUrho.Helpers;
using Urho;
using Urho.Resources;
using Urho.Urho2D;

namespace MHUrho.Logic
{
	public class BuildingType : ILoadableType, IDisposable
	{
		public int ID { get; private set; }

		public string Name { get; private set; }

		public GamePack Package { get; private set; }

		public AssetContainer Assets { get; private set; }

		public IntRect IconRectangle { get; private set; }

		public IntVector2 Size { get; private set; }

		public BuildingTypePlugin Plugin { get; private set; }

		public bool IsManuallySpawnable { get; private set; }
		
		/// <summary>
		/// Data has to be loaded after constructor by <see cref="Load(XElement, int, GamePack)"/>
		/// It is done this way to allow cyclic references during the Load method, so anything 
		/// that references this buildingType back can get the reference during the loading of this instance
		/// </summary>
		public BuildingType() {

		}

		public void Load(XElement xml, GamePack package) {
			ID = XmlHelpers.GetID(xml);
			Name = XmlHelpers.GetName(xml);
			Assets = AssetContainer.FromXml(xml.Element(BuildingTypeXml.Inst.Assets));
			IconRectangle = XmlHelpers.GetIconRectangle(xml);
			IsManuallySpawnable = XmlHelpers.GetManuallySpawnable(xml);
			Package = package;
			//TODO: Maybe remove
			Size = XmlHelpers.GetIntVector2(xml.Element(BuildingTypeXml.Inst.Size));

			XElement pathElement = xml.Element(BuildingTypeXml.Inst.AssemblyPath);

			Plugin = TypePlugin.LoadTypePlugin<BuildingTypePlugin>(XmlHelpers.GetPath(pathElement), package, Name);
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

		internal IBuilding BuildNewBuilding(int buildingID,
										 ILevelManager level, 
										 IntVector2 topLeft, 
										 IPlayer player) {

			return Building.CreateNew(buildingID, topLeft, this, player, level);
		}

		public bool CanBuildIn(IntVector2 topLeft, IntVector2 bottomRight, ILevelManager level) {
			return Plugin.CanBuildIn(topLeft, bottomRight, level);
		}

		public bool CanBuildIn(IntRect buildingTilesRectangle, ILevelManager level) {
			return CanBuildIn(buildingTilesRectangle.TopLeft(), buildingTilesRectangle.BottomRight(), level);
		}
 
		internal BuildingInstancePlugin GetNewInstancePlugin(IBuilding building, ILevelManager level) {
			return Plugin.CreateNewInstance(level, building);
		}

		internal BuildingInstancePlugin GetInstancePluginForLoading(IBuilding building, ILevelManager level) {
			return Plugin.GetInstanceForLoading(level, building);
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



	}
}
