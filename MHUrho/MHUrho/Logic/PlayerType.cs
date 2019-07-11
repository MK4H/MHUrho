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

    public class PlayerType : ILoadableType, IDisposable {
		public static PlayerType Placeholder { get; private set; } = new PlayerType()
																	{
																		ID = 0,
																		Name = "MHUrhoPlaceholder",
																		Package = null,
																		IconRectangle = new IntRect(0,0,0,0),
																		Category = PlayerTypeCategory.Neutral,
																		Plugin = new PlaceholderPlayerPluginType()
																	};

		public int ID { get; private set; }

		public string Name { get; private set; }

		public GamePack Package { get; private set; }

		public IntRect IconRectangle { get; private set; }

		public PlayerTypeCategory Category { get; private set; }

		public PlayerAITypePlugin Plugin { get; private set; }

		public void Load(XElement xml, GamePack package)
		{
			Package = package;

			string path = null;
			XElement extensionElem = null;
			try {
				ID = XmlHelpers.GetID(xml);
				Category = StringToCategory(xml.Attribute("category").Value);
				Name = XmlHelpers.GetName(xml);
				IconRectangle = XmlHelpers.GetIconRectangle(xml);
				path = XmlHelpers.GetPath(xml.Element(PlayerAITypeXml.Inst.AssemblyPath));
				extensionElem = XmlHelpers.GetExtensionElement(xml);
			}
			catch (Exception e) {
				LoadError($"Player type loading failed: Invalid XML of the package {package.Name}", e);
			}

			try {
				Plugin = TypePlugin.LoadTypePlugin<PlayerAITypePlugin>(path, package, Name, ID, extensionElem);
			}
			catch (Exception e) {
				LoadError($"Player type \"{Name}\"[{ID}] loading failed: Plugin loading failed with exception: {e.Message}", e);
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
