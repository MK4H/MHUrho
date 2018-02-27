using System;
using System.Collections.Generic;
using System.Text;
using MHUrho.Logic;
using Urho;
using Urho.Resources;

namespace MHUrho.Packaging {
    public class ResourcePack {
        private const string defaultThumbnailPath = "Textures/xamarin.png";

        public string Name { get; private set; }
        
        public int ID { get; private set; }

        public string Description { get; private set; }

        public Image Thumbnail { get; private set; }

        private string pathToXml;

        List<TileType> tileTypes;
        List<UnitType> unitTypes;

        /// <summary>
        /// Loads data for initial resource pack managment, so the user can choose which resource packs to 
        /// use, which to download, which to delete and so on
        /// 
        /// Before using this resource pack in game, you need to call LoadAll(...)
        /// </summary>
        /// <param name="cache">ResourceCache to store the thumbnail in</param>
        /// <param name="name">Name of the resource pack</param>
        /// <param name="id">Unique id of this pack in the currently active game</param>
        /// <param name="pathToXml">Path to the resource pack XML description</param>
        /// <param name="description">Human readable description of the resource pack contents for the user</param>
        /// <param name="pathToThumbnail">Path to thumbnail to display</param>
        /// <returns>Initialized resource pack</returns>
        public static ResourcePack InitialLoad( ResourceCache cache, 
                                                string name,
                                                string pathToXml, 
                                                string description, 
                                                string pathToThumbnail) {
            var thumbnail = cache.GetImage(pathToThumbnail ?? defaultThumbnailPath);

            return new ResourcePack(name, pathToXml, description ?? "No description", thumbnail);
        }

        protected ResourcePack(string name, string pathToXml, string description, Image thumbnail) {
            this.Name = name;
            this.pathToXml = pathToXml;
            this.Description = description;
            this.Thumbnail = thumbnail;
        }

        /// <summary>
        /// Loads all resources contained in this resource pack
        /// </summary>
        /// <param name="cache"></param>
        public void LoadAll(ResourceCache cache) {

        }
    }
}
