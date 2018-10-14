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


		public static string GetPath(XElement xmlElement) {
			return FileManager.CorrectRelativePath(xmlElement.Value.Trim());
		}

		public static int GetID(XElement typeXmlElement) {
			return GetIntAttribute(typeXmlElement, EntityXml.Inst.IDAttribute);
		}

		public static string GetName(XElement typeXmlElement) {
			return typeXmlElement.Attribute(EntityXml.Inst.NameAttribute).Value.Trim();
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

		public static uint GetUInt(XElement element)
		{
			return uint.Parse(element.Value);
		}

		public static IntVector2 GetIntVector2(XElement element) {
			return new IntVector2(	GetIntAttribute(element,Vector2Xml.Inst.XAttribute), 
									GetIntAttribute(element,Vector2Xml.Inst.YAttribute));
		}

		public static Vector3 GetVector3(XElement xmlElement) {
			return new Vector3(	GetFloatAttribute(xmlElement, Vector3Xml.Inst.XAttribute),
								GetFloatAttribute(xmlElement, Vector3Xml.Inst.YAttribute),
								GetFloatAttribute(xmlElement, Vector3Xml.Inst.ZAttribute));
		}

		public static IntRect GetIntRect(XElement element)
		{
			return new IntRect(GetIntAttribute(element, IntRectXml.Inst.LeftAttribute),
								GetIntAttribute(element, IntRectXml.Inst.TopAttribute),
								GetIntAttribute(element, IntRectXml.Inst.RightAttribute),
								GetIntAttribute(element, IntRectXml.Inst.BottomAttribute));
		}

		public static string GetString(XElement element) {
			return element.Value.Trim();
		}

		public static Quaternion GetQuaternion(XElement element)
		{
			return new Quaternion(GetFloatAttribute(element, QuaternionXml.Inst.XAngleAttribute),
								GetFloatAttribute(element, QuaternionXml.Inst.YAngleAttribute),
								GetFloatAttribute(element, QuaternionXml.Inst.ZAngleAttribute));
		}

		public static Color GetColor(XElement element)
		{
			int R = element.GetIntFromAttribute(ColorXml.Inst.RAttribute);
			int G = element.GetIntFromAttribute(ColorXml.Inst.GAttribute);
			int B = element.GetIntFromAttribute(ColorXml.Inst.BAttribute);
			int A = element.Attribute(ColorXml.Inst.AAttribute) != null ? element.GetIntFromAttribute(ColorXml.Inst.AAttribute) : 255;
			//xml schema makes sure it will be of type byte
			return Color.FromByteFormat((byte)R, (byte)G, (byte)B, (byte)A);
		}

		public static bool GetBool(XElement element)
		{
			return bool.Parse(element.Value);
		}

		public static XElement IntVector2ToXmlElement(XName elementName, IntVector2 value)
		{
			return new XElement(elementName, new XAttribute(IntVector2Xml.Inst.XAttribute, value.X), new XAttribute(IntVector2Xml.Inst.YAttribute, value.Y));
		}
	}
}
