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
		/// Relative path inside the DynamicDir to the directory where the packages are stored and
		/// the resource package directory xml file is
		/// </summary>
		public string PackageDirectoryPath { get; protected set; }

		/// <summary>
		/// Absolute path of the PackageDirectory <see cref="PackageDirectoryPath"/>
		/// </summary>
		public string PackageDirectoryAbsolutePath => Path.Combine(DynamicDirPath, PackageDirectoryPath);

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

		protected FileManager(
			string packageDirectoryPath,
			string configFilePath,
			string staticDirPath,
			string dynamicDirPath,
			string logPath,
			string saveDirPath) {

			this.PackageDirectoryPath = packageDirectoryPath;
			this.ConfigFilePath = configFilePath;
			this.StaticDirPath = staticDirPath;
			this.DynamicDirPath = dynamicDirPath;
			this.LogPath = logPath;
			this.SaveGameDirPath = saveDirPath;
		}

	}
}
