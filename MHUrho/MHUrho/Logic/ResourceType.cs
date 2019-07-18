using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using MHUrho.Helpers;
using MHUrho.Helpers.Extensions;
using MHUrho.Packaging;
using MHUrho.Storage;
using Urho;
using Urho.Resources;

namespace MHUrho.Logic
{
	/// <summary>
	/// Represents a resource type loaded from package.
	/// Used mainly to identify values that belong to this resource type.
	/// </summary>
	public class ResourceType : ILoadableType {

		/// <inheritdoc />
		public int ID { get; private set; }

		/// <inheritdoc />
		public string Name { get; private set; }

		/// <inheritdoc />
		public GamePack Package { get; private set; }

		/// <summary>
		/// Part of the <see cref="GamePack.ResourceIconTexture"/> representing this type of players.
		/// </summary>
		public IntRect IconRectangle { get; private set; }

		/// <summary>
		/// Loads data of this resource type from the xml element from the package.
		/// </summary>
		/// <param name="xml">The xml element of the resource type.</param>
		/// <param name="package">The source package of the xml.</param>
		public void Load(XElement xml, GamePack package)
		{
			Package = package;
			try {
				ID = XmlHelpers.GetID(xml);
				Name = XmlHelpers.GetName(xml);
				IconRectangle = XmlHelpers.GetIconRectangle(xml);
			}
			catch (Exception e)
			{
				string message = $"Resource type loading failed: Invalid XML of the package {package.Name}";
				Urho.IO.Log.Write(LogLevel.Error, message);
				throw new PackageLoadingException(message, e);
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

	}
}
