using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using MHUrho.Helpers;
using MHUrho.Helpers.Extensions;
using MHUrho.Storage;
using Urho;
using Urho.Resources;
using MHUrho.Packaging;
using Urho.Urho2D;

namespace MHUrho.Logic
{
	/// <summary>
	/// Represents a tile type loaded from the package.
	/// </summary>
	public class TileType : ILoadableType {

		/// <inheritdoc />
		public int ID { get; private set; }

		/// <inheritdoc />
		public string Name { get; private set; }

		/// <inheritdoc />
		public GamePack Package { get; private set; }

		/// <summary>
		/// Part of the tile texture corresponding to this tile type.
		/// </summary>
		public Rect TextureCoords { get; private set; }

		/// <summary>
		/// Color of this tile type when displayed on the minimap.
		/// </summary>
		public Color MinimapColor { get; private set; }

		/// <summary>
		/// Part of the <see cref="GamePack.TileIconTexture"/> corresponding to this tile type.
		/// </summary>
		public IntRect IconRectangle { get; private set; }

		/// <summary>
		/// Path to the image containing the tile type appearance.
		/// </summary>
		string imagePath;

		/// <summary>
		/// Loads the tile type data from Xml element.
		/// </summary>
		/// <param name="xml">The xml element containing the data of this tile type</param>
		/// <param name="package">The source package of the Xml.</param>
		public void Load(XElement xml, GamePack package) {
			Package = package;
			try {
				ID = XmlHelpers.GetID(xml);
				Name = XmlHelpers.GetName(xml);
				imagePath = XmlHelpers.GetPath(xml.Element(TileTypeXml.Inst.TexturePath));
				IconRectangle = XmlHelpers.GetIntRect(xml.Element(TileTypeXml.Inst.IconTextureRectangle));
				MinimapColor = XmlHelpers.GetColor(xml.Element(TileTypeXml.Inst.MinimapColor));
			}
			catch (Exception e) {
				string message = $"Tile type loading failed: Invalid XML of the package {package.Name}";
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


		public TileType() {
			ID = 0;
		}

		/// <summary>
		/// Called by map, after constructiong the one big texture out of all the tileType images
		/// </summary>
		/// <param name="coords">Coords of the image in the map texture</param>
		public void SetTextureCoords(Rect coords) {
			TextureCoords = coords;
		}

		/// <summary>
		/// Gets the image containing the appearance of this tile type.
		/// </summary>
		/// <returns>The image containing the appearance of this tile type.</returns>
		public Image GetImage() {
			return Package.PackageManager.GetImage(imagePath, true);
		}


	}
}