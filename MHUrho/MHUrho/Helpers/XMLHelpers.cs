using System;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using MHUrho.Packaging;
using MHUrho.Plugins;
using Urho;
using Urho.Resources;

namespace MHUrho.Helpers
{
	public static class XmlHelpers {

		//XML ELEMENTS AND ATTRIBUTES
		public static readonly XName IDAttributeName = "ID";
		public static readonly XName NameAttributeName = "name";
		public static readonly XName ModelPathElementName = PackageManager.XMLNamespace + "modelPath";
		public static readonly XName MaterialElementName = PackageManager.XMLNamespace + "material";
		public static readonly XName MaterialPathElementName = PackageManager.XMLNamespace + "materialPath";
		public static readonly XName MaterialListElementName = PackageManager.XMLNamespace + "materialListPath";
		public static readonly XName IconPathElementName = PackageManager.XMLNamespace + "iconPath";
		public static readonly XName AssemblyPathElementName = PackageManager.XMLNamespace + "assemblyPath";
		public static readonly XName ExtensionElementName = PackageManager.XMLNamespace + "extension";

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

		public static string GetFullPath(XElement xmlElement, string pathToPackageXmlDir) {
			return System.IO.Path.Combine(pathToPackageXmlDir,
										 GetPath(xmlElement));
		}

		public static string GetPath(XElement xmlElement) {
			return FileManager.CorrectRelativePath(xmlElement.Value.Trim());
		}

		public static int GetID(XElement typeXmlElement) {
			return GetIntAttribute(typeXmlElement, IDAttributeName);
		}

		public static string GetName(XElement typeXmlElement) {
			return typeXmlElement.Attribute(NameAttributeName).Value.Trim();
		}

		public static Model GetModel(XElement typeXmlElement) {
			XElement modelElement = typeXmlElement.Element(ModelPathElementName);

			return PackageManager.Instance.ResourceCache.GetModel(GetPath(modelElement));
		}

		public static MaterialWrapper GetMaterial(XElement typeXmlElement) {
			var materialElement = typeXmlElement.Element(MaterialElementName);

			XElement materialPathElement = materialElement.Element(MaterialPathElementName);
			XElement materialListPathElement = materialElement.Element(MaterialListElementName);

			if (materialPathElement != null) {
				return new SimpleMaterial(PackageManager.Instance.ResourceCache.GetMaterial(GetPath(materialPathElement)));
			}
			else if (materialListPathElement != null) {
				return new MaterialList(GetPath(materialListPathElement));
			}
			else {
				throw new InvalidOperationException("Xml Schema validator did not catch a missing choice member");
			}
		}

		public static Image GetIcon(XElement typeXmlElement) {
			XElement iconElement = typeXmlElement.Element(IconPathElementName);

			//TODO: Find a way to not need RGBA conversion
			return PackageManager.Instance.ResourceCache.GetImage(GetPath(iconElement)).ConvertToRGBA();
		}

		public static XElement GetExtensionElement(XElement typeXmlElement) {
			return typeXmlElement.Element(ExtensionElementName);
		}

		public static T LoadTypePlugin<T>(XElement typeXml, string pathToPackageXmlDir, string typeName) where T: TypePluginBase {
			if (!System.IO.Path.IsPathRooted(pathToPackageXmlDir)) {
				pathToPackageXmlDir = System.IO.Path.Combine(MyGame.Config.DynamicDirPath, pathToPackageXmlDir);
			}

			XElement assemblyPathElement = typeXml.Element(AssemblyPathElementName);

			string assemblyPath = GetFullPath(assemblyPathElement, pathToPackageXmlDir);

			var assembly = Assembly.LoadFile(assemblyPath);
			T pluginInstance = null;
			try {
				var unitPlugins = from type in assembly.GetTypes()
								  where typeof(T).IsAssignableFrom(type)
								  select type;

				foreach (var plugin in unitPlugins) {
					var newPluginInstance = (T)Activator.CreateInstance(plugin);
					if (newPluginInstance.IsMyType(typeName)) {
						pluginInstance = newPluginInstance;
						break;
					}
				}
			}
			catch (ReflectionTypeLoadException e) {
				Urho.IO.Log.Write(LogLevel.Error, $"Could not get types from the assembly {assembly}");
				//TODO: Exception
				throw new Exception("Type plugin loading failed, could not load plugin",e);
			}

			if (pluginInstance == null) {
				//TODO: Exception
				throw new Exception($"Type plugin loading failed, could not load plugin for type {typeName}");
			}

			return pluginInstance;
		}


		public static int GetInt(XElement typeXmlElement, string childElementName) {
			return int.Parse(typeXmlElement.Element(PackageManager.XMLNamespace + childElementName).Value);
		}

		public static int GetIntAttribute(XElement element, string attributeName) {
			return GetIntAttribute(element, (XName)attributeName);
		}

		public static int GetIntAttribute(XElement element, XName attributeName) {
			return int.Parse(element.Attribute(attributeName).Value);
		}

		public static float GetFloat(XElement typeXmlElement, string childElementName) {
			return float.Parse(typeXmlElement.Element(PackageManager.XMLNamespace + childElementName).Value);
		}

		public static IntVector2 GetIntVector2(XElement typeXmlElement, string childElementName) {
			var vectorElement = typeXmlElement.Element(PackageManager.XMLNamespace + childElementName);
			int x = int.Parse(vectorElement.Attribute("x").Value);
			int y = int.Parse(vectorElement.Attribute("y").Value);
			return new IntVector2(x, y);
		}

		public static string GetString(XElement typeXmlElement, string childElementName) {
			return typeXmlElement.Element(PackageManager.XMLNamespace + childElementName).Value.Trim();
		}

	}
}
