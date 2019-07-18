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
	/// <summary>
	/// Base class for all level logic instance plugins.
	/// </summary>
    public abstract class LevelLogicInstancePlugin : InstancePlugin {

		protected LevelLogicInstancePlugin(ILevelManager level)
			:base(level)
		{

		}

		/// <summary>
		/// Invoked just before the level stared, after everything is loaded and initialized.
		/// </summary>
		public virtual void OnStart()
		{

		}

		/// <summary>
		/// Called after all platform instances of game objects are loaded.
		/// Equivalent to LoadState call, but on a level creation instead of loading.
		/// </summary>
		public abstract void Initialize();

		/// <summary>
		/// Gets the factory for creating the pathFinding algorithm for the level.
		/// </summary>
		/// <returns>The factory for creating the pathFinding algorithm.</returns>
		public abstract IPathFindAlgFactory GetPathFindAlgFactory();

		/// <summary>
		/// Gets the tool manager which is responsible for managing tools.
		/// This tool manager decides which tools will be accessible to the user.
		/// </summary>
		/// <param name="levelManager">The level.</param>
		/// <param name="inputType">Input schema.</param>
		/// <returns>Tool manager for the current level.</returns>
		public abstract ToolManager GetToolManager(ILevelManager levelManager, InputType inputType);
	}
}
