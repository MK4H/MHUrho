using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using MHUrho.Helpers;
using MHUrho.Packaging;
using MHUrho.Plugins;
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
			ID = XmlHelpers.GetID(xml);
			Name = XmlHelpers.GetName(xml);
			Package = package;

			XElement assemblyPathElement = xml.Element(LevelLogicTypeXml.Inst.AssemblyPath);
			Plugin = TypePlugin.LoadTypePlugin<LevelLogicTypePlugin>(XmlHelpers.GetPath(assemblyPathElement),
																	package,
																	Name);

			Plugin.Initialize(xml.Element(LevelLogicTypeXml.Inst.Extension), package);
		}

		public void ClearCache()
		{
			
		}

		public LevelLogicCustomSettings GetCustomSettings(Window customSettingsWindow)
		{
			return Plugin.GetCustomSettings(customSettingsWindow);
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
	}
}
