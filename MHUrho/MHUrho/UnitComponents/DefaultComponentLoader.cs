using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.Plugins;
using MHUrho.Storage;

namespace MHUrho.UnitComponents
{
	internal abstract class DefaultComponentLoader : ILoader{
		public abstract DefaultComponent Component { get; }

		public abstract void StartLoading(LevelManager level, InstancePlugin plugin, PluginData storedData);

		public abstract void ConnectReferences(LevelManager level);

		public abstract void FinishLoading();


		public abstract DefaultComponentLoader Clone();
	}
}
