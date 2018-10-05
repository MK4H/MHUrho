using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using MHUrho.Packaging;
using MHUrho.Plugins;
using Urho;
using Urho.Resources;
using Urho.Urho2D;

namespace MHUrho.Helpers
{
	public static class XmlHelpers {

		/// <summary>
		/// Gets ful path from <paramref name="pathToPackageXmlDir"/> and path contained in the child of <paramref name="xmlElement"/> of name <paramref name="childElementName"/>
		/// 
		/// The path in the child must be relative to <paramref name="pathToPackageXmlDir"/>
		/// </summary>
		/// <param name="typeXmlElement">The xml element representing the type</param>
		/// <param name="childElementName">name of the child of the <paramref name="xmlElement"/> which contains the relative path</param>
		/// <param name="pathToPackageXmlDir">the absolute path of the containing xml file</param>
		/// <returns>the full path combined from the <paramref name="pathToPackageXmlDir"/> and the relative path contained in the child <paramref name="childElementName"/> of <paramref name="xmlElement"/></returns>
		public static string GetFullPathFromChild(XElement typeXmlElement, string childElementName,
												string pathToPackageXmlDir) {
			return System.IO.Path.Combine(pathToPackageXmlDir,
										 GetPath(typeXmlElement.Element(PackageManager.XMLNamespace +
																		childElementName)));
		}

		public static string GetFullPath(XElement element, string pathToPackageXmlDir) {
			return System.IO.Path.Combine(pathToPackageXmlDir,
										 GetPath(element));
		}

		public static string GetPath(XElement xmlElement) {
			return FileManager.CorrectRelativePath(xmlElement.Value.Trim());
		}

		public static int GetID(XElement typeXmlElement) {
			return GetIntAttribute(typeXmlElement, EntityXml.Inst.IDAttribute);
		}

		public static string GetName(XElement typeXmlElement) {
			return typeXmlElement.Attribute(EntityXml.Inst.NameAttribute).Value.Trim();
		}

		public static ModelWrapper GetModel(XElement typeXmlElement) {
			XElement modelElement = typeXmlElement.Element(EntityXml.Inst.Model);

			XElement modelPathElement = modelElement.Element(ModelXml.Inst.ModelPath);
			XElement modelScaleElement = modelElement.Element(ModelXml.Inst.Scale);

			if (modelScaleElement != null) {
				return new ModelWrapper(PackageManager.Instance.GetModel(GetPath(modelPathElement)),
										GetVector3(modelScaleElement));
			}

			return new ModelWrapper(PackageManager.Instance.GetModel(GetPath(modelPathElement)));
		}

		public static MaterialWrapper GetMaterial(XElement typeXmlElement) {
			var materialElement = typeXmlElement.Element(EntityXml.Inst.Material);

			XElement materialPathElement = materialElement.Element(MaterialXml.Inst.MaterialPath);
			XElement materialListPathElement = materialElement.Element(MaterialXml.Inst.MaterialListPath);

			if (materialPathElement != null) {
				return new SimpleMaterial(PackageManager.Instance.GetMaterial(GetPath(materialPathElement)));
			}
			else if (materialListPathElement != null) {
				var path = GetPath(materialListPathElement);
				if (!PackageManager.Instance.Exists(path)) {
					throw new FileNotFoundException("Material list file not found",path);
				}
				return new MaterialList(path);
			}
			else {
				throw new InvalidOperationException("Xml Schema validator did not catch a missing choice member");
			}
		}

		public static IntRect GetIconRectangle(XElement typeXmlElement) {
			XElement iconElement = typeXmlElement.Element(EntityWithIconXml.Inst.IconTextureRectangle);

			return GetIntRect(iconElement);

		}

		public static XElement GetExtensionElement(XElement typeXmlElement) {
			return typeXmlElement.Element(EntityXml.Inst.Extension);
		}

		public static bool GetManuallySpawnable(XElement typeXmlElement)
		{
			return GetBool(typeXmlElement.Element(UnitTypeXml.Inst.ManuallySpawnable));
		}


		public static XElement GetChild(XElement ofElement, string childName) {
			return GetChild(ofElement, PackageManager.XMLNamespace + childName);
		}

		public static XElement GetChild(XElement ofElement, XName childName) {
			return ofElement.Element(childName);
		}

		public static int GetInt(XElement element) {
			return int.Parse(element.Value);
		}

		public static int GetIntAttribute(XElement element, string attributeName) {
			return GetIntAttribute(element, (XName)attributeName);
		}

		public static int GetIntAttribute(XElement element, XName attributeName) {
			return int.Parse(element.Attribute(attributeName).Value);
		}

		public static float GetFloatAttribute(XElement element, XName attributeName) {
			return float.Parse(element.Attribute(attributeName).Value);
		}

		public static float GetFloat(XElement element) {
			return float.Parse(element.Value);
		}

		public static IntVector2 GetIntVector2(XElement element) {
			return new IntVector2(	GetIntAttribute(element,"x"), 
									GetIntAttribute(element,"y"));
		}

		public static Vector3 GetVector3(XElement xmlElement) {
			return new Vector3(	GetFloatAttribute(xmlElement, "x"),
								GetFloatAttribute(xmlElement, "y"),
								GetFloatAttribute(xmlElement, "z"));
		}

		public static IntRect GetIntRect(XElement element)
		{
			return new IntRect(GetIntAttribute(element, "left"),
								GetIntAttribute(element, "top"),
								GetIntAttribute(element, "right"),
								GetIntAttribute(element, "bottom"));
		}

		public static string GetString(XElement element) {
			return element.Value.Trim();
		}

		public static Color GetColor(XElement element)
		{
			int R = element.GetIntFromAttribute("R");
			int G = element.GetIntFromAttribute("G");
			int B = element.GetIntFromAttribute("B");
			int A = element.Attribute("A") != null ? element.GetIntFromAttribute("A") : 255;
			//xml schema makes sure it will be of type byte
			return Color.FromByteFormat((byte)R, (byte)G, (byte)B, (byte)A);
		}

		public static bool GetBool(XElement element)
		{
			return bool.Parse(element.Value);
		}

		public static XElement IntVector2ToXmlElement(XName elementName, IntVector2 value)
		{
			return new XElement(elementName, new XAttribute("x", value.X), new XAttribute("y", value.Y));
		}
	}
}
