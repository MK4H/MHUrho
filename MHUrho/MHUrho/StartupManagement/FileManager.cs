﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Urho.IO;

namespace MHUrho
{
    public abstract class FileManager
    {
        //TODO: Load from config file
        public List<string> PackagePaths { get; protected set; }

        public string LogPath { get; private set; }

        protected string ConfigFilePath;
        protected string DefaultConfigFilePath;

        protected string StaticDirPath;
        protected string DynamicDirPath;

        public static string CorrectRelativePath(string relativePath) {
            if (relativePath == null) {
                return null;
            }
            return Path.DirectorySeparatorChar != '/' ? relativePath.Replace('/', Path.DirectorySeparatorChar) : relativePath;
        } 

        /// <summary>
        /// Gets stream allowing reading from static file, either directy from static file
        /// or if there exists changed copy in dynamic files, from this changed copy
        /// </summary>
        /// <param name="relativePath"></param>
        /// <returns>Read only stream allowing reading from static file</returns>
        public abstract Stream OpenStaticFileRO(string relativePath);

        /// <summary>
        /// Checks if there is changed copy in dynamic files, if there is, 
        /// opens it, otherwise creates new copy and opens this new copy
        /// </summary>
        /// <param name="relativePath"></param>
        /// <returns>ReadWrite stream</returns>
        public abstract Stream OpenStaticFileRW(string relativePath);

        /// <summary>
        /// Gets stream allowing reading and writing from file,
        /// if the file does not exist at dynamic path, 
        /// tries to make a copy of the file from static data
        /// </summary>
        /// <param name="relativePath"></param>
        /// <param name="fileMode"></param>
        /// <param name="fileAccess"></param>
        /// <returns></returns>
        public abstract Stream OpenDynamicFile(string relativePath, System.IO.FileMode fileMode, FileAccess fileAccess);

        /// <summary>
        /// Copies static file or directory to dynamic data directory, 
        /// recreating the same directory structure as in static data
        /// </summary>
        /// <param name="srcRelativePath">Source relative path in static data</param>
        public abstract void CopyStaticToDynamic(string srcRelativePath);

        protected FileManager(
            List<string> packagePaths,
            string configFilePath,
            string defaultConfigFilePath,
            string staticDirPath,
            string dynamicDirPath,
            string logPath) {

            this.PackagePaths = packagePaths;
            this.ConfigFilePath = configFilePath;
            this.DefaultConfigFilePath = defaultConfigFilePath;
            this.StaticDirPath = staticDirPath;
            this.DynamicDirPath = dynamicDirPath;
            this.LogPath = logPath;
        }

    }
}