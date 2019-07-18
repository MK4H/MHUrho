using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using MHUrho.Helpers;
using MHUrho.Packaging;
using MHUrho.Plugins;
using Urho;
using Urho.Gui;

namespace MHUrho.Logic {
	/// <summary>
	/// Type of level logic loaded from package.
	/// </summary>
	public class LevelLogicType : ILoadableType, IDisposable
	{
		/// <inheritdoc/>
		public int ID { get; set; }

		/// <inheritdoc/>
		public string Name { get; private set; }

		/// <summary>
		/// Maximum number of players this level supports. 
		/// </summary>
		public int MaxNumberOfPlayers => Plugin.MaxNumberOfPlayers;

		/// <summary>
		/// Minimum number of players this level can be played with.
		/// </summary>
		public int MinNumberOfPlayers => Plugin.MinNumberOfPlayers;

		/// <inheritdoc/>
		public GamePack Package { get; private set; }

		/// <summary>
		/// The type plugin of this level logic type.
		/// </summary>
		public LevelLogicTypePlugin Plugin { get; private set; }

		public LevelLogicType()
		{

		}

		/// <summary>
		/// Loads the level logic type from the package Xml.
		/// </summary>
		/// <param name="xml">Xml element holding the data for this level logic type.</param>
		/// <param name="package">The source package of the Xml.</param>
		public void Load(XElement xml, GamePack package)
		{
			Package = package;

			string assemblyPath = null;
			XElement extensionElem = null;
			try {
				ID = XmlHelpers.GetID(xml);
				Name = XmlHelpers.GetName(xml);
				assemblyPath = XmlHelpers.GetPath(xml.Element(LevelLogicTypeXml.Inst.AssemblyPath));
				extensionElem = xml.Element(LevelLogicTypeXml.Inst.Extension);
			}
			catch (Exception e) {
				LoadError($"Level logic type loading failed: Invalid XML of the package {package.Name}", e);
			}

			try {
				Plugin = TypePlugin.LoadTypePlugin<LevelLogicTypePlugin>(assemblyPath,
																		package,
																		Name,
																		 ID,
																		 extensionElem);
			}
			catch (Exception e) {
				LoadError($"Level logic type \"{Name}\"[{ID}] loading failed: Plugin loading failed with exception: {e.Message}", e);
			}	
		}

		/// <summary>
		/// Compares level logic types for equality.
		/// </summary>
		/// <param name="obj">The compared object.</param>
		/// <returns>True if the <paramref name="obj"/> is the same level logic type.</returns>
		public override bool Equals(object obj)
		{
			return object.ReferenceEquals(this, obj);
		}

		/// <summary>
		/// Returns hashcode to enable the use of this class as keys in dictionaries.
		/// </summary>
		/// <returns>Hashcode of this instance.</returns>
		public override int GetHashCode()
		{
			return ID;
		}

		/// <summary>
		/// Clears any level specific data.
		/// </summary>
		public void ClearCache()
		{
			
		}

		/// <summary>
		/// Returns plugin custom settings, encapsulating the GUI given to the plugin to place it's own elements in.
		/// </summary>
		/// <param name="customSettingsWindow">The part of the GUI given to the plugin.</param>
		/// <param name="game">Th instance representing the app.</param>
		/// <returns>Instance encapsulating the access to the GUI for the plugin.</returns>
		public LevelLogicCustomSettings GetCustomSettings(Window customSettingsWindow, MHUrhoApp game)
		{
			return Plugin.GetCustomSettings(customSettingsWindow, game);
		}

		/// <summary>
		/// Creates level logic instance plugin for a level that is newly generated.
		/// </summary>
		/// <param name="level">The newly generated level.</param>
		/// <returns>Instance plugin for a newly generated level.</returns>
		public LevelLogicInstancePlugin CreateInstancePluginForBrandNewLevel(ILevelManager level)
		{
			return Plugin.CreateInstanceForNewLevel(level);
		}

		/// <summary>
		/// Gets level logic instance plugin for level that is loaded for playing from a level prototype,
		/// so it has never been loaded for playing before.
		/// </summary>
		/// <param name="levelSettings">The GUI encapsulation given to the plugin so it can get input from user.</param>
		/// <param name="level">The loaded level.</param>
		/// <returns>Instance plugin for level loaded for playing for the first time.</returns>
		public LevelLogicInstancePlugin CreateInstancePluginForNewPlaying(LevelLogicCustomSettings levelSettings, ILevelManager level)
		{
			return Plugin.CreateInstanceForNewPlaying(levelSettings, level);
		}

		/// <summary>
		/// Gets level logic instance plugin for existing level that is loaded for editing.
		/// </summary>
		/// <param name="level">The level that will be controlled by the plugin.</param>
		/// <returns>Instance plugin for the level loaded for editing.</returns>
		public LevelLogicInstancePlugin CreateInstancePluginForEditorLoading(ILevelManager level)
		{
			return Plugin.CreateInstanceForEditorLoading(level);
		}

		/// <summary>
		/// Gets level logic instance plugin for existing level that was saved during playing and so can only be loaded for playing.
		/// Already has stored state.
		/// </summary>
		/// <param name="level">The level that will be controlled by the plugin.</param>
		/// <returns>Instance plugin for the level loaded for playing.</returns>
		public LevelLogicInstancePlugin CreateInstancePluginForLoadingToPlaying(ILevelManager level)
		{
			return Plugin.CreateInstanceForLoadingToPlaying(level);
		}

		/// <summary>
		/// Releases the plugin.
		/// </summary>
		public void Dispose()
		{
			Plugin.Dispose();
		}

		/// <summary>
		/// Logs message and throws a <see cref="PackageLoadingException"/>
		/// </summary>
		/// <param name="message">Message to log and propagate via exception</param>
		/// <exception cref="PackageLoadingException">Always throws this exception</exception>
		void LoadError(string message, Exception e)
		{
			Urho.IO.Log.Write(LogLevel.Error, message);
			throw new PackageLoadingException(message, e);
		}
	}
}
