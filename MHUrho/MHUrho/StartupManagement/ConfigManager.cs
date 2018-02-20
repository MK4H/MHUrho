using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Urho.IO;

namespace MHUrho
{
    public abstract class ConfigManager
    {
        //TODO: Load from config file
        public List<string> PackagePaths { get; protected set; }

        public string LogPath { get; private set; }

        protected string ConfigFilePath;
        protected string DefaultConfigFilePath;

        protected string StaticFilePath;
        protected string DynamicFilePath;

        /// <summary>
        /// Gets stream allowing reading from static file, for writing call GetDynamicFile
        /// </summary>
        /// <param name="relativePath"></param>
        /// <returns>Read only stream allowing reading from static file</returns>
        public abstract Stream GetStaticFileRO(string relativePath);

        /// <summary>
        /// Gets stream allowing reading and writing from file,
        /// if the file does not exist at dynamic path, 
        /// tries to make a copy of the file from static data
        /// </summary>
        /// <param name="relativePath"></param>
        /// <returns></returns>
        public abstract Stream GetDynamicFile(string relativePath);

        /// <summary>
        /// Copies static file or directory to dynamic data directory, 
        /// recreating the same directory structure as in static data
        /// </summary>
        /// <param name="srcRelativePath">Source relative path in static data</param>
        public abstract void CopyStaticToDynamic(string srcRelativePath);

        protected ConfigManager(
            List<string> packagePaths,
            string configFilePath,
            string defaultConfigFilePath,
            string staticFilePath,
            string dynamicFilePath,
            string logPath) {

            this.PackagePaths = packagePaths;
            this.ConfigFilePath = configFilePath;
            this.DefaultConfigFilePath = defaultConfigFilePath;
            this.StaticFilePath = staticFilePath;
            this.DynamicFilePath = dynamicFilePath;
            this.LogPath = logPath;
        }

    }
}
