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
        public static int GetIntFromChild(this XElement xElement, string childElementName) {
            return XmlHelpers.GetInt(xElement, childElementName);
        }

        public static int GetIntFromAttribute(this XElement xElement, string attributeName) {
            return XmlHelpers.GetIntAttribute(xElement, attributeName);
        }

        public static float GetFloatFromChild(this XElement xElement, string childElementName) {
            return XmlHelpers.GetFloat(xElement, childElementName);
        }

        public static IntVector2 GetIntVector2FromChild(XElement xElement, string childElementName) {
            return XmlHelpers.GetIntVector2(xElement, childElementName);
        }

        public static string GetStringFromChild(this XElement xElement, string childElementName) {
            return XmlHelpers.GetString(xElement, childElementName);
        }

    }
}
