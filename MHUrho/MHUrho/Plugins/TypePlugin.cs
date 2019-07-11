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
	public abstract class TypePlugin : IDisposable {
		public abstract string Name { get; }
		public abstract int ID { get; }

	
		/// <summary>
		/// Loads plugin from assembly at the <paramref name="relativeAssemblyPath"/> inheriting from type <typeparamref name="T"/>, with name <paramref name="typeName"/> and ID <paramref name="ID"/>,
		/// and initializes this type using <paramref name="extensionElement"/>.
		/// </summary>
		/// <typeparam name="T">Type the plugin should inherit from.</typeparam>
		/// <param name="relativeAssemblyPath">Path to the assembly relative to the package.</param>
		/// <param name="package">Package from which we are loading the type.</param>
		/// <param name="typeName">Name of the type this plugin is for.</param>
		/// <param name="ID">ID of the type this plugin is for.</param>
		/// <param name="extensionElement">Extension element containing user data to initialize the plugin.</param>
		/// <returns>Loaded and initialized plugin.</returns>
		/// <exception cref="PackageLoadingException">Thrown when we were unable to load or initialize the plugin.</exception>
		public static T LoadTypePlugin<T>(string relativeAssemblyPath, GamePack package, string typeName, int ID, XElement extensionElement)
			where T : TypePlugin
		{
			string absoluteAssemblyPath = Path.Combine(package.RootedDirectoryPath, relativeAssemblyPath);

			var assembly = Assembly.LoadFrom(absoluteAssemblyPath);

			T pluginInstance = null;
			try
			{
				var plugins = from type in assembly.GetTypes()
								where !type.IsAbstract && type.IsPublic && typeof(T).IsAssignableFrom(type)
								select type;

				foreach (var plugin in plugins)
				{
					var newPluginInstance = (T)Activator.CreateInstance(plugin);
					if (newPluginInstance.Name == typeName && newPluginInstance.ID == ID)
					{
						pluginInstance = newPluginInstance;
						break;
					}
				}
			}
			catch (ReflectionTypeLoadException e) {
				string message = $"Could not get types from the assembly {assembly}";
				Urho.IO.Log.Write(LogLevel.Error, message);
				throw new PackageLoadingException(message, e);
			}

			if (pluginInstance == null)
			{
				throw new PackageLoadingException($"Type plugin loading failed, could not find plugin for type {typeName} [{ID}]");
			}

			try {
				pluginInstance.Initialize(extensionElement, package);
			}
			catch (Exception e) {
				string message = $"Failed to initialize plugin for type {typeName}[{ID}], exception: {e.Message}";
				Urho.IO.Log.Write(LogLevel.Error, message);
				throw new PackageLoadingException(message, e);
			}

			return pluginInstance;
		}

		public virtual void Dispose()
		{

		}

		/// <summary>
		/// Called to initialize the instance
		/// </summary>
		/// <param name="extensionElement">extension element of the unitType xml description or null if there is none</param>
		/// <param name="package"></param>
		protected abstract void Initialize(XElement extensionElement, GamePack package);
	}
}
