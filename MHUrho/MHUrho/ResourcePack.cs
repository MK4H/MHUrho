using System;
using System.Collections.Generic;
using System.Text;
using Urho;
using Urho.Resources;

namespace MHUrho
{
    internal class ResourcePack
    {
        private const string defaultThumbnailPath = "Textures/xamarin.png";

        private string name;
        private string pathToXml;
        private string description;
        private Image thumbnail;

        public static ResourcePack InitialLoad(ResourceCache cache, string name, string pathToXml, string description, string pathToThumbnail)
        {
            var thumbnail = cache.GetImage(pathToThumbnail ?? defaultThumbnailPath);

            return new ResourcePack(name, pathToXml, description ?? "No description",thumbnail);
        }

        protected ResourcePack(string name, string pathToXml, string description, Image thumbnail)
        {
            this.name = name;
            this.pathToXml = pathToXml;
            this.description = description;
            this.thumbnail = thumbnail;
        }
    }
}
