using System;
using System.Collections.Generic;
using System.Text;
using System.Xml.Linq;
using MHUrho.Packaging;
using Urho;

namespace MHUrho.Helpers
{
	public static class XElementExtensions
	{
		public static int GetIntFromChild(this XElement xElement) {
			return XmlHelpers.GetInt(xElement);
		}

		public static int GetIntFromAttribute(this XElement xElement, XName attributeName) {
			return XmlHelpers.GetIntAttribute(xElement, attributeName);
		}

		public static float GetFloat(this XElement xElement) {
			return XmlHelpers.GetFloat(xElement);
		}

		public static uint GetUInt(this XElement xElement)
		{
			return XmlHelpers.GetUInt(xElement);
		}

		public static IntVector2 GetIntVector2(this XElement xElement) {
			return XmlHelpers.GetIntVector2(xElement);
		}

		public static Vector3 GetVector3(this XElement xElement)
		{
			return XmlHelpers.GetVector3(xElement);
		}

		public static Quaternion GetQuaternion(this XElement xElement)
		{
			return XmlHelpers.GetQuaternion(xElement);
		}

		public static string GetString(this XElement xElement) {
			return XmlHelpers.GetString(xElement);
		}

		public static string GetPath(this XElement xElement)
		{
			return XmlHelpers.GetPath(xElement);
		}
	}
}
