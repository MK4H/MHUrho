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
	public enum PlayerTypeCategory { Human, Neutral, AI };

    public class PlayerType : ILoadableType, IDisposable
    {
		public int ID { get; private set; }

		public string Name { get; private set; }

		public GamePack Package { get; private set; }

		public IntRect IconRectangle { get; private set; }

		public PlayerTypeCategory Category { get; private set; }

		public PlayerAITypePlugin Plugin { get; private set; }

		public void Load(XElement xml, GamePack package)
		{
			//TODO: Check for errors
			ID = XmlHelpers.GetID(xml);
			Category = StringToCategory(xml.Attribute("category").Value);
			Name = XmlHelpers.GetName(xml);
			Package = package;

			XElement pathElement = xml.Element(PlayerAITypeXml.Inst.AssemblyPath);

			Plugin = TypePlugin.LoadTypePlugin<PlayerAITypePlugin>(XmlHelpers.GetPath(pathElement), package, Name);

			IconRectangle = XmlHelpers.GetIconRectangle(xml);

			Plugin.Initialize(XmlHelpers.GetExtensionElement(xml),
							package);
		}

		/// <summary>
		/// Clears any cache state dependent on the current level
		/// </summary>
		public void ClearCache()
		{
			//If you add any cache dependent on current level, clear it here
		}

		public PlayerAIInstancePlugin GetNewInstancePlugin(IPlayer player, ILevelManager level)
		{			
			try
			{
				return Plugin.CreateNewInstance(level, player);
			}
			catch (Exception e) {
				Urho.IO.Log.Write(LogLevel.Error,
								$"Player type plugin call {nameof(Plugin.CreateNewInstance)} failed with Exception: {e.Message}");
				throw;
			}
		}

		public PlayerAIInstancePlugin GetInstancePluginForLoading(IPlayer player, ILevelManager level) {
			try {
				return Plugin.GetInstanceForLoading(level, player);
			}
			catch (Exception e) {
				Urho.IO.Log.Write(LogLevel.Error,
								$"Player type plugin call {nameof(Plugin.GetInstanceForLoading)} failed with Exception: {e.Message}");
				throw;
			}
			
		}

		public void Dispose() {

		}

		static PlayerTypeCategory StringToCategory(string categoryString)
		{
			//Values have to match those in GamePack.xsd playerTypeCategoryType enumeration
			if (string.Equals(categoryString, "human", StringComparison.InvariantCultureIgnoreCase)) {
				return PlayerTypeCategory.Human;
			}
			else if (string.Equals(categoryString, "neutral", StringComparison.InvariantCultureIgnoreCase)) {
				return PlayerTypeCategory.Neutral;
			}
			else if (string.Equals(categoryString, "ai", StringComparison.InvariantCultureIgnoreCase)) {
				return PlayerTypeCategory.AI;
			}
			else {
				throw new ArgumentOutOfRangeException(nameof(categoryString),
													categoryString,
													"Category string value does not match any known categories");
			}
		}
	}
}
