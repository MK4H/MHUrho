﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using MHUrho.EditorTools;
using MHUrho.Input;
using MHUrho.Logic;
using MHUrho.Storage;
using Urho;
using Urho.Gui;

namespace MHUrho.Plugins
{
    public abstract class LevelLogicPlugin : IDisposable {
		public abstract int NumberOfPlayers { get; }

		public abstract string Name { get; }

		public static LevelLogicPlugin Load(string fullAssemblyPath, string name)
		{
			if (!System.IO.Path.IsPathRooted(fullAssemblyPath)) {
				throw new ArgumentException("Provided path was not ful path", nameof(fullAssemblyPath));
			}


			var assembly = Assembly.LoadFile(fullAssemblyPath);
			LevelLogicPlugin pluginInstance = null;
			try {
				var levelPlugins = from type in assembly.GetTypes()
								where typeof(LevelLogicPlugin).IsAssignableFrom(type)
								select type;

				foreach (var plugin in levelPlugins) {
					if (plugin.IsAbstract || !plugin.IsPublic) {
						continue;
					}

					LevelLogicPlugin newPluginInstance = (LevelLogicPlugin)Activator.CreateInstance(plugin);
					if (newPluginInstance.Name == name) {
						pluginInstance = newPluginInstance;
						break;
					}
				}
			}
			catch (ReflectionTypeLoadException e) {
				Urho.IO.Log.Write(LogLevel.Error, $"Could not get types from the assembly {assembly}");
				//TODO: Exception
				throw new Exception("Type plugin loading failed, could not load plugin", e);
			}

			if (pluginInstance == null) {
				//TODO: Exception
				throw new Exception($"Plugin loading failed, could not load plugin for level {name}");
			}

			return pluginInstance;
		}

		public abstract void OnUpdate(float timeStep);

		public abstract void LoadState(PluginDataWrapper fromPluginData);

		public abstract void SaveState(PluginDataWrapper toPluginData);

		public abstract void GetCustomSettings(Window customSettingsWindow);

		public virtual void OnLoad(ILevelManager levelManager)
		{

		}

		public virtual void OnStart(ILevelManager levelManager)
		{

		}

		public abstract ToolManager GetToolManager(ILevelManager levelManager, InputType inputType);

		public abstract void Dispose();
	}
}
