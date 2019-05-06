using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
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
		public GateInstance(ILevelManager level, IBuilding building)
			: base(level, building)
		{ }

		public override void SaveState(PluginDataWrapper pluginData)
		{
			throw new NotImplementedException();
		}

		public override void LoadState(PluginDataWrapper pluginData)
		{
			throw new NotImplementedException();
		}

		public override void Dispose()
		{
			throw new NotImplementedException();
		}

		public override void OnHit(IEntity byEntity, object userData)
		{
			throw new NotImplementedException();
		}
	}
}
