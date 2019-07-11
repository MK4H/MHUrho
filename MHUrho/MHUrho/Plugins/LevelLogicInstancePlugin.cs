using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using MHUrho.EditorTools;
using MHUrho.Input;
using MHUrho.Logic;
using MHUrho.PathFinding;
using MHUrho.Storage;
using Urho;
using Urho.Gui;

namespace MHUrho.Plugins
{
    public abstract class LevelLogicInstancePlugin : InstancePlugin {

		protected LevelLogicInstancePlugin(ILevelManager level)
			:base(level)
		{

		}

		public virtual void OnStart()
		{

		}

		/// <summary>
		/// Called after all platform instances of game objects are loaded.
		/// Equivalent to LoadState call, but on a level creation instead of loading.
		/// </summary>
		public abstract void Initialize();

		public abstract IPathFindAlgFactory GetPathFindAlgFactory();

		public abstract ToolManager GetToolManager(ILevelManager levelManager, InputType inputType);
	}
}
