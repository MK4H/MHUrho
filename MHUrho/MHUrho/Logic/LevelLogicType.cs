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
	public class LevelLogicType : ILoadableType, IDisposable
	{
		public int ID { get; set; }
		public string Name { get; private set; }

		public int MaxNumberOfPlayers => Plugin.MaxNumberOfPlayers;

		public int MinNumberOfPlayers => Plugin.MinNumberOfPlayers;

		public GamePack Package { get; private set; }

		public LevelLogicTypePlugin Plugin { get; private set; }

		public LevelLogicType()
		{

		}

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

		public override bool Equals(object obj)
		{
			return object.ReferenceEquals(this, obj);
		}

		public override int GetHashCode()
		{
			return ID;
		}

		public void ClearCache()
		{
			
		}

		public LevelLogicCustomSettings GetCustomSettings(Window customSettingsWindow, MHUrhoApp game)
		{
			return Plugin.GetCustomSettings(customSettingsWindow, game);
		}

		public LevelLogicInstancePlugin CreateInstancePluginForBrandNewLevel(ILevelManager level)
		{
			return Plugin.CreateInstanceForNewLevel(level);
		}

		public LevelLogicInstancePlugin CreateInstancePluginForNewPlaying(LevelLogicCustomSettings levelSettings, ILevelManager level)
		{
			return Plugin.CreateInstanceForNewPlaying(levelSettings, level);
		}

		public LevelLogicInstancePlugin CreateInstancePluginForEditorLoading(ILevelManager level)
		{
			return Plugin.CreateInstanceForEditorLoading(level);
		}

		public LevelLogicInstancePlugin CreateInstancePluginForLoadingToPlaying(ILevelManager level)
		{
			return Plugin.CreateInstanceForLoadingToPlaying(level);
		}

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
