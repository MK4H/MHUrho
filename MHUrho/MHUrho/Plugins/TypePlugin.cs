using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml.Linq;
using MHUrho.Packaging;
using Urho;

namespace MHUrho.Plugins
{
	public abstract class TypePlugin {
		public abstract string Name { get; }

		/// <summary>
		/// Called to initialize the instance
		/// </summary>
		/// <param name="extensionElement">extension element of the unitType xml description or null if there is none</param>
		/// <param name="package">TODO: comment</param>
		public abstract void Initialize(XElement extensionElement, GamePack package);

		public static T LoadTypePlugin<T>(string relativeAssemblyPath, GamePack package, string typeName)
			where T : TypePlugin
		{
			string absoluteAssemblyPath = Path.Combine(package.RootedDirectoryPath, relativeAssemblyPath);

			var assembly = Assembly.LoadFile(absoluteAssemblyPath);
			T pluginInstance = null;
			try
			{
				var unitPlugins = from type in assembly.GetTypes()
								where typeof(T).IsAssignableFrom(type)
								select type;

				foreach (var plugin in unitPlugins)
				{
					if (plugin.IsAbstract || !plugin.IsPublic)
					{
						continue;
					}

					var newPluginInstance = (T)Activator.CreateInstance(plugin);
					if (newPluginInstance.Name == typeName)
					{
						pluginInstance = newPluginInstance;
						break;
					}
				}
			}
			catch (ReflectionTypeLoadException e)
			{
				Urho.IO.Log.Write(LogLevel.Error, $"Could not get types from the assembly {assembly}");
				//TODO: Exception
				throw new Exception("Type plugin loading failed, could not load plugin", e);
			}

			if (pluginInstance == null)
			{
				//TODO: Exception
				throw new Exception($"Type plugin loading failed, could not load plugin for type {typeName}");
			}

			return pluginInstance;
		}
	}
}
