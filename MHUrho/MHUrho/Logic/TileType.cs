using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Xml.Linq;
using MHUrho.Helpers;
using MHUrho.Storage;
using Urho;
using Urho.Resources;
using MHUrho.Packaging;
using Urho.Urho2D;

namespace MHUrho.Logic
{
	public class TileType : ILoadableType {
		static readonly string IDAttributeName = "ID";
		static readonly string NameAttributeName = "name";
		static readonly XName TexturePathElementName = PackageManager.XMLNamespace + "texturePath";
		static readonly XName IconTextureElementName = PackageManager.XMLNamespace + "iconTextureRectangle";
		static readonly XName MinimapColorElement = PackageManager.XMLNamespace + "minimapColor";

		public int ID { get; set; }

		public string Name { get; private set; }

		public GamePack Package { get; private set; }

		//TODO: Check that texture is null
		public Rect TextureCoords { get; private set; }

		public Color MinimapColor { get; private set; }

		public IntRect IconRectangle { get; private set; }

		public bool IsManuallySpawnable { get; private set; }

		string imagePath;

		public static string GetNameFromXml(XElement tileTypeElement) {
			return tileTypeElement.Attribute("name").Value;
		}

		public void Load(XElement xml, GamePack package) {
			//TODO: Check for errors
			ID = xml.GetIntFromAttribute(IDAttributeName);
			Name = xml.Attribute(NameAttributeName).Value;
			imagePath = XmlHelpers.GetPath(xml.Element(TexturePathElementName));
			IconRectangle = XmlHelpers.GetIntRect(xml.Element(IconTextureElementName));
			MinimapColor = XmlHelpers.GetColor(xml.Element(MinimapColorElement));
			IsManuallySpawnable = XmlHelpers.GetManuallySpawnable(xml);
			Package = package;
		}

		public void ClearCache()
		{
			//TODO: If i add any cache dependent on current level, clear it here
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
			return PackageManager.Instance.GetImage(imagePath);
		}


	}
}