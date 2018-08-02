using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using MHUrho.Helpers;
using MHUrho.Packaging;
using MHUrho.Plugins;
using Urho;
using Urho.Resources;
using Urho.Urho2D;

namespace MHUrho.Logic
{
    public class PlayerType : ILoadableType, IDisposable
    {
		public int ID { get; set; }

		public string Name { get; private set; }

		public GamePack Package { get; private set; }

		public IntRect IconRectangle { get; private set; }

		public PlayerAITypePlugin Plugin { get; private set; }

		public void Load(XElement xml, GamePack package)
		{
			ID = XmlHelpers.GetID(xml);
			Name = XmlHelpers.GetName(xml);
			Package = package;

			Plugin = XmlHelpers.LoadTypePlugin<PlayerAITypePlugin>(xml, 
																	package.XmlDirectoryPath, 
																	Name);

			IconRectangle = XmlHelpers.GetIconRectangle(xml);

			Plugin.Initialize(XmlHelpers.GetExtensionElement(xml),
							package.PackageManager);
		}

		public PlayerAIInstancePlugin GetNewInstancePlugin(IPlayer player, ILevelManager level)
		{
			return Plugin.CreateNewInstance(level, player);
		}

		public PlayerAIInstancePlugin GetInstancePluginForLoading(IPlayer player, ILevelManager level) {
			return Plugin.GetInstanceForLoading(level, player);
		}

		public void Dispose() {

		}
	}
}
