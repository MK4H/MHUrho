using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using Urho;
using MHUrho.Storage;

namespace MHUrho.UnitComponents
{
	public enum DefaultComponents {
		Shooter,
		Meele,
		ResourceCarrier,
		UnitSelector,
		WallClimber,
		WorkQueue,
		WorldWalker,
		UnpoweredFlier,
		StaticRangeTarget,
		MovingRangeTarget,
		Clicker
	}

	public abstract class DefaultComponent : Component {

		public new abstract DefaultComponents ComponentTypeID{ get; }

		public abstract string ComponentTypeName { get; }

		internal abstract void ConnectReferences(ILevelManager level);

		public abstract PluginData SaveState();
	}
}
