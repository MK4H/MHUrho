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

namespace MHUrho.Logic
{
	public class BuildingType : IEntityType, IDisposable
	{
		//XML ELEMENTS AND ATTRIBUTES
		private const string SizeElementName = "size";


		public int ID { get; set; }

		public string Name { get; private set; }

		public GamePack Package { get; private set; }

		public Model Model { get; private set; }

		public MaterialWrapper Material { get; private set; }

		public Image Icon { get; private set; }

		public IntVector2 Size { get; private set; }

		public object Plugin => buildingTypeLogic;

		private BuildingTypePluginBase buildingTypeLogic;


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
			//TODO: Join the implementations from all the 
			Model = XmlHelpers.GetModel(xml,package.XmlDirectoryPath);
			Material = XmlHelpers.GetMaterial(xml, package.XmlDirectoryPath);
			Icon = XmlHelpers.GetIcon(xml, package.XmlDirectoryPath);
			Package = package;
			Size = XmlHelpers.GetIntVector2(xml, SizeElementName);
			buildingTypeLogic = XmlHelpers.LoadTypePlugin<BuildingTypePluginBase>(xml,
																				 package.XmlDirectoryPath,
																				 Name);
			buildingTypeLogic.Initialize(XmlHelpers.GetExtensionElement(xml),
										 package.PackageManager);
		}

		public Building BuildNewBuilding(int buildingID, 
										 Node buildingNode, 
										 ILevelManager level, 
										 IntVector2 topLeft, 
										 IPlayer player) {
			buildingNode.Scale = new Vector3(Size.X, 3, Size.Y);
			var building = Building.BuildAt(buildingID, topLeft, this, buildingNode, player, level);

			//TODO: Probably add animatedModel before creating building instance, and pass the model to the BuildAt method to control the animations from plugin
			AddComponents(buildingNode);

			return building;
		}

		internal void LoadComponentsForBuilding(LevelManager level, Node buildingNode) {
			buildingNode.Scale = new Vector3(Size.X, 3, Size.Y);
			AddComponents(buildingNode);
		}


		public bool CanBuildIn(IntVector2 topLeft, IntVector2 bottomRight, ILevelManager level) {
			return buildingTypeLogic.CanBuildIn(topLeft, bottomRight, level);
		}

		public bool CanBuildIn(IntRect buildingTilesRectangle, ILevelManager level) {
			return CanBuildIn(buildingTilesRectangle.TopLeft(), buildingTilesRectangle.BottomRight(), level);
		}
 
		public BuildingInstancePluginBase GetNewInstancePlugin(Building building, ILevelManager level) {
			return buildingTypeLogic.CreateNewInstance(level, building);
		}

		public BuildingInstancePluginBase GetInstancePluginForLoading() {
			return buildingTypeLogic.GetInstanceForLoading();
		}

		public IntRect GetBuildingTilesRectangle(IntVector2 topLeft) {
			return new IntRect(topLeft.X,
							   topLeft.Y,
							   topLeft.X + Size.X - 1,
							   topLeft.Y + Size.Y - 1);
		}


		public void Dispose() {
			Model?.Dispose();
			Icon?.Dispose();
		}

		private void AddComponents(Node buildingNode) {
			var staticModel = buildingNode.CreateComponent<StaticModel>();
			staticModel.Model = Model;
			Material.ApplyMaterial(staticModel);
			staticModel.CastShadows = true;
		}

	}
}
