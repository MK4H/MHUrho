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

		//XML ELEMENTS AND ATTRIBUTES
		public static readonly XName IDAttributeName = "ID";
		public static readonly XName NameAttributeName = "name";
		public static readonly XName ModelElementName = PackageManager.XMLNamespace + "model";
		public static readonly XName ModelPathElementName = PackageManager.XMLNamespace + "modelPath";
		public static readonly XName ModelScaleElementName = PackageManager.XMLNamespace + "scale";
		public static readonly XName MaterialElementName = PackageManager.XMLNamespace + "material";
		public static readonly XName MaterialPathElementName = PackageManager.XMLNamespace + "materialPath";
		public static readonly XName MaterialListElementName = PackageManager.XMLNamespace + "materialListPath";
		public static readonly XName IconRectangleElementName = PackageManager.XMLNamespace + "iconTextureRectangle";
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

		public static string GetFullPath(XElement element, string pathToPackageXmlDir) {
			return System.IO.Path.Combine(pathToPackageXmlDir,
										 GetPath(element));
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

		public static ModelWrapper GetModel(XElement typeXmlElement) {
			XElement modelElement = typeXmlElement.Element(ModelElementName);

			XElement modelPathElement = modelElement.Element(ModelPathElementName);
			XElement modelScaleElement = modelElement.Element(ModelScaleElementName);

			if (modelScaleElement != null) {
				return new ModelWrapper(PackageManager.Instance.GetModel(GetPath(modelPathElement)),
										GetVector3(modelScaleElement));
			}

			return new ModelWrapper(PackageManager.Instance.GetModel(GetPath(modelPathElement)));
		}

		public static MaterialWrapper GetMaterial(XElement typeXmlElement) {
			var materialElement = typeXmlElement.Element(MaterialElementName);

			XElement materialPathElement = materialElement.Element(MaterialPathElementName);
			XElement materialListPathElement = materialElement.Element(MaterialListElementName);

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
				XElement iconElement = typeXmlElement.Element(IconRectangleElementName);

			return GetIntRect(iconElement);

		}

		public static XElement GetExtensionElement(XElement typeXmlElement) {
			return typeXmlElement.Element(ExtensionElementName);
		}

		public static T LoadTypePlugin<T>(XElement typeXml, string pathToPackageXmlDir, string typeName) where T: TypePlugin {
			if (!System.IO.Path.IsPathRooted(pathToPackageXmlDir)) {
				pathToPackageXmlDir = System.IO.Path.Combine(MyGame.Files.DynamicDirPath, pathToPackageXmlDir);
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
	}
}
