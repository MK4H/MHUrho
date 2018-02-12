﻿using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Xml;
using System.Linq;
using System.Xml.Linq;
using System.Xml.Schema;
using Urho;
using Urho.IO;
using Urho.Resources;
using FileMode = System.IO.FileMode;

namespace MHUrhoStandard
{
    /// <summary>
    /// ResourceCache wrapper providing loading, unloading and downloading of ResourcePacks
    /// </summary>
    internal class AssetManager
    {
        /// <summary>
        /// Path to the schema for Resource Pack Directory xml files
        /// </summary>
        private static readonly string ResPacDirSchemaPath = Path.Combine("Data","Schemas","ResourcePack.xsd");

        private readonly ResourceCache cache;

        private readonly List<ResourcePack> availablePacks = new List<ResourcePack>();

        public AssetManager(ResourceCache cache, IStartupDataProvider startupData, IEnumerable<string> resourcePackPaths)
        {
            this.cache = cache;

            var schema = new XmlSchemaSet();
            try
            {

                schema.Add("http://www.MobileHold.cz/ResourcePack.xsd", XmlReader.Create(startupData.GetFile(ResPacDirSchemaPath)));
            }
            catch (IOException e)
            {
                Log.Write(LogLevel.Error,string.Format("Error loading ResroucePack schema: {0}",e));
                if (Debugger.IsAttached) Debugger.Break();
                //Reading of static file of this app failed, something is horribly wrong, die
                //TODO: Error reading static data of app
            }
           
            foreach (var path in resourcePackPaths)
            {
                ParseResourcePackDir(path, schema);
            }
        }

        /// <summary>
        /// Pulls data about the resource packs contained in this directory from XML file
        /// </summary>
        /// <param name="path">Path to the XML file of Resource pack directory</param>
        /// <param name="schema">Schema for the resource pack directory type of XML files</param>
        /// <returns>True if successfuly read, False if there was an error while loading</returns>
        void ParseResourcePackDir(string path, XmlSchemaSet schema)
        {

            IEnumerable<ResourcePack> loadedPacks = null;

            try
            {
                XDocument doc = XDocument.Load(new FileStream(path, FileMode.Open, FileAccess.Read));
                doc.Validate(schema, null);

                loadedPacks = from packages in doc.Root.Elements("resourcePack")
                    select ResourcePack.InitialLoad(
                        cache,
                        packages.Attribute("name")?.Value,
                        packages.Element("path")?.Value,
                        packages.Element("description")?.Value,
                        packages.Element("thumbnailPath")?.Value);
            }
            catch (IOException e)
            {
                //Creation of the FileStream failed, cannot load this directory
                Log.Write(LogLevel.Warning, string.Format("Opening ResroucePack directory file at {0} failed: {1}", path,e));
                if (Debugger.IsAttached) Debugger.Break();
            }
            //TODO: Exceptions
            catch (XmlSchemaValidationException e)
            {
                //Invalid resource pack description file, dont load this pack directory
                Log.Write(LogLevel.Warning, string.Format("ResroucePack directory file at {0} does not conform to the schema: {1}", path, e));
                if (Debugger.IsAttached) Debugger.Break();
            }
            catch (XmlException e)
            {
                //TODO: Alert user for corrupt file
                Log.Write(LogLevel.Warning, string.Format("ResroucePack directory file at {0} : {1}", path, e));
                if (Debugger.IsAttached) Debugger.Break();
            }

            //If loading failed completely, dont add anything
            if (loadedPacks == null) return;

            //Adds all the discovered packs into the availablePacks list
            availablePacks.AddRange(loadedPacks);

        }
    }
}
