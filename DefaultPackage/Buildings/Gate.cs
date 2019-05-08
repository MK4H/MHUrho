using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using MHUrho.Control;
using MHUrho.Logic;
using MHUrho.Packaging;
using MHUrho.Plugins;
using MHUrho.Storage;
using Urho;

namespace DefaultPackage
{
	public class GateType : BuildingTypePlugin
	{
		public override string Name { get; }
		public override int ID { get; }
		public override void Initialize(XElement extensionElement, GamePack package)
		{
			throw new NotImplementedException();
		}

		public override BuildingInstancePlugin CreateNewInstance(ILevelManager level, IBuilding building)
		{
			throw new NotImplementedException();
		}

		public override BuildingInstancePlugin GetInstanceForLoading(ILevelManager level, IBuilding projectile)
		{
			throw new NotImplementedException();
		}

		public override bool CanBuildIn(IntVector2 topLeftTileIndex, IntVector2 bottomRightTileIndex, ILevelManager level)
		{
			throw new NotImplementedException();
		}
	}

	public class GateInstance : BuildingInstancePlugin {

		bool isOpen;


		public GateInstance(ILevelManager level, IBuilding building)
			: base(level, building)
		{ }

		public void Open()
		{

		}

		public void Close()
		{

		}

		public override void SaveState(PluginDataWrapper pluginData)
		{
			var writer = pluginData.GetWriterForWrappedSequentialData();
			writer.StoreNext(isOpen);
		}

		public override void LoadState(PluginDataWrapper pluginData)
		{
			var reader = pluginData.GetReaderForWrappedSequentialData();
			isOpen = reader.GetNext<bool>();
		}

		public override void Dispose()
		{
			
		}

		public override void OnHit(IEntity byEntity, object userData)
		{
			
		}

		public override float? GetHeightAt(float x, float y)
		{
			return null;
		}

		public override IFormationController GetFormationController(Vector3 centerPosition)
		{
			return null;
		}
	}
}
