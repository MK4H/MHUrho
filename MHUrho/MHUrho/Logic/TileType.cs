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
	public class TileType : ILoadableType {

		public int ID { get; private set; }

		public string Name { get; private set; }

		public GamePack Package { get; private set; }

		public Rect TextureCoords { get; private set; }

		public Color MinimapColor { get; private set; }

		public IntRect IconRectangle { get; private set; }

		string imagePath;

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

		public Image GetImage() {
			return Package.PackageManager.GetImage(imagePath, true);
		}


	}
}