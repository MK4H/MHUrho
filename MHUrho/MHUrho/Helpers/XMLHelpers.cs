using System;
using System.Linq;
using System.Reflection;
using System.Xml.Linq;
using MHUrho.Packaging;
using MHUrho.Plugins;
using Urho;

namespace MHUrho.Helpers
{
    public static class XmlHelpers
    {
        /// <summary>
        /// Gets ful path from <paramref name="pathToPackageXmlDir"/> and path contained in the child of <paramref name="xmlElement"/> of name <paramref name="childElementName"/>
        /// 
        /// The path in the child must be relative to <paramref name="pathToPackageXmlDir"/>
        /// </summary>
        /// <param name="typeXmlElement">The xml element representing the type</param>
        /// <param name="childElementName">name of the child of the <paramref name="xmlElement"/> which contains the relative path</param>
        /// <param name="pathToPackageXmlDir">the absolute path of the containing xml file</param>
        /// <returns>the full path combined from the <paramref name="pathToPackageXmlDir"/> and the relative path contained in the child <paramref name="childElementName"/> of <paramref name="xmlElement"/></returns>
        public static string GetFullPath(XElement typeXmlElement, string childElementName, string pathToPackageXmlDir) {
            return System.IO.Path.Combine(pathToPackageXmlDir,
                                          FileManager.CorrectRelativePath(typeXmlElement.Element(PackageManager.XMLNamespace + 
                                                                                             childElementName)
                                                                                    .Value
                                                                                    .Trim()));
        }

        public static int GetInt(XElement typeXmlElement, string childElementName) {
            return int.Parse(typeXmlElement.Element(PackageManager.XMLNamespace + childElementName).Value);
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

        public static T LoadTypePlugin<T>(XElement typeXml, string assemblyPathElementName, string pathToPackageXmlDir, string typeName) where T: class, ITypePlugin {
            if (!System.IO.Path.IsPathRooted(pathToPackageXmlDir)) {
                pathToPackageXmlDir = System.IO.Path.Combine(MyGame.Config.DynamicDirPath, pathToPackageXmlDir);
            }

            string assemblyPath = GetFullPath(typeXml, assemblyPathElementName, pathToPackageXmlDir);

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
                throw new Exception("Type plugin loading failed, could not load plugin");
            }

            if (pluginInstance == null) {
                //TODO: Exception
                throw new Exception("Type plugin loading failed, could not load plugin");
            }

            return pluginInstance;
        }
    }
}
