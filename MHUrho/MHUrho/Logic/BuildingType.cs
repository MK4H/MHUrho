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
		//XML ELEMENTS AND ATTRIBUTES
		private static readonly XName SizeElementName = PackageManager.XMLNamespace + "size";


		public int ID { get; set; }

		public string Name { get; private set; }

		public GamePack Package { get; private set; }

		public ModelWrapper Model { get; private set; }

		public MaterialWrapper Material { get; private set; }

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
			Model = XmlHelpers.GetModel(xml);
			Material = XmlHelpers.GetMaterial(xml);
			IconRectangle = XmlHelpers.GetIconRectangle(xml);
			IsManuallySpawnable = XmlHelpers.GetManuallySpawnable(xml);
			Package = package;
			Size = XmlHelpers.GetIntVector2(xml.Element(SizeElementName));
			Plugin = XmlHelpers.LoadTypePlugin<BuildingTypePlugin>(xml,
																 package.XmlDirectoryPath,
																 Name);
			Plugin.Initialize(XmlHelpers.GetExtensionElement(xml),
										 package.PackageManager);
		}

		public Building BuildNewBuilding(int buildingID, 
										 Node buildingNode, 
										 ILevelManager level, 
										 IntVector2 topLeft, 
										 IPlayer player) {

			return Building.Loader.CreateNew(buildingID, topLeft, this, buildingNode, player, level);
		}

		public bool CanBuildIn(IntVector2 topLeft, IntVector2 bottomRight, ILevelManager level) {
			return Plugin.CanBuildIn(topLeft, bottomRight, level);
		}

		public bool CanBuildIn(IntRect buildingTilesRectangle, ILevelManager level) {
			return CanBuildIn(buildingTilesRectangle.TopLeft(), buildingTilesRectangle.BottomRight(), level);
		}
 
		public BuildingInstancePlugin GetNewInstancePlugin(Building building, ILevelManager level) {
			return Plugin.CreateNewInstance(level, building);
		}

		public BuildingInstancePlugin GetInstancePluginForLoading() {
			return Plugin.GetInstanceForLoading();
		}

		public IntRect GetBuildingTilesRectangle(IntVector2 topLeft) {
			return new IntRect(topLeft.X,
							   topLeft.Y,
							   topLeft.X + Size.X - 1,
							   topLeft.Y + Size.Y - 1);
		}


		public void Dispose() {
			Model?.Dispose();
		}



	}
}
