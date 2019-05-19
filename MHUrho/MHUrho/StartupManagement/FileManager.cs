using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using MHUrho.Packaging;
using Urho.IO;

namespace MHUrho
{
	public abstract class FileManager
	{
		/// <summary>
		/// Absolute path of the PackageDirectory <see cref="PackageDirectoryPath"/>
		/// </summary>
		public string PackageDirectoryPath { get; private set; } 

		/// <summary>
		/// Absolute path of the log file.
		/// </summary>
		public string LogPath { get; private set; }
		
		/// <summary>
		/// Absolute path of the directory containing static app data.
		/// </summary>
		public string StaticDirPath { get; protected set; }

		/// <summary>
		/// Absolute path of the directory containing dynamic app data, created at runtime.
		/// </summary>
		public string DynamicDirPath { get; protected set; }

		/// <summary>
		/// Relative file path of the config file, which has the default instance inside the <see cref="StaticDirPath"/> and the user specific in <see cref="DynamicDirPath"/>
		/// </summary>
		public string ConfigFilePath { get; protected set; }

		/// <summary>
		/// Relative path to the directory containing saved games.
		/// Relative to <see cref="DynamicDirPath"/>
		/// </summary>
		public string SaveGameDirPath { get; protected set; }

		/// <summary>
		/// Absolute path of the directory containing saved games.
		/// </summary>
		public string SaveGameDirAbsolutePath => Path.Combine(DynamicDirPath, SaveGameDirPath);

		/// <summary>
		/// Replaces directory separators in the provided <paramref name="relativePath"/> from / as a directory separator to the platform specific <see cref="Path.DirectorySeparatorChar"/>
		/// </summary>
		/// <param name="relativePath">The path to correct</param>
		/// <returns>Relative path separated by the correct <see cref="Path.DirectorySeparatorChar"/></returns>
		public static string ReplaceDirectorySeparators(string relativePath) {
			if (relativePath == null) {
				return null;
			}
			return Path.DirectorySeparatorChar != '/' ? relativePath.Replace('/', Path.DirectorySeparatorChar) : relativePath;
		}

		/// <summary>
		/// Opens a file on the <paramref name="relativePath"/> in the <paramref name="package"/> directory
		///
		/// If no package is specified, the <see cref="PackageManager.ActivePackage"/> is used
		/// </summary>
		/// <param name="relativePath">Path of the file relative to the package directory</param>
		/// <param name="fileMode"></param>
		/// <param name="fileAccess"></param>
		/// <param name="package"></param>
		/// <returns></returns>
		public Stream OpenDynamicFileInPackage(string relativePath,
												System.IO.FileMode fileMode,
												FileAccess fileAccess,
												GamePack package = null)
		{
			if (package == null) {
				package = PackageManager.Instance.ActivePackage;
			}

			string basePath = package.DirectoryPath;
			string dynamicPath = Path.Combine(basePath, relativePath);

			return OpenDynamicFile(dynamicPath, fileMode, fileAccess);
		}

		/// <summary>
		/// Gets stream allowing reading from static file, either directly from static file
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

		/// <summary>
		/// Creates a copy of a file or a directory with the whole subtree copied
		/// If <paramref name="source"/> refers to the same file/directory as <paramref name="target"/>, behavior is undefined
		/// </summary>
		/// <param name="source">Rooted path from which to copy</param>
		/// <param name="target">Rooted path to which to copy</param>
		/// <param name="overrideFiles"></param>
		public abstract void Copy(string source, string target, bool overrideFiles);

		public abstract bool FileExists(string path);

		public abstract bool DirectoryExists(string path);

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

		/// <summary>
		/// Creates new instance of file manager
		/// </summary>
		/// <param name="staticDataDirAbs">Absolute path of the directory containing static data, distributed with the app and read only.</param>
		/// <param name="dynamicDataDirAbs">Absolute path of the directory containing dynamic data, created during runtime.</param>
		/// <param name="packageDirAbs">Absolute path of the directory containing packages.</param>
		/// <param name="logFileAbs">Absolute path of where to create the log file.</param>
		/// <param name="configFileRel">Relative path of the config file in static and dynamic directories. (Static for default, dynamic for user specific)</param>
		/// <param name="savedGamesRel">Relative path inside the dynamic directory where to save the saved games, should be separate directory containing only the saved games.</param>
		protected FileManager(
			string staticDataDirAbs,
			string dynamicDataDirAbs,
			string packageDirAbs,
			string logFileAbs,
			string configFileRel,
			string savedGamesRel) {

			
			
			this.StaticDirPath = staticDataDirAbs;
			this.DynamicDirPath = dynamicDataDirAbs;
			this.PackageDirectoryPath = packageDirAbs;
			this.LogPath = logFileAbs;
			this.ConfigFilePath = configFileRel;
			this.SaveGameDirPath = savedGamesRel;
			
		}

	}
}
