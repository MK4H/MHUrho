using System;
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
		public string StaticDirPath { get; protected set; }
		public string DynamicDirPath { get; protected set; }

		public string ConfigFilePath { get; protected set; }

		/// <summary>
		/// Path to the directory containing saved games
		/// Relative to <see cref="DynamicDirPath"/>
		/// </summary>
		public string SaveGameDirPath { get; protected set; }

		public string SaveGameDirAbsolutePath => Path.Combine(DynamicDirPath, SaveGameDirPath);

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

		public abstract bool FileExists(string path);

		/// <summary>
		/// 
		/// </summary>
		/// <param name="dirPath"></param>
		/// <param name="files"></param>
		/// <param name="directories"></param>
		/// <param name="searchPattern">A string to match against the file names of the entries, can contain literals and wildCard characters * and ?.</param>
		/// <param name="searchOption"></param>
		/// <returns></returns>
		public abstract IEnumerable<string> GetFSEntriesInDirectory(string dirPath, 
																	bool files, 
																	bool directories);

		public abstract IEnumerable<string> GetFSEntriesInDirectory(string dirPath,
																	bool files,
																	bool directories,
																	string searchPattern,
																	SearchOption searchOption);

		public abstract void DeleteDynamicFile(string relativePath);

		protected FileManager(
			List<string> packagePaths,
			string configFilePath,
			string staticDirPath,
			string dynamicDirPath,
			string logPath,
			string saveDirPath) {

			this.PackagePaths = packagePaths;
			this.ConfigFilePath = configFilePath;
			this.StaticDirPath = staticDirPath;
			this.DynamicDirPath = dynamicDirPath;
			this.LogPath = logPath;
			this.SaveGameDirPath = saveDirPath;
		}

	}
}
