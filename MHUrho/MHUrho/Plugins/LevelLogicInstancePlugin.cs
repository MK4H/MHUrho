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

		public virtual void OnLoad(ILevelManager levelManager)
		{

		}

		public virtual void OnStart(ILevelManager levelManager)
		{

		}

		public abstract IPathFindAlgFactory GetPathFindAlgFactory();

		public abstract ToolManager GetToolManager(ILevelManager levelManager, InputType inputType);
	}
}
