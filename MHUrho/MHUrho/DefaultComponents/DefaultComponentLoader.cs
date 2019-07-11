using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using MHUrho.Plugins;
using MHUrho.Storage;

namespace MHUrho.DefaultComponents
{
	internal abstract class DefaultComponentLoader : ILoader{
		public abstract DefaultComponent Component { get; }

		public abstract void StartLoading();

		public abstract void ConnectReferences();

		public abstract void FinishLoading();


		public abstract DefaultComponentLoader Clone(LevelManager level, InstancePlugin plugin, StDefaultComponent storedData);
	}
}
