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

namespace MHUrho.Logic
{
	public class TileType : ILoadableType {
		private static readonly string IDAttributeName = "ID";
		private static readonly string NameAttributeName = "name";
		private static readonly XName TexturePathElementName = PackageManager.XMLNamespace + "texturePath";
		private static readonly XName MovementSpeedElementName = PackageManager.XMLNamespace + "movementSpeed";

		public int ID { get; set; }

		public float MovementSpeedModifier { get; private set; }

		public string Name { get; private set; }

		public GamePack Package { get; private set; }

		//TODO: Check that texture is null
		public Rect TextureCoords { get; private set; }

		private string imagePath;

		public static string GetNameFromXml(XElement tileTypeElement) {
			return tileTypeElement.Attribute("name").Value;
		}

		public void Load(XElement xml, GamePack package) {
			//TODO: Check for errors
			ID = xml.GetIntFromAttribute(IDAttributeName);
			Name = xml.Attribute(NameAttributeName).Value;
			imagePath = XmlHelpers.GetPath(xml.Element(TexturePathElementName));
			MovementSpeedModifier = XmlHelpers.GetFloat(xml.Element(MovementSpeedElementName));
			Package = package;
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
			return PackageManager.Instance.ResourceCache.GetImage(imagePath);
		}


	}
}